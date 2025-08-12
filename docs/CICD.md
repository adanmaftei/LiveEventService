# CI/CD Pipeline Documentation

This document describes the Continuous Integration and Continuous Deployment (CI/CD) pipeline for the Live Event Service.

## Overview

The CI/CD pipeline is implemented using GitHub Actions and consists of the following stages:

1. **Unit Tests** - Run unit tests on every push and pull request
2. **Integration Tests** - Run integration tests with Docker containers
3. **Security Testing** - Run security scans and vulnerability checks
4. **Build and Deploy** - Build Docker image and deploy to AWS ECS (only on main branch)

## Pipeline Configuration

### GitHub Actions Workflows

No workflow files are currently committed in this repository. The following outlines a proposed setup:

#### 1. Deploy Workflow (proposed path: `.github/workflows/deploy.yml`)

Key stages:
- Restore → Build → Unit + Integration Tests (with Docker) → Build/push image → Deploy (ECS/ECR)

#### 2. Security Testing Workflow (proposed path: `.github/workflows/security-testing.yml`)

Key stages:
- Dependency scanning (e.g., dotnet list package --vulnerable), SAST, container image scan