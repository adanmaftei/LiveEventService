## Deployment Checklist

This checklist describes exactly what to set up in AWS and GitHub to deploy the Live Event Service, plus the basic CLI path. Optional flags cover blue/green, multi‑region, and Aurora Global.

### 1) Prerequisites
- AWS Account with permissions to create IAM, VPC, ECS, RDS, ElastiCache, ALB, Route 53, CodeDeploy, CloudWatch, Secrets Manager
- AWS CLI v2 and credentials locally (if deploying from CLI)
- AWS CDK v2 installed (`npm i -g aws-cdk`)
- GitHub repository admin access (to configure OIDC and repo secrets/variables)
- Optional: Existing Route 53 hosted zone and a DNS name (only if you want a custom domain)

### 2) AWS Setup (OIDC for GitHub Actions)
Use an IAM role that GitHub Actions can assume via OpenID Connect. The role should have permissions to deploy CDK stacks (you can start with `AdministratorAccess` in non‑prod, then scope down).

Minimum steps:
1. In AWS IAM, create a role for web identity provider `token.actions.githubusercontent.com` with audience `sts.amazonaws.com`.
2. Add a trust condition that limits to your repository, e.g. `repo:owner/repo-name:ref:refs/heads/main`.
3. Attach a policy allowing CDK deployments (CloudFormation, ECR, ECS, ELBv2, RDS, EC2, IAM PassRole, Secrets Manager, Route 53, CloudWatch/Logs, CodeDeploy, X‑Ray). Start broad; refine later.

References: AWS docs “GitHub Actions OIDC to AWS” and `aws-actions/configure-aws-credentials` usage.

### 3) GitHub Repository Configuration
In “Settings → Actions → General”: allow GitHub Actions to create OIDC tokens (permissions: `Read id-token` for workflows) and enable workflows.

Add the following to “Settings → Secrets and variables → Actions”:

- Secrets/Variables for deploy.yml:
  - `AWS_ACCOUNT_ID` (variable)
  - `AWS_REGION` (variable), e.g. `eu-west-1`
  - `AWS_ROLE_TO_ASSUME` (secret or variable), e.g. `arn:aws:iam::<acct>:role/<your-oidc-role>`
  - `API_ENDPOINT` (secret) – set after first deploy to your ALB DNS or custom DNS for smoke test

- Optional context variables (Variables or pass via workflow inputs):
  - `EnableBlueGreen=true|false` (default false)
  - `CodeDeployShiftingConfig=AllAtOnce|Linear10PercentEvery5Minutes`
  - `HostedZoneId=ZXXXXXXXXXXXX` (if you have a hosted zone)
  - `DnsRecordName=events.example.com` (if you want Route 53 alias)
  - `DnsFailoverRole=PRIMARY|SECONDARY` (when using multi‑region)
  - `EnableAuroraGlobal=true|false`
  - `AuroraGlobalClusterId=liveevent-global-cluster`

Workflows included:
- `.github/workflows/deploy.yml` – builds, tests, deploys CDK, applies EF migrations, smoke tests `/health`
- `.github/workflows/deploy-secondary.yml` – deploys the replica stack in a second region (for Aurora Global)

### 4) First Deploy (CLI path)
From your workstation (optional if using GitHub Actions only):

```bash
cd src/infrastructure
npm i -g aws-cdk
cdk bootstrap

# Base deploy (single region)
cdk deploy LiveEventServiceStack --require-approval never

# Optional: Blue/Green
cdk deploy LiveEventServiceStack --require-approval never \
  -c EnableBlueGreen=true -c CodeDeployShiftingConfig=Linear10PercentEvery5Minutes

# Optional: Route 53 alias
cdk deploy LiveEventServiceStack --require-approval never \
  -c HostedZoneId=ZXXXXXXXXXXXX -c DnsRecordName=events.example.com -c DnsFailoverRole=PRIMARY

# Optional: Aurora Global (primary)
cdk deploy LiveEventServiceStack --require-approval never \
  -c EnableAuroraGlobal=true -c AuroraGlobalClusterId=liveevent-global-cluster
```

Outputs include: `AlbDnsName`, `UserPoolId`, `UserPoolClientId`, `IdentityPoolId`, `RedisEndpoint`.

### 5) First Deploy (GitHub Actions path)
Push to your default branch to trigger `.github/workflows/deploy.yml`. The workflow:
- Assumes the AWS role
- Synths and deploys CDK (builds/pushes container images)
- Runs EF Core migrations
- Smoke tests the API using `API_ENDPOINT`

After the first run, set `API_ENDPOINT` to the ALB DNS or your Route 53 record for reliable smoke tests.

### 6) Optional Multi‑Region
1. Deploy primary region with `DnsFailoverRole=PRIMARY`.
2. Deploy secondary region using `deploy-secondary.yml` (set `AWS_REGION` to the secondary and `DnsFailoverRole=SECONDARY`).
3. Use your hosted zone to create PRIMARY/SECONDARY failover alias records.
4. For Aurora Global, deploy `LiveEventReplicaStack` in the replica region with the same `AuroraGlobalClusterId`.

### 7) Blue/Green Rollouts (optional)
- Enable with `-c EnableBlueGreen=true` and set `-c CodeDeployShiftingConfig=...`.
- Validate on the test listener path before CodeDeploy shifts traffic.

### 8) Post‑Deployment
- Confirm `/health` returns healthy
- Verify logs in CloudWatch and traces in X‑Ray
- Capture outputs for app configuration or frontend use (Cognito IDs)

### 9) Tear‑down
Use `cdk destroy` for stacks you want to remove. Delete Route 53 records manually if created.


