# CI/CD Pipeline Documentation

This document describes the Continuous Integration and Continuous Deployment (CI/CD) pipeline for the Live Event Service.

## Overview

The CI/CD pipeline is implemented using GitHub Actions and consists of the following stages:

1. **Unit Tests** - Run unit tests on every push and pull request
2. **Integration Tests** - Run integration tests with Testcontainers (Docker on hosted runner)
3. **Security Testing** - Run dependency/SAST scans on schedule and PRs
4. **Deploy** - Deploy CDK stack to AWS (ECS, RDS, ADOT, AMP/AMG) on main branch

## Pipeline Configuration

### GitHub Actions Workflows

The repository includes:

- `.github/workflows/deploy.yml`
  - Jobs: unit-tests, integration-tests, build-and-deploy
  - Uses GitHub OIDC (`id-token: write`) and `aws-actions/configure-aws-credentials` to assume a role
  - Runs tests then deploys the CDK stack (`src/infrastructure`) as the single source of truth
  - CDK provisions AMP and AMG and auto-imports dashboards via a custom resource

- `.github/workflows/security-testing.yml`
  - Runs dependency audit, OWASP Dependency-Check, SonarCloud (if configured), SCS, gitleaks
  - Scheduled weekly and on PRs

Notes:
- Image builds and task-definition wiring are handled by CDK; pipeline no longer pushes images directly.
- For multi-environment promotion, pass an image tag into CDK via context/parameters and manage environments via separate stacks.