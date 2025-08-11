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

#### 1. Deploy Workflow (`.github/workflows/deploy.yml`)

This workflow handles the main CI/CD pipeline:

```yaml
name: Deploy Live Event Service

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]
  workflow_dispatch:

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
    - name: Set up .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
```

#### 2. Security Testing Workflow (`.github/workflows/security-testing.yml`)

This workflow runs security scans:

```yaml
name: Security Testing

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]
  schedule:
    - cron: '0 0 * * 0'  # Weekly security scan

jobs:
  security-scan:
    runs-on: ubuntu-latest
    steps:
    - name: Set up .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
``` 