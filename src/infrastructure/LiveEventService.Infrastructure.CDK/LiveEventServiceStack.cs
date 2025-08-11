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

namespace LiveEventService.Infrastructure.CDK;

public class LiveEventServiceStack : Stack
{
    internal LiveEventServiceStack(Construct scope, string id, IStackProps? props = null)
        : base(scope, id, props)
    {
        // Create a VPC
        var vpc = new Vpc(this, "LiveEventVPC", new VpcProps
        {
            MaxAzs = 2,
            NatGateways = 1,
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

        // Add container to the task definition
        var container = taskDefinition.AddContainer("LiveEventAPI", new ContainerDefinitionOptions
        {
            Image = ContainerImage.FromAsset("../../LiveEventService.API", new AssetImageProps
            {
                File = "Dockerfile"
            }),
            Logging = LogDriver.AwsLogs(new AwsLogDriverProps
            {
                StreamPrefix = "LiveEventAPI"
            }),
            Environment = new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["AWS_REGION"] = this.Region,
                ["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "false"
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

        // Request or create an ACM certificate for HTTPS
        var certificate = new Certificate(this, "LiveEventCertificate", new CertificateProps
        {
            DomainName = "*.example.com", // Replace with your domain
            Validation = CertificateValidation.FromDns() // DNS validation is recommended for production
        });

        // Create a load balanced Fargate service with HTTPS
        var service = new ApplicationLoadBalancedFargateService(this, "LiveEventService", new ApplicationLoadBalancedFargateServiceProps
        {
            Cluster = cluster,
            TaskDefinition = taskDefinition,
            DesiredCount = 2,
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

        // Configure auto scaling with cost optimization
        var scaling = service.Service.AutoScaleTaskCount(new EnableScalingProps
        {
            MinCapacity = 1,  // Reduced from 2 for cost optimization
            MaxCapacity = 5   // Reduced from 10 for cost optimization
        });

        scaling.ScaleOnCpuUtilization("CpuScaling", new Amazon.CDK.AWS.ECS.CpuUtilizationScalingProps
        {
            TargetUtilizationPercent = 70,
            ScaleInCooldown = Duration.Minutes(3),
            ScaleOutCooldown = Duration.Minutes(1)
        });

        // Allow the service to access the database
        database.Connections.AllowDefaultPortFrom(service.Service.Connections, "Allow database access from ECS service");

        // Create a usage plan for API Gateway throttling
        var api = new RestApi(this, "LiveEventAPI", new RestApiProps
        {
            RestApiName = "Live Event Service API",
            Description = "API for the Live Event Service",
            DefaultCorsPreflightOptions = new CorsOptions
            {
                AllowOrigins = new[] { "*" },
                AllowMethods = new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" },
                AllowHeaders = new[] { "Content-Type", "Authorization" },
                MaxAge = Duration.Days(1)
            },
            DeployOptions = new StageOptions
            {
                StageName = "v1",
                ThrottlingRateLimit = 100,  // Steady-state rate (requests per second)
                ThrottlingBurstLimit = 200, // Burst capacity
                TracingEnabled = true,
                LoggingLevel = MethodLoggingLevel.INFO,
                DataTraceEnabled = true,
                MetricsEnabled = true
            },
            // Enable request validation
            DefaultMethodOptions = new MethodOptions
            {
                RequestValidatorOptions = new RequestValidatorOptions
                {
                    ValidateRequestBody = true,
                    ValidateRequestParameters = true
                }
            }
        });

        // Add a usage plan with throttling
        var plan = api.AddUsagePlan("LiveEventServiceUsagePlan", new UsagePlanProps
        {
            Name = "LiveEventServiceUsagePlan",
            Throttle = new ThrottleSettings
            {
                RateLimit = 100,    // Requests per second
                BurstLimit = 200    // Maximum burst capacity
            },
            Quota = new QuotaSettings
            {
                Limit = 10000,      // Total requests per month
                Period = Period.MONTH,
                Offset = 0
            }
        });

        // Apply the usage plan to all API methods
        plan.AddApiStage(new UsagePlanPerApiStage
        {
            Api = api,
            Stage = api.DeploymentStage
        });

        // Create a Web Application Firewall (WAF) for the API Gateway
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

        // Associate the WAF with the API Gateway
        var webAclAssociation = new CfnWebACLAssociation(this, "WebAclAssociation", new CfnWebACLAssociationProps
        {
            ResourceArn = $"arn:aws:apigateway:{this.Region}::/restapis/{api.RestApiId}/stages/{api.DeploymentStage.StageName}",
            WebAclArn = webAcl.AttrArn
        });

        // Add a resource proxy to the ECS service
        var proxy = api.Root.AddResource("{proxy+}");
        proxy.AddMethod(
            "ANY",
            new HttpIntegration($"http://{service.LoadBalancer.LoadBalancerDnsName}/{proxy}", new HttpIntegrationProps
            {
                HttpMethod = "ANY",
                Proxy = true,
                Options = new IntegrationOptions
                {
                    RequestParameters = new Dictionary<string, string>
                    {
                        ["integration.request.path.proxy"] = "method.request.path.proxy",
                        ["integration.request.header.X-Forwarded-For"] = "context.identity.sourceIp"
                    }
                }
            }),
            new MethodOptions
            {
                AuthorizationType = AuthorizationType.COGNITO,
                Authorizer = new CognitoUserPoolsAuthorizer(this, "CognitoAuthorizer", new CognitoUserPoolsAuthorizerProps
                {
                    CognitoUserPools = new[] { userPool }
                })
            }
        );

        // Output important values
        new CfnOutput(this, "UserPoolId", new CfnOutputProps { Value = userPool.UserPoolId });
        new CfnOutput(this, "UserPoolClientId", new CfnOutputProps { Value = userPoolClient.UserPoolClientId });
        new CfnOutput(this, "IdentityPoolId", new CfnOutputProps { Value = identityPool.Ref });
        new CfnOutput(this, "ApiUrl", new CfnOutputProps { Value = api.Url });
        new CfnOutput(this, "ApiId", new CfnOutputProps { Value = api.RestApiId });

        // Set up monitoring
        _ = new MonitoringConstruct(this, "Monitoring", new MonitoringConstructProps
        {
            ApiGateway = api,
            EcsService = service,
            Database = database,
            AlarmEmail = "alerts@example.com" // Replace with actual alert email
        });
    }
}