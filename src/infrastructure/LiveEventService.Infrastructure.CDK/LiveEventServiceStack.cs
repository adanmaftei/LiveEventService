using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.SecretsManager;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.Backup;
using Amazon.CDK.AWS.KMS;
using Amazon.CDK.AWS.WAFv2;
using Amazon.CDK.AWS.Logs;
using Constructs;
using System.Text.Json;
using Amazon.CDK.AWS.ApplicationAutoScaling;
using Amazon.CDK.AWS.APS;
using Amazon.CDK.AWS.Grafana;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.Lambda;
// using Amazon.CDK.AWS.Lambda.Python;
using Amazon.CDK.AWS.S3.Assets;
using Amazon.CDK.CustomResources;
using Amazon.CDK.AWS.SQS;

namespace LiveEventService.Infrastructure.CDK;

public class LiveEventServiceStack : Stack
{
    internal LiveEventServiceStack(Construct scope, string id, IStackProps? props = null)
        : base(scope, id, props)
    {
        // Parameters to right-size networking costs
        var natGatewaysParam = new CfnParameter(this, "NatGateways", new CfnParameterProps
        {
            Type = "Number",
            Default = 1,
            MinValue = 0,
            MaxValue = 2,
            Description = "Number of NAT Gateways (0 for dev/cost-saving; 1 for prod)"
        });
        // Create a VPC
        var vpc = new Vpc(this, "LiveEventVPC", new VpcProps
        {
            MaxAzs = 2,
            NatGateways = natGatewaysParam.ValueAsNumber,
            SubnetConfiguration = new[]
            {
                new SubnetConfiguration
                {
                    Name = "Public",
                    SubnetType = SubnetType.PUBLIC,
                    CidrMask = 24
                },
                new SubnetConfiguration
                {
                    Name = "Private",
                    SubnetType = SubnetType.PRIVATE_WITH_EGRESS,
                    CidrMask = 24
                }
            }
        });

        // Add VPC Interface Endpoint for SQS to minimize NAT data processing costs
        vpc.AddInterfaceEndpoint("SqsVpcEndpoint", new InterfaceVpcEndpointOptions
        {
            Service = InterfaceVpcEndpointAwsService.SQS,
            Subnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS }
        });

        // Add VPC Interface Endpoints to reduce NAT usage for common AWS services
        vpc.AddInterfaceEndpoint("SecretsManagerVpcEndpoint", new InterfaceVpcEndpointOptions
        {
            Service = InterfaceVpcEndpointAwsService.SECRETS_MANAGER,
            Subnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS }
        });
        vpc.AddInterfaceEndpoint("CloudWatchLogsVpcEndpoint", new InterfaceVpcEndpointOptions
        {
            Service = InterfaceVpcEndpointAwsService.CLOUDWATCH_LOGS,
            Subnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS }
        });
        vpc.AddInterfaceEndpoint("XRayVpcEndpoint", new InterfaceVpcEndpointOptions
        {
            Service = InterfaceVpcEndpointAwsService.XRAY,
            Subnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS }
        });

        // ECR (API and Docker registry) endpoints for ECS image pulls without NAT
        vpc.AddInterfaceEndpoint("EcrApiVpcEndpoint", new InterfaceVpcEndpointOptions
        {
            Service = InterfaceVpcEndpointAwsService.ECR,
            Subnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS }
        });
        vpc.AddInterfaceEndpoint("EcrDkrVpcEndpoint", new InterfaceVpcEndpointOptions
        {
            Service = InterfaceVpcEndpointAwsService.ECR_DOCKER,
            Subnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS }
        });
        // STS for temporary credentials without NAT
        vpc.AddInterfaceEndpoint("StsVpcEndpoint", new InterfaceVpcEndpointOptions
        {
            Service = InterfaceVpcEndpointAwsService.STS,
            Subnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS }
        });
        // S3 Gateway endpoint for ECR layer downloads and other S3 access without NAT
        vpc.AddGatewayEndpoint("S3GatewayEndpoint", new GatewayVpcEndpointOptions
        {
            Service = GatewayVpcEndpointAwsService.S3,
            Subnets = new[] { new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS } }
        });

        // Create a Cognito User Pool for authentication
        var userPool = new UserPool(this, "LiveEventUserPool", new UserPoolProps
        {
            UserPoolName = "LiveEventUserPool",
            SelfSignUpEnabled = true,
            SignInAliases = new SignInAliases
            {
                Email = true
            },
            AutoVerify = new AutoVerifiedAttrs
            {
                Email = true
            },
            StandardAttributes = new StandardAttributes
            {
                Email = new StandardAttribute { Required = true, Mutable = true },
                GivenName = new StandardAttribute { Required = true, Mutable = true },
                FamilyName = new StandardAttribute { Required = true, Mutable = true },
                PhoneNumber = new StandardAttribute { Required = false, Mutable = true }
            },
            PasswordPolicy = new PasswordPolicy
            {
                MinLength = 8,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireDigits = true,
                RequireSymbols = true,
                TempPasswordValidity = Duration.Days(7)
            }
        });

        // Create a user pool client
        var userPoolClient = new UserPoolClient(this, "LiveEventUserPoolClient", new UserPoolClientProps
        {
            UserPool = userPool,
            AuthFlows = new AuthFlow { UserPassword = true, AdminUserPassword = true },
            OAuth = new OAuthSettings
            {
                Flows = new OAuthFlows
                {
                    AuthorizationCodeGrant = true
                },
                Scopes = new[] { OAuthScope.EMAIL, OAuthScope.OPENID, OAuthScope.PROFILE },
                CallbackUrls = new[] { "http://localhost:3000/callback" }, // Update with your frontend URL
                LogoutUrls = new[] { "http://localhost:3000" } // Update with your frontend URL
            },
            GenerateSecret = true
        });

        // Create an identity pool
        var identityPool = new CfnIdentityPool(this, "LiveEventIdentityPool", new CfnIdentityPoolProps
        {
            AllowUnauthenticatedIdentities = false,
            CognitoIdentityProviders = new[]
            {
                new CfnIdentityPool.CognitoIdentityProviderProperty
                {
                    ClientId = userPoolClient.UserPoolClientId,
                    ProviderName = userPool.UserPoolProviderName,
                    ServerSideTokenCheck = true
                }
            }
        });

        // Create a database credentials secret with rotation
        var databaseCredentialsSecret = new Amazon.CDK.AWS.SecretsManager.Secret(this, "LiveEventDBCredentials", new SecretProps
        {
            SecretName = "liveevent-db-credentials",
            GenerateSecretString = new SecretStringGenerator
            {
                ExcludeCharacters = "%@/\"",
                ExcludePunctuation = true,
                IncludeSpace = false,
                GenerateStringKey = "password",
                SecretStringTemplate = JsonSerializer.Serialize(new
                {
                    username = "liveeventadmin"
                })
            }
        });

        // Create a KMS key for RDS encryption
        var rdsKey = new Key(this, "RDSEncryptionKey", new KeyProps
        {
            EnableKeyRotation = true,
            Description = "KMS key for encrypting RDS data",
            Policy = new PolicyDocument(new PolicyDocumentProps
            {
                Statements = new[]
                {
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new[] { "kms:*" },
                        Principals = new[] { new AccountRootPrincipal() },
                        Resources = new[] { "*" }
                    })
                }
            })
        });

        // Create a database with backup and monitoring
        var database = new DatabaseInstance(this, "LiveEventDatabase", new DatabaseInstanceProps
        {
            Engine = DatabaseInstanceEngine.Postgres(new PostgresInstanceEngineProps
            {
                Version = PostgresEngineVersion.VER_14
            }),
            Credentials = Credentials.FromSecret(databaseCredentialsSecret),
            Vpc = vpc,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
            InstanceType = Amazon.CDK.AWS.EC2.InstanceType.Of(InstanceClass.T3, InstanceSize.MICRO),
            AllocatedStorage = 20,
            MaxAllocatedStorage = 100, // Allow storage autoscaling up to 100GB
            RemovalPolicy = RemovalPolicy.RETAIN, // Retain database on stack deletion
            DeletionProtection = true, // Prevent accidental deletion
            DatabaseName = "LiveEventDB",
            BackupRetention = Duration.Days(35), // 35 days of automated backups
            MonitoringInterval = Duration.Seconds(60),
            StorageEncrypted = true,
            StorageEncryptionKey = rdsKey,
            EnablePerformanceInsights = true,
            PerformanceInsightRetention = PerformanceInsightRetention.DEFAULT,
            CloudwatchLogsExports = new[] { "postgresql" },
            CloudwatchLogsRetention = RetentionDays.ONE_YEAR,
            AutoMinorVersionUpgrade = true,
            DeleteAutomatedBackups = false, // Keep automated backups when instance is deleted

            // Enable enhanced monitoring
            MonitoringRole = new Role(this, "RDSMonitoringRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("monitoring.rds.amazonaws.com"),
                ManagedPolicies = new[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AmazonRDSEnhancedMonitoringRole")
                }
            }),

            // Configure backup window
            PreferredBackupWindow = "03:00-04:00", // 3-4 AM UTC
            PreferredMaintenanceWindow = "sun:04:00-sun:05:00", // Weekly maintenance window

            // Enable Performance Insights
            ParameterGroup = new ParameterGroup(this, "DBParameterGroup", new ParameterGroupProps
            {
                Engine = DatabaseInstanceEngine.Postgres(new PostgresInstanceEngineProps
                {
                    Version = PostgresEngineVersion.VER_14
                }),
                Parameters = new Dictionary<string, string>
                {
                    { "rds.force_ssl", "1" }, // Force SSL connections
                    { "log_statement", "ddl" }, // Log all DDL statements
                    { "log_min_duration_statement", "1000" }, // Log slow queries > 1s
                    { "log_connections", "1" }, // Log all connection attempts
                    { "log_disconnections", "1" } // Log all disconnections
                }
            })
        });

        // Create a new backup plan
        var customBackupPlan = new BackupPlan(this, "LiveEventBackupPlan", new BackupPlanProps
        {
            BackupPlanName = "LiveEventServiceBackupPlan",
            BackupPlanRules = new[]
            {
                new BackupPlanRule(new BackupPlanRuleProps
                {
                    RuleName = "DailyBackups",
                    ScheduleExpression = Amazon.CDK.AWS.Events.Schedule.Cron(new Amazon.CDK.AWS.Events.CronOptions
                    {
                        Minute = "5",
                        Hour = "4",
                        Day = "*"
                    }),
                    EnableContinuousBackup = true,
                    DeleteAfter = Duration.Days(35),
                    MoveToColdStorageAfter = Duration.Days(1)
                }),
                new BackupPlanRule(new BackupPlanRuleProps
                {
                    RuleName = "MonthlyBackupRetention",
                    ScheduleExpression = Amazon.CDK.AWS.Events.Schedule.Cron(new Amazon.CDK.AWS.Events.CronOptions
                    {
                        Day = "1",
                        Hour = "5",
                        Minute = "0"
                    }),
                    DeleteAfter = Duration.Days(90)
                })
            }
        });

        // Add database to backup plan
        customBackupPlan.AddSelection("DatabaseBackupSelection", new BackupSelectionOptions
        {
            Resources = new[] { BackupResource.FromRdsDatabaseInstance(database) },
            AllowRestores = true
        });

        // Create a CloudWatch alarm for backup failures
        new Alarm(this, "BackupFailureAlarm", new AlarmProps
        {
            AlarmName = "LiveEventService-BackupFailure",
            Metric = new Metric(new MetricProps
            {
                Namespace = "AWS/Backup",
                MetricName = "NumberOfBackupJobsFailed",
                DimensionsMap = new Dictionary<string, string>
                {
                    ["BackupVaultName"] = "Default"
                },
                Statistic = "Sum",
                Period = Duration.Hours(1)
            }),
            Threshold = 0,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD,
            AlarmDescription = "Alarm when any backup job fails",
            ActionsEnabled = true
        });

        // Add a dependency to ensure the KMS key is created before the database
        database.Node.AddDependency(rdsKey);

        // Allow connections to the database from ECS tasks
        database.Connections.AllowDefaultPortFromAnyIpv4("Allow database access from ECS tasks");

        // Create an ECS cluster with container insights and capacity providers
        var cluster = new Cluster(this, "LiveEventCluster", new ClusterProps
        {
            Vpc = vpc,            
            EnableFargateCapacityProviders = true,
            ClusterName = "LiveEventServiceCluster"
        });

        // Create SQS queue with DLQ for domain events
        var dlq = new Queue(this, "DomainEventsDLQ", new QueueProps
        {
            QueueName = "liveevent-domain-events-dlq",
            VisibilityTimeout = Duration.Seconds(60),
            RetentionPeriod = Duration.Days(14)
        });

        var domainEventsQueue = new Queue(this, "DomainEventsQueue", new QueueProps
        {
            QueueName = "liveevent-domain-events",
            VisibilityTimeout = Duration.Seconds(30),
            DeadLetterQueue = new DeadLetterQueue { Queue = dlq, MaxReceiveCount = 5 }
        });

        // SQS CloudWatch alarms (queue lag and DLQ growth)
        var sqsOldestAgeAlarm = new Alarm(this, "SqsQueueOldestAgeAlarm", new AlarmProps
        {
            AlarmName = "LiveEventService-SQS-OldestMessageAgeHigh",
            AlarmDescription = "Approximate age of oldest message in SQS is high (processing backlog).",
            Metric = new Metric(new MetricProps
            {
                Namespace = "AWS/SQS",
                MetricName = "ApproximateAgeOfOldestMessage",
                DimensionsMap = new Dictionary<string, string> { ["QueueName"] = domainEventsQueue.QueueName },
                Statistic = "Maximum",
                Period = Duration.Minutes(1)
            }),
            Threshold = 300, // 5 minutes
            EvaluationPeriods = 5,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD
        });

        var sqsDepthAlarm = new Alarm(this, "SqsQueueDepthAlarm", new AlarmProps
        {
            AlarmName = "LiveEventService-SQS-QueueDepthHigh",
            AlarmDescription = "Number of visible messages is high (backlog growing).",
            Metric = new Metric(new MetricProps
            {
                Namespace = "AWS/SQS",
                MetricName = "ApproximateNumberOfMessagesVisible",
                DimensionsMap = new Dictionary<string, string> { ["QueueName"] = domainEventsQueue.QueueName },
                Statistic = "Average",
                Period = Duration.Minutes(1)
            }),
            Threshold = 1000,
            EvaluationPeriods = 5,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD
        });

        var sqsDlqAlarm = new Alarm(this, "SqsDlqNotEmptyAlarm", new AlarmProps
        {
            AlarmName = "LiveEventService-SQS-DLQ-NotEmpty",
            AlarmDescription = "Messages are landing in DLQ.",
            Metric = new Metric(new MetricProps
            {
                Namespace = "AWS/SQS",
                MetricName = "ApproximateNumberOfMessagesVisible",
                DimensionsMap = new Dictionary<string, string> { ["QueueName"] = dlq.QueueName },
                Statistic = "Average",
                Period = Duration.Minutes(1)
            }),
            Threshold = 1,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD
        });

        // Parameters for worker desired and scaling capacity (override per env at deploy time)
        var workerDesiredCountParam = new CfnParameter(this, "WorkerDesiredCount", new CfnParameterProps
        {
            Type = "Number",
            Default = 1,
            MinValue = 0,
            MaxValue = 100,
            Description = "Desired number of LiveEvent worker tasks"
        });
        var workerMinCapacityParam = new CfnParameter(this, "WorkerMinCapacity", new CfnParameterProps
        {
            Type = "Number",
            Default = 0,
            MinValue = 0,
            MaxValue = 100,
            Description = "Minimum number of LiveEvent worker tasks"
        });
        var workerMaxCapacityParam = new CfnParameter(this, "WorkerMaxCapacity", new CfnParameterProps
        {
            Type = "Number",
            Default = 10,
            MinValue = 1,
            MaxValue = 200,
            Description = "Maximum number of LiveEvent worker tasks"
        });

        // Worker: Fargate service to process domain events from SQS
        var workerTaskDef = new FargateTaskDefinition(this, "LiveEventWorkerTaskDef", new FargateTaskDefinitionProps
        {
            MemoryLimitMiB = 512,
            Cpu = 256,
            RuntimePlatform = new RuntimePlatform
            {
                CpuArchitecture = CpuArchitecture.ARM64,
                OperatingSystemFamily = OperatingSystemFamily.LINUX
            }
        });

        // Allow worker to consume from SQS
        domainEventsQueue.GrantConsumeMessages(workerTaskDef.TaskRole);

        var workerContainer = workerTaskDef.AddContainer("LiveEventWorker", new ContainerDefinitionOptions
        {
            Image = ContainerImage.FromAsset("../../LiveEventService.Worker", new AssetImageProps
            {
                File = "Dockerfile"
            }),
            Logging = LogDriver.AwsLogs(new AwsLogDriverProps
            {
                StreamPrefix = "LiveEventWorker",
                LogRetention = RetentionDays.ONE_MONTH
            }),
            Environment = new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["AWS_REGION"] = this.Region,
                ["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "false",
                // Ensure worker points at the SQS queue in all environments
                ["AWS__SQS__QueueName"] = domainEventsQueue.QueueName
            },
            Secrets = new Dictionary<string, Amazon.CDK.AWS.ECS.Secret>
            {
                ["ConnectionStrings__DefaultConnection"] = Amazon.CDK.AWS.ECS.Secret.FromSecretsManager(
                    databaseCredentialsSecret,
                    "ConnectionStrings__DefaultConnection")
            }
        });

        // Run worker without a load balancer; private subnets only
        var workerService = new FargateService(this, "LiveEventWorkerService", new FargateServiceProps
        {
            Cluster = cluster,
            TaskDefinition = workerTaskDef,
            DesiredCount = workerDesiredCountParam.ValueAsNumber,
            AssignPublicIp = false,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS }
        });

        // Autoscale worker based on SQS backlog indicators
        var workerScaling = workerService.AutoScaleTaskCount(new EnableScalingProps
        {
            MinCapacity = workerMinCapacityParam.ValueAsNumber,
            MaxCapacity = workerMaxCapacityParam.ValueAsNumber
        });

        // Target tracking: keep age of oldest message around 60s
        workerScaling.ScaleToTrackCustomMetric("SqsAgeTarget", new Amazon.CDK.AWS.ECS.TrackCustomMetricProps
        {
            TargetValue = 60,
            Metric = new Metric(new MetricProps
            {
                Namespace = "AWS/SQS",
                MetricName = "ApproximateAgeOfOldestMessage",
                DimensionsMap = new Dictionary<string, string> { ["QueueName"] = domainEventsQueue.QueueName },
                Statistic = "Maximum",
                Period = Duration.Minutes(1)
            })
        });

        // Optional safety: step scaling when queue depth spikes
        workerScaling.ScaleOnMetric("SqsDepthStepScale", new BasicStepScalingPolicyProps
        {
            Metric = new Metric(new MetricProps
            {
                Namespace = "AWS/SQS",
                MetricName = "ApproximateNumberOfMessagesVisible",
                DimensionsMap = new Dictionary<string, string> { ["QueueName"] = domainEventsQueue.QueueName },
                Statistic = "Average",
                Period = Duration.Minutes(1)
            }),
            ScalingSteps = new[]
            {
                new ScalingInterval { Lower = 200, Change = +1 },
                new ScalingInterval { Lower = 500, Change = +2 },
                new ScalingInterval { Lower = 1000, Change = +3 }
            },
            Cooldown = Duration.Minutes(1),
            AdjustmentType = AdjustmentType.CHANGE_IN_CAPACITY
        });

        // Create a task definition for the API
        var taskDefinition = new FargateTaskDefinition(this, "LiveEventTaskDef", new FargateTaskDefinitionProps
        {
            MemoryLimitMiB = 1024,
            Cpu = 512,
            RuntimePlatform = new RuntimePlatform
            {
                CpuArchitecture = CpuArchitecture.ARM64,
                OperatingSystemFamily = OperatingSystemFamily.LINUX
            }
        });

        // Observability: use X-Ray + CloudWatch EMF only (AMP/Grafana deferred)

        // Grant task role permissions for observability exporters
        taskDefinition.TaskRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("CloudWatchAgentServerPolicy"));
        taskDefinition.TaskRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AWSXRayDaemonWriteAccess"));

        // Add container to the task definition
        var container = taskDefinition.AddContainer("LiveEventAPI", new ContainerDefinitionOptions
        {
            Image = ContainerImage.FromAsset("../../LiveEventService.API", new AssetImageProps
            {
                File = "Dockerfile"
            }),
            Logging = LogDriver.AwsLogs(new AwsLogDriverProps
            {
                StreamPrefix = "LiveEventAPI",
                LogRetention = RetentionDays.ONE_MONTH
            }),
            Environment = new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["AWS_REGION"] = this.Region,
                ["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "false",
                // OpenTelemetry export to local ADOT collector sidecar
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://127.0.0.1:4317",
                ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc",
                ["OTEL_SERVICE_NAME"] = "LiveEventService",
                ["AWS__SQS__UseSqsForDomainEvents"] = "true",
                ["Performance__BackgroundProcessing__UseInProcess"] = "false"
            },
            Secrets = new Dictionary<string, Amazon.CDK.AWS.ECS.Secret>
            {
                ["ConnectionStrings__DefaultConnection"] = Amazon.CDK.AWS.ECS.Secret.FromSecretsManager(
                    databaseCredentialsSecret,
                    "ConnectionStrings__DefaultConnection")
            }
        });

        container.AddPortMappings(new PortMapping
        {
            ContainerPort = 80,
            Protocol = Amazon.CDK.AWS.ECS.Protocol.TCP
        });

        // Add ADOT collector sidecar for traces and metrics export
        var adotCollectorConfig = @"receivers:
  otlp:
    protocols:
      grpc:
      http:
exporters:
  awsxray:
    region: ${AWS_REGION}
  awsemf:
    region: ${AWS_REGION}
processors:
  batch: {}
service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [awsxray]
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [awsemf]
";

        _ = taskDefinition.AddContainer("ADOTCollector", new ContainerDefinitionOptions
        {
            Image = ContainerImage.FromRegistry("public.ecr.aws/aws-observability/aws-otel-collector:latest"),
            Essential = true,
            Logging = LogDriver.AwsLogs(new AwsLogDriverProps
            {
                StreamPrefix = "ADOT"
            }),
            Environment = new Dictionary<string, string>
            {
                ["AWS_REGION"] = this.Region,
                ["AWS_OTEL_COLLECTOR_CONFIG_CONTENT"] = adotCollectorConfig
            }
        });

        // Amazon Managed Grafana workspace (deferred to reduce cost)

        // S3 bucket to host dashboard JSONs
        // Dashboards bucket (deferred)

        // Upload local dashboards as assets
        // Grafana dashboard assets (deferred)

        // Grant read to the Lambda provisioner
        // Asset grants (deferred)

        // Custom resource Lambda to import dashboards and create Prometheus datasource
        // Provisioner Lambda (deferred)

        // Permissions to call AWS Grafana API and read S3 assets
        // Provisioner permissions (deferred)

        // Custom resource that triggers the Lambda
        // Custom resource provider (deferred)

        // Grafana provisioning custom resource (deferred)

        // Request or create an ACM certificate for HTTPS
        var certificate = new Certificate(this, "LiveEventCertificate", new CertificateProps
        {
            DomainName = "*.example.com", // Replace with your domain
            Validation = CertificateValidation.FromDns() // DNS validation is recommended for production
        });

        // Parameters for API desired/min/max capacity (override per env at deploy time)
        var apiDesiredCountParam = new CfnParameter(this, "ApiDesiredCount", new CfnParameterProps
        {
            Type = "Number",
            Default = 1,
            MinValue = 0,
            MaxValue = 200,
            Description = "Desired number of API tasks"
        });
        var apiMinCapacityParam = new CfnParameter(this, "ApiMinCapacity", new CfnParameterProps
        {
            Type = "Number",
            Default = 0,
            MinValue = 0,
            MaxValue = 200,
            Description = "Minimum number of API tasks"
        });
        var apiMaxCapacityParam = new CfnParameter(this, "ApiMaxCapacity", new CfnParameterProps
        {
            Type = "Number",
            Default = 3,
            MinValue = 1,
            MaxValue = 500,
            Description = "Maximum number of API tasks"
        });

        // Create a load balanced Fargate service with HTTPS
        var service = new ApplicationLoadBalancedFargateService(this, "LiveEventService", new ApplicationLoadBalancedFargateServiceProps
        {
            Cluster = cluster,
            TaskDefinition = taskDefinition,
            DesiredCount = apiDesiredCountParam.ValueAsNumber,
            PublicLoadBalancer = true,
            AssignPublicIp = true,
            HealthCheckGracePeriod = Duration.Minutes(5),
            CircuitBreaker = new DeploymentCircuitBreaker
            {
                Rollback = true
            },
            Protocol = ApplicationProtocol.HTTPS,
            Certificate = certificate,
            RedirectHTTP = true, // Redirect HTTP to HTTPS
            SslPolicy = SslPolicy.TLS12, // Enforce TLS 1.2 or higher
            TargetProtocol = ApplicationProtocol.HTTP, // Internal communication can be HTTP
            TaskImageOptions = new ApplicationLoadBalancedTaskImageOptions
            {
                EnableLogging = true,
                LogDriver = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    LogRetention = RetentionDays.ONE_MONTH, // Reduced from ONE_YEAR for cost optimization
                    StreamPrefix = "LiveEventService"
                }),
                ContainerPort = 80,
                Environment = new Dictionary<string, string>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Production",
                    ["ASPNETCORE_HTTPS_PORT"] = "443",
                    ["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "false"
                },
                Secrets = new Dictionary<string, Amazon.CDK.AWS.ECS.Secret>
                {
                    ["ConnectionStrings__DefaultConnection"] = Amazon.CDK.AWS.ECS.Secret.FromSecretsManager(
                        databaseCredentialsSecret,
                        "ConnectionStrings__DefaultConnection")
                }
            }
        });

        // Dedicated CloudWatch Log Group for audit logs with 90-day retention
        var auditLogGroup = new LogGroup(this, "AuditLogGroup", new LogGroupProps
        {
            LogGroupName = "/live-event-service/audit",
            Retention = RetentionDays.THREE_MONTHS,
            RemovalPolicy = RemovalPolicy.RETAIN
        });

        // Configure auto scaling with cost optimization
        var scaling = service.Service.AutoScaleTaskCount(new EnableScalingProps
        {
            MinCapacity = apiMinCapacityParam.ValueAsNumber,
            MaxCapacity = apiMaxCapacityParam.ValueAsNumber
        });

        scaling.ScaleOnCpuUtilization("CpuScaling", new Amazon.CDK.AWS.ECS.CpuUtilizationScalingProps
        {
            TargetUtilizationPercent = 70,
            ScaleInCooldown = Duration.Minutes(3),
            ScaleOutCooldown = Duration.Minutes(1)
        });

        // Replace the obsolete method call with the recommended property access
        var reqPerTargetMetric = service.TargetGroup.Metrics.RequestCount(new Amazon.CDK.AWS.CloudWatch.MetricOptions
        {
            Period = Duration.Minutes(1)
        });

        scaling.ScaleToTrackCustomMetric("RequestCountScaling", new Amazon.CDK.AWS.ECS.TrackCustomMetricProps
        {
            TargetValue = 1000,
            Metric = reqPerTargetMetric
        });

        // Allow the service to access the database
        database.Connections.AllowDefaultPortFrom(service.Service.Connections, "Allow database access from ECS service");

        // Create a Web Application Firewall (WAF) for the ALB
        var webAcl = new CfnWebACL(this, "LiveEventWebACL", new CfnWebACLProps
        {
            DefaultAction = new CfnWebACL.RuleActionProperty { Allow = new object() },
            Scope = "REGIONAL",
            VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
            {
                CloudWatchMetricsEnabled = true,
                MetricName = "LiveEventWebACLMetrics",
                SampledRequestsEnabled = true
            },
            Rules = new[]
            {
                // AWS Managed Rules for common threats
                new CfnWebACL.RuleProperty
                {
                    Name = "AWSManagedRulesCommonRuleSet",
                    Priority = 1,
                    Statement = new CfnWebACL.StatementProperty
                    {
                        ManagedRuleGroupStatement = new CfnWebACL.ManagedRuleGroupStatementProperty
                        {
                            VendorName = "AWS",
                            Name = "AWSManagedRulesCommonRuleSet"
                        }
                    },
                    OverrideAction = new CfnWebACL.OverrideActionProperty { None = new object() },
                    VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
                    {
                        CloudWatchMetricsEnabled = true,
                        MetricName = "AWSManagedRulesCommonRuleSetMetric",
                        SampledRequestsEnabled = true
                    }
                },
                // Rate-based rule for DDoS protection
                new CfnWebACL.RuleProperty
                {
                    Name = "RateLimitRule",
                    Priority = 2,
                    Action = new CfnWebACL.RuleActionProperty { Block = new object() },
                    Statement = new CfnWebACL.StatementProperty
                    {
                        RateBasedStatement = new CfnWebACL.RateBasedStatementProperty
                        {
                            Limit = 2000, // Maximum requests per 5 minutes per IP
                            AggregateKeyType = "IP"
                        }
                    },
                    VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
                    {
                        CloudWatchMetricsEnabled = true,
                        MetricName = "RateLimitRuleMetric",
                        SampledRequestsEnabled = true
                    }
                }
            }
        });

        // Associate the WAF with the Application Load Balancer
        _ = new CfnWebACLAssociation(this, "WebAclAlbAssociation", new CfnWebACLAssociationProps
        {
            ResourceArn = service.LoadBalancer.LoadBalancerArn,
            WebAclArn = webAcl.AttrArn
        });

        // Output important values
        new CfnOutput(this, "UserPoolId", new CfnOutputProps { Value = userPool.UserPoolId });
        new CfnOutput(this, "UserPoolClientId", new CfnOutputProps { Value = userPoolClient.UserPoolClientId });
        new CfnOutput(this, "IdentityPoolId", new CfnOutputProps { Value = identityPool.Ref });
        new CfnOutput(this, "AlbDnsName", new CfnOutputProps { Value = service.LoadBalancer.LoadBalancerDnsName });
        new CfnOutput(this, "DomainEventsQueueUrl", new CfnOutputProps { Value = domainEventsQueue.QueueUrl });
        new CfnOutput(this, "DomainEventsDlqUrl", new CfnOutputProps { Value = dlq.QueueUrl });

        // Set up monitoring
        var monitoring = new MonitoringConstruct(this, "Monitoring", new MonitoringConstructProps
        {
            EcsService = service,
            Database = database,
            AlarmEmail = "alerts@example.com" // Replace with actual alert email
        });

        // Wire SQS alarms to existing SNS alarm topic
        var sqsAlarmAction = new Amazon.CDK.AWS.CloudWatch.Actions.SnsAction(monitoring.AlarmTopic);
        sqsOldestAgeAlarm.AddAlarmAction(sqsAlarmAction);
        sqsDepthAlarm.AddAlarmAction(sqsAlarmAction);
        sqsDlqAlarm.AddAlarmAction(sqsAlarmAction);
    }
}
