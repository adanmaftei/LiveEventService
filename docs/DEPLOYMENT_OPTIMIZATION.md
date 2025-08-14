# Deployment Optimization Guide

This document outlines deployment optimization strategies for the Live Event Service to improve reliability, performance, and operational efficiency.

## Current Deployment Assessment

### ✅ Well-Implemented Features
- **Infrastructure as Code**: AWS CDK for reproducible deployments
- **CI/CD Pipeline**: GitHub Actions with automated testing and deployment
- **Containerization**: Docker with multi-stage builds
- **Auto-scaling**: ECS Fargate with CPU and ALB request-based scaling
- **Load balancing**: Application Load Balancer with health checks (WAF associated)
- **Security**: WAF, Cognito, encryption at rest and in transit
- **Monitoring**: CloudWatch dashboards and alarms

### ⚠️ Deployment Limitations
- **Single region by default**: The stack deploys to one region with Multi-AZ RDS for HA. Multi‑region is available via parameters: deploy a secondary `LiveEventReplicaStack` and configure Route 53 failover records; optionally enable Aurora PostgreSQL Global Database.
- **Blue/green optional (canary not built-in)**: Rolling updates are default. Blue/green with traffic shifting is available when `EnableBlueGreen=true` (ECS CodeDeploy). Canary via weighted routing is not provided out‑of‑the‑box.
- **Rollback**: CodeDeploy blue/green provides health‑based automated rollback. Alarm‑driven rollbacks and advanced policies are not configured by default.
- **Limited experimentation**: A/B testing and feature flags are not included.

## High-Priority Deployment Optimizations

### Parameterizing service capacity (CDK parameters)

You can tune API and Worker capacity per environment at deploy time via CDK parameters.

- API parameters: `ApiDesiredCount`, `ApiMinCapacity`, `ApiMaxCapacity`
- Worker parameters: `WorkerDesiredCount`, `WorkerMinCapacity`, `WorkerMaxCapacity`

Recommended per-environment starting points:
- Dev: API (desired=1, min=0, max=2); Worker (desired=0, min=0, max=2)
- Staging: API (desired=1, min=1, max=4); Worker (desired=1, min=0, max=4)
- Prod: API (desired=2, min=2, max=10); Worker (desired=1, min=1, max=10)

CDK deploy examples:

```bash
# API-only overrides
cdk deploy LiveEventServiceStack \
  --parameters ApiDesiredCount=2 \
  --parameters ApiMinCapacity=1 \
  --parameters ApiMaxCapacity=8

# Worker-only overrides
cdk deploy LiveEventServiceStack \
  --parameters WorkerDesiredCount=1 \
  --parameters WorkerMinCapacity=0 \
  --parameters WorkerMaxCapacity=6

# Override both
cdk deploy LiveEventServiceStack \
  --parameters ApiDesiredCount=2 ApiMinCapacity=2 ApiMaxCapacity=10 \
  --parameters WorkerDesiredCount=1 WorkerMinCapacity=1 WorkerMaxCapacity=10
```

GitHub Actions snippet:

```yaml
- name: CDK Deploy (Prod)
  run: |
    cdk deploy LiveEventServiceStack \
      --require-approval never \
      --parameters ApiDesiredCount=2 \
      --parameters ApiMinCapacity=2 \
      --parameters ApiMaxCapacity=10 \
      --parameters WorkerDesiredCount=1 \
      --parameters WorkerMinCapacity=1 \
      --parameters WorkerMaxCapacity=10
```

Notes:
- These parameters are surfaced in `LiveEventServiceStack.cs` and control ECS Service desired count and autoscaling bounds.
- Pair capacity settings with autoscaling policies (CPU and ALB request-count-per-target) already configured in the stack.
- Review CloudWatch alarms and adjust thresholds if you significantly change capacity.

### 1. Blue-Green Deployment Strategy (available via parameter)

Enable blue/green with ECS CodeDeploy by setting the CDK context parameter `EnableBlueGreen=true`. You can control traffic shifting with `-c CodeDeployShiftingConfig=<strategy>` (for example, `Linear10PercentEvery5Minutes`). See the "Progressive Rollouts" section below for CLI examples.

### 2. Canary Deployment Strategy (future consideration)

#### Implementation
The current stack uses ALB + ECS Fargate. Canary can be approximated with ALB weighted target groups or additional listeners; not provisioned out of the box.

#### GitHub Actions Integration
```yaml
# Enhanced deployment workflow
- name: Deploy Canary
  run: |
    # Deploy to canary environment first
    aws ecs update-service --cluster $ECS_CLUSTER --service $ECS_SERVICE --task-definition $TASK_DEFINITION_ARN
    
    # Wait for canary to be healthy
    aws ecs wait services-stable --cluster $ECS_CLUSTER --services $ECS_SERVICE
    
    # Shift 10% traffic to canary
    aws elbv2 modify-listener --listener-arn $LISTENER_ARN --default-actions Type=forward,TargetGroupArn=$CANARY_TARGET_GROUP_ARN
    
    # Monitor canary for 5 minutes
    sleep 300
    
    # If healthy, shift 50% traffic
    aws elbv2 modify-listener --listener-arn $LISTENER_ARN --default-actions Type=forward,TargetGroupArn=$CANARY_TARGET_GROUP_ARN
    
    # Monitor for 5 more minutes
    sleep 300
    
    # If still healthy, shift 100% traffic
    aws elbv2 modify-listener --listener-arn $LISTENER_ARN --default-actions Type=forward,TargetGroupArn=$CANARY_TARGET_GROUP_ARN
```

### 3. Multi-Region Deployment (optional)

Deploy a primary stack in one region and the `LiveEventReplicaStack` in a second region, then create Route 53 failover alias records pointing to each ALB. Pass your existing `HostedZoneId` and `DnsRecordName` via CDK context parameters. See the examples below.

### 4. Automated Rollback Strategy (future consideration)

Alarm‑driven or custom rollback workflows (beyond CodeDeploy health checks) can be added with CloudWatch alarms on ECS/ALB metrics and an automation target (e.g., Step Functions or Lambda) to initiate rollback.

### 5. Feature Flag Implementation

#### AWS AppConfig Integration
```csharp
// Create feature flag configuration
var application = new CfnApplication(this, "FeatureFlags", new CfnApplicationProps
{
    Name = "LiveEventService-Features"
});

var environment = new CfnEnvironment(this, "ProductionEnv", new CfnEnvironmentProps
{
    ApplicationId = application.Ref,
    Name = "production"
});

var configurationProfile = new CfnConfigurationProfile(this, "FeatureFlagsProfile", new CfnConfigurationProfileProps
{
    ApplicationId = application.Ref,
    Name = "feature-flags",
    LocationUri = "hosted"
});
```

#### Application Integration
```csharp
// Add feature flag service to DI container
services.AddSingleton<IFeatureFlagService, AppConfigFeatureFlagService>();

// Use feature flags in application
public class EventService
{
    private readonly IFeatureFlagService _featureFlags;
    
    public async Task<Event> GetEventAsync(Guid id)
    {
        if (await _featureFlags.IsEnabledAsync("new-caching-strategy"))
        {
            return await GetEventWithNewCachingAsync(id);
        }
        return await GetEventWithLegacyCachingAsync(id);
    }
}
```

## Medium-Priority Optimizations

### 6. Database Migration Strategy

#### Zero-Downtime Schema Changes
```csharp
// Implement database migration strategy
public class DatabaseMigrationService
{
    public async Task MigrateSchemaAsync(string migrationName)
    {
        // 1. Create new table with new schema
        await CreateNewTableAsync();
        
        // 2. Copy data from old table to new table
        await CopyDataAsync();
        
        // 3. Verify data integrity
        await VerifyDataIntegrityAsync();
        
        // 4. Switch application to use new table
        await SwitchToNewTableAsync();
        
        // 5. Drop old table
        await DropOldTableAsync();
    }
}
```

### 7. Configuration Management

#### AWS Systems Manager Parameter Store
```csharp
// Store configuration in Parameter Store
var appConfig = new CfnParameter(this, "AppConfig", new CfnParameterProps
{
    Name = "/liveeventservice/production/config",
    Type = "String",
    Value = JsonSerializer.Serialize(new
    {
        DatabaseConnectionString = databaseCredentialsSecret.SecretValueFromJson("connectionString"),
        ApiRateLimit = 100,
        CacheTimeout = 300
    })
});
```

### 8. Deployment Monitoring

#### Enhanced CloudWatch Dashboards
```csharp
var deploymentDashboard = new Dashboard(this, "DeploymentDashboard", new DashboardProps
{
    DashboardName = "LiveEventService-Deployments",
    Widgets = new[]
    {
        new GraphWidget(new GraphWidgetProps
        {
            Title = "Deployment Success Rate",
            Left = new[]
            {
                new Metric(new MetricProps
                {
                    Namespace = "AWS/ECS",
                    MetricName = "DeploymentSuccess",
                    DimensionsMap = new Dictionary<string, string>
                    {
                        ["ClusterName"] = cluster.ClusterName,
                        ["ServiceName"] = service.ServiceName
                    }
                })
            }
        }),
        new GraphWidget(new GraphWidgetProps
        {
            Title = "Rollback Events",
            Left = new[]
            {
                new Metric(new MetricProps
                {
                    Namespace = "AWS/ECS",
                    MetricName = "DeploymentRollback",
                    DimensionsMap = new Dictionary<string, string>
                    {
                        ["ClusterName"] = cluster.ClusterName,
                        ["ServiceName"] = service.ServiceName
                    }
                })
            }
        })
    }
});
```

## Implementation Roadmap

### Phase 1 (Immediate - 2-4 weeks)
1. Implement blue-green deployment strategy
2. Add automated rollback capabilities
3. Enhance monitoring and alerting
4. Implement feature flags

### Phase 2 (Short-term - 1-2 months)
1. Implement canary or blue/green deployments (CodeDeploy + weighted target groups)
2. Add multi-region deployment and failover (Route 53 health checks, active/passive)
3. Implement zero-downtime database migrations
4. Add centralized configuration management (AppConfig/Parameter Store)

#### Secondary region deployment example
```bash
# Primary region deploy (e.g., us-east-1)
cd src/infrastructure
cdk deploy --require-approval never \
  -c HostedZoneId=Z123EXAMPLE \
  -c DnsRecordName=events.example.com \
  -c DnsFailoverRole=PRIMARY

# Secondary region deploy (e.g., eu-central-1)
setx CDK_DEFAULT_REGION eu-central-1
cdk deploy --require-approval never \
  -c HostedZoneId=Z123EXAMPLE \
  -c DnsRecordName=events.example.com \
  -c DnsFailoverRole=SECONDARY
```

### Phase 3 (Long-term - 2-3 months)
1. Implement advanced deployment strategies
2. Add A/B testing capabilities
3. Implement chaos engineering
4. Add advanced monitoring and observability

## Expected Benefits

### Deployment Reliability
- **Zero-downtime deployments**: 99.9%+ uptime during deployments
- **Automated rollback**: < 2 minutes to rollback failed deployments
- **Risk mitigation**: Gradual traffic shifting reduces deployment risk
- **Multi-region availability**: 99.99%+ uptime with failover capability

### Operational Efficiency
- **Faster deployments**: Automated processes reduce deployment time
- **Reduced manual intervention**: Automated rollback and monitoring
- **Better visibility**: Enhanced monitoring and alerting
- **Configuration management**: Centralized configuration with version control

### Risk Reduction
- **Feature flags**: Safe feature releases with instant rollback
- **Canary deployments**: Gradual rollout with monitoring
- **Blue-green deployments**: Easy rollback capability
- **Multi-region**: Disaster recovery and global availability

## Risk Mitigation

### Multi-Region Deployment Guidance
1. Deploy identical stacks per target region (e.g., `us-east-1`, `eu-central-1`).
2. Use Route 53 failover records (PRIMARY/SECONDARY) with health checks to `/health`.
3. Consider data strategy:
   - Read replicas with asynchronous replication
   - Aurora Global Database for lower RPO/RTO
   - Region-pinned data by market for data residency
4. This repo includes optional Route 53 alias record and health check parameters in the CDK (`HostedZoneId`, `DnsRecordName`, `DnsFailoverRole`) to accelerate setup.

## Progressive Rollouts (Blue/Green / Canary)
- Enable Blue/Green via CDK context parameter `EnableBlueGreen=true`.
- CDK provisions a test listener and an ECS CodeDeploy Deployment Group (using L1 resources for broad compatibility).
- Default deployment config is `ALL_AT_ONCE` (adjustable via `-c CodeDeployShiftingConfig=Linear10PercentEvery5Minutes`).

### Example
```bash
cd src/infrastructure
cdk deploy --require-approval never \
  -c EnableBlueGreen=true \
  -c CodeDeployShiftingConfig=Linear10PercentEvery5Minutes \
  -c HostedZoneId=Z123EXAMPLE \
  -c DnsRecordName=events.example.com \
  -c DnsFailoverRole=PRIMARY
```

### Aurora PostgreSQL Global
- Primary region: enable with `-c EnableAuroraGlobal=true -c AuroraGlobalClusterId=liveevent-global-cluster`.
- Secondary region: deploy `LiveEventReplicaStack` with the same `AuroraGlobalClusterId` to attach a replica cluster.
- Cutover: update the API/Worker DB connection secret to the Aurora writer endpoint.

### Testing Strategy
- **Staging environment**: Test all deployment strategies in staging
- **Load testing**: Validate performance under load
- **Chaos engineering**: Test failure scenarios
- **Rollback testing**: Ensure rollback procedures work

### Monitoring and Alerting
- **Real-time monitoring**: Monitor deployments in real-time
- **Automated alerts**: Alert on deployment failures
- **Performance tracking**: Track deployment performance metrics
- **User experience monitoring**: Monitor impact on end users

## Conclusion

By implementing these deployment optimizations, the Live Event Service can achieve enterprise-grade deployment capabilities with zero downtime, automated rollback, and multi-region availability. The phased approach ensures smooth implementation while maintaining system stability. 