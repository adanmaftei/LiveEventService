using Amazon.CDK;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.CloudWatch.Actions;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SNS.Subscriptions;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.RDS;
using Constructs;

namespace LiveEventService.Infrastructure.CDK;

public class MonitoringConstruct : Construct
{
    public ITopic AlarmTopic { get; private set; } = null!;
    public MonitoringConstruct(Construct scope, string id, MonitoringConstructProps props) : base(scope, id)
    {
        // Create SNS topic for alerts
        var alarmTopic = new Topic(this, "AlarmTopic", new TopicProps
        {
            DisplayName = "LiveEventServiceAlarms",
            TopicName = "LiveEventServiceAlarms"
        });

        AlarmTopic = alarmTopic;

        // Add email subscription if email is provided
        if (!string.IsNullOrEmpty(props.AlarmEmail))
        {
            alarmTopic.AddSubscription(new EmailSubscription(props.AlarmEmail));
        }

        // Create dashboard
        var dashboard = new Dashboard(this, "LiveEventDashboard", new DashboardProps
        {
            DashboardName = "LiveEventService-Dashboard",
            DefaultInterval = Duration.Days(7)
        });

        // API Gateway Widgets
        var apiGatewayWidgets = CreateApiGatewayWidgets(props.ApiGateway);
        dashboard.AddWidgets(apiGatewayWidgets);

        // ECS Service Widgets
        var ecsWidgets = CreateEcsWidgets(props.EcsService);
        dashboard.AddWidgets(ecsWidgets);

        // RDS Widgets
        var rdsWidgets = CreateRdsWidgets(props.Database);
        dashboard.AddWidgets(rdsWidgets);

        // Set up alarms
        SetupAlarms(props, alarmTopic);
    }

    private IWidget[] CreateApiGatewayWidgets(IRestApi api)
    {
        return new IWidget[]
        {
            new GraphWidget(new GraphWidgetProps
            {
                Title = "API Gateway - 4XX Errors",
                Left = new[] { new Metric(new MetricProps {
                    Namespace = "AWS/ApiGateway",
                    MetricName = "4XXError",
                    DimensionsMap = new Dictionary<string, string> { ["ApiName"] = api.RestApiName },
                    Statistic = "Sum",
                    Period = Duration.Minutes(1)
                })}
            }),
            new GraphWidget(new GraphWidgetProps
            {
                Title = "API Gateway - 5XX Errors",
                Left = new[] { new Metric(new MetricProps {
                    Namespace = "AWS/ApiGateway",
                    MetricName = "5XXError",
                    DimensionsMap = new Dictionary<string, string> { ["ApiName"] = api.RestApiName },
                    Statistic = "Sum",
                    Period = Duration.Minutes(1)
                })}
            }),
            new GraphWidget(new GraphWidgetProps
            {
                Title = "API Gateway - Latency",
                Left = new[] { new Metric(new MetricProps {
                    Namespace = "AWS/ApiGateway",
                    MetricName = "Latency",
                    DimensionsMap = new Dictionary<string, string> { ["ApiName"] = api.RestApiName },
                    Statistic = "p95",
                    Period = Duration.Minutes(1)
                })}
            })
        };
    }

    private IWidget[] CreateEcsWidgets(ApplicationLoadBalancedFargateService service)
    {
        // Get the cluster and service name from the Fargate service
        var clusterName = service.Service.Cluster.ClusterName;
        var serviceName = service.Service.ServiceName;

        return new IWidget[]
        {
            new GraphWidget(new GraphWidgetProps
            {
                Title = "ECS - CPU Utilization",
                Left = new[] {
                    service.Service.MetricCpuUtilization(new MetricOptions {
                        Statistic = "Average",
                        Period = Duration.Minutes(1)
                    })
                }
            }),
            new GraphWidget(new GraphWidgetProps
            {
                Title = "ECS - Memory Utilization",
                Left = new[] {
                    service.Service.MetricMemoryUtilization(new MetricOptions {
                        Statistic = "Average",
                        Period = Duration.Minutes(1)
                    })
                }
            }),
            new GraphWidget(new GraphWidgetProps
            {
                Title = "ECS - Running Tasks",
                Left = new[] {
                    new Metric(new MetricProps {
                        Namespace = "AWS/ECS",
                        MetricName = "RunningTaskCount",
                        DimensionsMap = new Dictionary<string, string>
                        {
                            { "ClusterName", clusterName },
                            { "ServiceName", serviceName }
                        },
                        Statistic = "Average",
                        Period = Duration.Minutes(1)
                    })
                }
            })
        };
    }

    private IWidget[] CreateRdsWidgets(IDatabaseInstance database)
    {
        return new IWidget[]
        {
            new GraphWidget(new GraphWidgetProps
            {
                Title = "RDS - CPU Utilization",
                Left = new[] {
                    database.MetricCPUUtilization(new MetricOptions {
                        Statistic = "Average",
                        Period = Duration.Minutes(1)
                    })
                }
            }),
            new GraphWidget(new GraphWidgetProps
            {
                Title = "RDS - Database Connections",
                Left = new[] {
                    database.MetricDatabaseConnections(new MetricOptions {
                        Statistic = "Average",
                        Period = Duration.Minutes(1)
                    })
                }
            }),
            new GraphWidget(new GraphWidgetProps
            {
                Title = "RDS - Storage",
                Left = new[] {
                    database.MetricFreeStorageSpace(new MetricOptions {
                        Statistic = "Average",
                        Period = Duration.Hours(1)
                    }),
                    database.MetricFreeableMemory(new MetricOptions {
                        Statistic = "Average",
                        Period = Duration.Hours(1)
                    })
                }
            })
        };
    }

    private void SetupAlarms(MonitoringConstructProps props, ITopic alarmTopic)
    {
        // API Gateway Alarms
        var api5xxAlarm = new Alarm(this, "Api5xxAlarm", new AlarmProps
        {
            AlarmDescription = "API Gateway 5XX errors > 1% of requests over 5 minutes",
            Metric = new Metric(new MetricProps
            {
                Namespace = "AWS/ApiGateway",
                MetricName = "5XXError",
                DimensionsMap = new Dictionary<string, string> { ["ApiName"] = props.ApiGateway.RestApiName },
                Statistic = "Sum",
                Period = Duration.Minutes(5)
            }),
            Threshold = 1,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD,
            TreatMissingData = TreatMissingData.NOT_BREACHING
        });

        // ECS Alarms
        var ecsCpuAlarm = new Alarm(this, "EcsCpuAlarm", new AlarmProps
        {
            AlarmDescription = "ECS CPU utilization > 80% for 5 minutes",
            Metric = props.EcsService.Service.MetricCpuUtilization(new MetricOptions
            {
                Statistic = "Average",
                Period = Duration.Minutes(1)
            }),
            Threshold = 80,
            EvaluationPeriods = 5,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD
        });

        var ecsMemoryAlarm = new Alarm(this, "EcsMemoryAlarm", new AlarmProps
        {
            AlarmDescription = "ECS memory utilization > 85% for 5 minutes",
            Metric = props.EcsService.Service.MetricMemoryUtilization(new MetricOptions
            {
                Statistic = "Average",
                Period = Duration.Minutes(1)
            }),
            Threshold = 85,
            EvaluationPeriods = 5,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD
        });

        // RDS Alarms
        var rdsCpuAlarm = new Alarm(this, "RdsCpuAlarm", new AlarmProps
        {
            AlarmDescription = "RDS CPU utilization > 80% for 15 minutes",
            Metric = props.Database.MetricCPUUtilization(new MetricOptions
            {
                Statistic = "Average",
                Period = Duration.Minutes(5)
            }),
            Threshold = 80,
            EvaluationPeriods = 3,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD
        });

        var rdsConnectionsAlarm = new Alarm(this, "RdsConnectionsAlarm", new AlarmProps
        {
            AlarmDescription = "RDS connections > 90% of max over 10 minutes",
            Metric = props.Database.MetricDatabaseConnections(new MetricOptions
            {
                Statistic = "Average",
                Period = Duration.Minutes(5)
            }),
            Threshold = 0, // set in real env using Parameter or dynamic lookup of max_connections
            EvaluationPeriods = 2,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD,
            TreatMissingData = TreatMissingData.NOT_BREACHING
        });

        // Cost guardrail: CloudWatch Logs retention already shorter; create 4XX rate warning alarm
        var api4xxAlarm = new Alarm(this, "Api4xxAlarm", new AlarmProps
        {
            AlarmDescription = "API Gateway 4XX errors > 5% over 5 minutes",
            Metric = new Metric(new MetricProps
            {
                Namespace = "AWS/ApiGateway",
                MetricName = "4XXError",
                DimensionsMap = new Dictionary<string, string> { ["ApiName"] = props.ApiGateway.RestApiName },
                Statistic = "Sum",
                Period = Duration.Minutes(5)
            }),
            Threshold = 5,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD,
            TreatMissingData = TreatMissingData.NOT_BREACHING
        });

        // Add actions to alarms
        var alarmAction = new SnsAction(alarmTopic);
        api5xxAlarm.AddAlarmAction(alarmAction);
        ecsCpuAlarm.AddAlarmAction(alarmAction);
        ecsMemoryAlarm.AddAlarmAction(alarmAction);
        rdsCpuAlarm.AddAlarmAction(alarmAction);
        rdsConnectionsAlarm.AddAlarmAction(alarmAction);
        api4xxAlarm.AddAlarmAction(alarmAction);
    }
}

public class MonitoringConstructProps
{
    public IRestApi ApiGateway { get; set; } = null!;
    public ApplicationLoadBalancedFargateService EcsService { get; set; } = null!;
    public IDatabaseInstance Database { get; set; } = null!;
    public string AlarmEmail { get; set; } = null!;
}