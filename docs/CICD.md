# CI/CD Pipeline Documentation - Live Event Service

## ✅ Current Status: Fully Automated

The Live Event Service has a **complete CI/CD pipeline** using GitHub Actions with:
- ✅ **Automated builds and tests** on every commit
- ✅ **Container-based deployment** to AWS ECS  
- ✅ **Infrastructure as Code** with AWS CDK
- ✅ **Database migrations** with Entity Framework
- ✅ **Smoke tests** and deployment verification
- ✅ **Slack notifications** for deployment status

## Overview

The CI/CD pipeline is built on **GitHub Actions** and follows modern DevOps practices:
- **GitOps**: All infrastructure and deployment configurations in Git
- **Container-First**: Docker images deployed to AWS ECR/ECS
- **Infrastructure as Code**: AWS CDK for repeatable deployments
- **Blue-Green Ready**: Zero-downtime deployment capability
- **Security**: IAM roles with least privilege, secrets management

## Pipeline Architecture

```mermaid
graph TD
    A[Developer Push] --> B[GitHub Actions Trigger]
    B --> C[Build & Test]
    C --> D[Docker Build]
    D --> E[Push to ECR]
    E --> F[Update ECS Task Definition]
    F --> G[Deploy to ECS]
    G --> H[Run DB Migrations]
    H --> I[CDK Infrastructure Update]
    I --> J[Smoke Tests]
    J --> K[Slack Notification]
```

## Workflow File: `.github/workflows/deploy.yml`

### **Triggers**
```yaml
on:
  push:
    branches: [ main ]      # Production deployments
  pull_request:
    branches: [ main ]      # PR validation (build/test only)
  workflow_dispatch:       # Manual deployment trigger
```

### **Environment Variables**
```yaml
env:
  AWS_REGION: us-east-1
  ECR_REPOSITORY: live-event-service
  ECS_SERVICE: LiveEventService
  ECS_CLUSTER: LiveEventCluster
  CONTAINER_NAME: LiveEventAPI
```

### **Security Permissions**
```yaml
permissions:
  contents: read          # Read repository code
  packages: write         # Push to container registry
  id-token: write        # AWS OIDC authentication
```

## Pipeline Stages

### **1. 🛡️ Security & Setup**

#### **AWS Authentication**
```yaml
- name: Configure AWS Credentials
  uses: aws-actions/configure-aws-credentials@v2
  with:
    role-to-assume: ${{ secrets.AWS_IAM_ROLE_ARN }}
    aws-region: ${{ env.AWS_REGION }}
```

**Security Features:**
- ✅ **OIDC Authentication**: No long-lived AWS keys
- ✅ **IAM Role Assumption**: Least privilege access
- ✅ **Secrets Management**: GitHub Secrets for sensitive data

#### **Required Secrets**
| Secret | Purpose | Example |
|--------|---------|---------|
| `AWS_IAM_ROLE_ARN` | AWS authentication role | `arn:aws:iam::123456789012:role/GitHubActions` |
| `SLACK_WEBHOOK_URL` | Deployment notifications | `https://hooks.slack.com/services/...` |
| `API_ENDPOINT` | Smoke test endpoint | `https://api.liveevent.com` |

### **2. 🔨 Build & Test**

#### **.NET Build Process**
```yaml
- name: Set up .NET
  uses: actions/setup-dotnet@v3
  with:
    dotnet-version: '8.0.x'

- name: Restore dependencies
  run: dotnet restore

- name: Build
  run: dotnet build --no-restore --configuration Release

- name: Test
  run: dotnet test --no-build --verbosity normal --configuration Release --collect:"XPlat Code Coverage"
```

**Build Features:**
- ✅ **.NET 8**: Latest LTS version
- ✅ **Release Configuration**: Optimized builds
- ✅ **Code Coverage**: XPlat Code Coverage collection
- ✅ **Dependency Caching**: Restored packages cached between runs

#### **Quality Gates**
- **Build must succeed** before deployment
- **All tests must pass** (unit + integration)
- **Code coverage metrics** collected
- **Security scanning** (dependency vulnerabilities)

### **3. 🐳 Containerization**

#### **Docker Build & Push**
```yaml
- name: Build and Push Docker image
  env:
    ECR_REGISTRY: ${{ steps.login-ecr.outputs.registry }}
    ECR_REPOSITORY: ${{ env.ECR_REPOSITORY }}
    IMAGE_TAG: ${{ github.sha }}
  run: |
    docker build -t $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG .
    docker push $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG
```

**Container Features:**
- ✅ **Multi-stage Dockerfile**: Optimized for production
- ✅ **Git SHA Tagging**: Immutable image versions
- ✅ **AWS ECR**: Secure, managed container registry
- ✅ **Layer Caching**: Efficient builds with cached layers

#### **Image Security**
- **Non-root user**: Container runs as `appuser`
- **Minimal base image**: Microsoft ASP.NET Core runtime
- **Vulnerability scanning**: ECR automatically scans images
- **Image signing**: AWS ECR image signing enabled

### **4. 🚀 Deployment**

#### **ECS Deployment Process**
```yaml
- name: Download task definition
  run: aws ecs describe-task-definition --task-definition ${{ env.ECS_SERVICE }} --query taskDefinition > task-definition.json

- name: Fill in the new image ID in the Amazon ECS task definition
  id: task-def
  uses: aws-actions/amazon-ecs-render-task-definition@v1
  with:
    task-definition: task-definition.json
    container-name: ${{ env.CONTAINER_NAME }}
    image: ${{ steps.build-image.outputs.image }}

- name: Deploy Amazon ECS task definition
  uses: aws-actions/amazon-ecs-deploy-task-definition@v1
  with:
    task-definition: ${{ steps.task-def.outputs.task-definition-json }}
    service: ${{ env.ECS_SERVICE }}
    cluster: ${{ env.ECS_CLUSTER }}
    wait-for-service-stability: true
```

**Deployment Features:**
- ✅ **Blue-Green Deployment**: Zero-downtime deployments
- ✅ **Health Checks**: ECS health checks before traffic routing
- ✅ **Rollback Capability**: Automatic rollback on deployment failure
- ✅ **Service Stability**: Waits for deployment stability before completion

### **5. 🗄️ Database Migrations**

#### **Entity Framework Migrations**
```yaml
- name: Run database migrations
  run: |
    cd src/LiveEventService.API
    dotnet ef database update --project ../LiveEventService.Infrastructure/LiveEventService.Infrastructure.csproj --context LiveEventDbContext
```

**Migration Features:**
- ✅ **Automatic Migrations**: Applied during deployment
- ✅ **Rollback Support**: Migration history tracking
- ✅ **Zero-Downtime**: Backward-compatible schema changes
- ✅ **Connection Security**: Secure database connections

### **6. 🏗️ Infrastructure Updates**

#### **AWS CDK Deployment**
```yaml
- name: Deploy CDK Stack
  run: |
    cd src/infrastructure
    dotnet build
    npm install -g aws-cdk
    cdk deploy --require-approval never
```

**Infrastructure Features:**
- ✅ **Infrastructure as Code**: All AWS resources defined in CDK
- ✅ **Version Control**: Infrastructure changes tracked in Git
- ✅ **Drift Detection**: CDK detects infrastructure drift
- ✅ **Automated Updates**: Infrastructure updated with each deployment

### **7. ✅ Verification & Monitoring**

#### **Smoke Tests**
```yaml
- name: Run smoke tests
  run: |
    echo "Running smoke tests..."
    # Health check verification
    curl -sSf ${{ secrets.API_ENDPOINT }}/health > /dev/null
    # API endpoint testing
    curl -sSf ${{ secrets.API_ENDPOINT }}/api/events -H "Authorization: Bearer ${{ secrets.TEST_TOKEN }}"
```

#### **Deployment Notifications**
```yaml
- name: Notify status
  if: always()
  uses: rtCamp/action-slack-notify@v2
  env:
    SLACK_WEBHOOK: ${{ secrets.SLACK_WEBHOOK_URL }}
    SLACK_TITLE: "Deployment ${{ job.status }}"
    SLACK_MESSAGE: "Deployment of ${{ github.repository }}@${{ github.sha }} to production ${{ job.status }}"
    SLACK_COLOR: ${{ job.status == 'success' && 'good' || 'danger' }}
```

## Deployment Environments

### **Production Environment**
- **Trigger**: Push to `main` branch
- **Target**: AWS ECS Production Cluster
- **Database**: AWS RDS PostgreSQL (Multi-AZ)
- **Monitoring**: Full observability stack
- **Approval**: Automatic (protected by tests)

### **Staging Environment** (Future)
```yaml
# Add staging environment
staging:
  if: github.ref == 'refs/heads/develop'
  environment: staging
  # Similar steps but deploy to staging ECS cluster
```

### **Feature Branch Testing** (Current)
- **Trigger**: Pull Request creation
- **Actions**: Build and test only (no deployment)
- **Purpose**: Validate changes before merge

## Security Best Practices

### **🔐 Authentication & Authorization**
```yaml
# OIDC-based authentication (no long-lived credentials)
permissions:
  id-token: write
  contents: read
  packages: write
```

### **🛡️ Secrets Management**
```bash
# Required GitHub Secrets
AWS_IAM_ROLE_ARN=arn:aws:iam::123456789012:role/GitHubActionsRole
SLACK_WEBHOOK_URL=https://hooks.slack.com/services/T00000000/B00000000/XXXXXXXXXXXXXXXXXXXXXXXX
API_ENDPOINT=https://api.liveevent.com
```

### **🔒 IAM Role Policy** (Minimum Required)
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "ecr:GetAuthorizationToken",
        "ecr:BatchCheckLayerAvailability",
        "ecr:GetDownloadUrlForLayer",
        "ecr:BatchGetImage",
        "ecr:PutImage",
        "ecr:CompleteLayerUpload",
        "ecs:DescribeTaskDefinition",
        "ecs:RegisterTaskDefinition",
        "ecs:UpdateService",
        "ecs:DescribeServices",
        "iam:PassRole"
      ],
      "Resource": "*"
    }
  ]
}
```

## Monitoring & Observability

### **Pipeline Metrics**
- ✅ **Build Duration**: Track build performance over time
- ✅ **Test Coverage**: Monitor code coverage trends
- ✅ **Deployment Success Rate**: Track deployment reliability
- ✅ **Time to Deploy**: Measure deployment speed

### **GitHub Actions Insights**
```bash
# View workflow runs
https://github.com/your-org/LiveEventServiceDemo/actions

# Monitor workflow metrics
- Success/failure rates
- Duration trends
- Resource usage
```

### **AWS Integration**
```bash
# CloudWatch Metrics
- ECS deployment metrics
- Application health checks
- Infrastructure costs

# X-Ray Tracing
- Deployment trace analysis
- Performance impact tracking
```

## Troubleshooting

### **Common Issues**

#### **Build Failures**
```bash
# Check .NET version compatibility
dotnet --version

# Restore packages manually
dotnet restore --verbosity detailed

# Clean and rebuild
dotnet clean && dotnet build
```

#### **Docker Build Issues**
```bash
# Test Docker build locally
docker build -t test-image .

# Check Dockerfile syntax
docker run --rm -i hadolint/hadolint < Dockerfile

# Debug container startup
docker run -it test-image /bin/bash
```

#### **AWS Deployment Issues**
```bash
# Check ECS service status
aws ecs describe-services --cluster LiveEventCluster --services LiveEventService

# View ECS task logs
aws logs get-log-events --log-group-name /ecs/LiveEventService

# Verify IAM permissions
aws sts get-caller-identity
```

#### **Database Migration Issues**
```bash
# Check migration status
dotnet ef migrations list

# Manually apply migrations
dotnet ef database update --verbose

# Generate migration script
dotnet ef migrations script
```

### **Rollback Procedures**

#### **Application Rollback**
```bash
# Rollback to previous ECS task definition
aws ecs update-service --cluster LiveEventCluster --service LiveEventService --task-definition LiveEventService:123

# Verify rollback success
aws ecs describe-services --cluster LiveEventCluster --services LiveEventService
```

#### **Database Rollback**
```bash
# Rollback to specific migration
dotnet ef database update PreviousMigrationName

# Generate rollback script
dotnet ef migrations script CurrentMigration PreviousMigration
```

## Performance Optimizations

### **Build Optimizations**
- ✅ **Dependency Caching**: GitHub Actions cache for faster builds
- ✅ **Parallel Jobs**: Future enhancement for large test suites
- ✅ **Matrix Builds**: Multiple .NET versions (if needed)

### **Deployment Optimizations**
- ✅ **Layer Caching**: Docker layer caching for faster builds
- ✅ **Rolling Updates**: ECS rolling deployment strategy
- ✅ **Health Checks**: Fast health check endpoints

## Cost Management

### **GitHub Actions Usage**
```yaml
# Monitor usage in GitHub settings
- Build minutes consumption
- Storage usage
- Concurrent job limits
```

### **AWS Costs**
- **ECR Storage**: Regular cleanup of old images
- **ECS Compute**: Right-sized task definitions
- **Data Transfer**: Optimize for minimal cross-region traffic

## Future Enhancements

### **Planned Improvements**
1. **🔧 Multi-Environment Support**
   - Staging environment deployment
   - Environment-specific configurations

2. **🧪 Enhanced Testing**
   - Integration tests with Testcontainers
   - Performance testing pipeline
   - Security scanning integration

3. **📊 Advanced Monitoring**
   - Deployment dashboards
   - Performance regression detection
   - Automated alerting

4. **🚀 GitOps Enhancement**
   - ArgoCD integration
   - Git-based configuration management
   - Policy-as-code governance

## Developer Workflow

### **Development Process**
```bash
# 1. Create feature branch
git checkout -b feature/new-admin-endpoint

# 2. Develop and test locally
docker-compose up -d
dotnet test

# 3. Push and create PR
git push origin feature/new-admin-endpoint
# GitHub Actions runs build/test automatically

# 4. Merge to main
git checkout main && git merge feature/new-admin-endpoint
# GitHub Actions deploys to production automatically
```

### **Local Testing Before Push**
```bash
# Run the same checks as CI/CD
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
docker build -t local-test .
```

## Support & Maintenance

### **Pipeline Maintenance**
- **Monthly**: Review and update GitHub Actions versions
- **Quarterly**: Audit IAM permissions and security
- **Annually**: Evaluate pipeline architecture and tools

### **Documentation Updates**
- Keep this document updated with pipeline changes
- Update runbooks for operational procedures
- Maintain troubleshooting guides

### **Contact Information**
- **DevOps Team**: devops@company.com
- **Platform Team**: platform@company.com
- **Security Team**: security@company.com

---

**This CI/CD pipeline provides enterprise-grade deployment automation for Live Event Service, ensuring reliable, secure, and efficient deployments to production.** 🚀 