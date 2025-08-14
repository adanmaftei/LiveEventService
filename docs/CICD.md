# CI/CD Pipeline Documentation

This document describes the Continuous Integration and Continuous Deployment (CI/CD) pipeline for the Live Event Service.

## Overview

The CI/CD pipeline is implemented using GitHub Actions and consists of the following stages:

1. **Unit Tests** - Run unit tests on every push and pull request
2. **Integration Tests** - Run integration tests with Testcontainers (Docker on hosted runner)
3. **Security Testing** - Run dependency/SAST scans on schedule and PRs
4. **Deploy** - Deploy CDK stack to AWS (ECS, RDS, ADOT), with AMP/AMG deferred

## Pipeline Configuration

### GitHub Actions Workflows (Implemented)

- `.github/workflows/deploy.yml`
  - Jobs: lint-format, unit-tests, integration-tests, build-and-deploy
  - Uses OIDC + `aws-actions/configure-aws-credentials` to assume an AWS role
  - Deploys CDK stack in `src/infrastructure` and runs EF Core migrations
  - Smoke test hits `/health` using `API_ENDPOINT` secret

- `.github/workflows/security-testing.yml`
  - Runs NuGet vulnerability scan, OWASP Dependency-Check, Security Code Scan, SonarCloud (optional), gitleaks
  - Also supports scheduled ZAP baseline scans (configure your public API URL)

Notes:
- CDK builds/publishes container images from the API and Worker directories via `ContainerImage.FromAsset`.
- EF migrations run as a separate step post-deploy.
- For multi-environment promotion, parameterize capacity and environment-specific values in CDK (`ApiDesiredCount`, `WorkerDesiredCount`, etc.) and deploy distinct stacks.