#!/bin/bash
# Pre-commit hook setup script for Live Event Service
# This script sets up Git hooks to run security checks before each commit

# Exit on error
set -e

# Check if pre-commit is installed
if ! command -v pre-commit &> /dev/null; then
    echo "Installing pre-commit..."
    pip install pre-commit
fi

# Create pre-commit config file
cat > .pre-commit-config.yaml << 'EOL'
# Pre-commit configuration for Live Event Service
repos:
-   repo: https://github.com/pre-commit/pre-commit-hooks
    rev: v4.3.0
    hooks:
    -   id: trailing-whitespace
    -   id: end-of-file-fixer
    -   id: check-yaml
    -   id: check-json
    -   id: check-added-large-files
        args: [--maxkb=500]
    -   id: detect-aws-credentials
    -   id: detect-private-key

-   repo: https://github.com/antonbabenko/pre-commit-terraform
    rev: v1.74.1
    hooks:
    -   id: terraform_fmt
    -   id: terraform_validate

-   repo: https://github.com/gruntwork-io/pre-commit
    rev: v0.1.12
    hooks:
    -   id: shellcheck
        files: '^scripts/.*\.sh$'

-   repo: local
    hooks:
    -   id: dotnet-format
        name: dotnet-format
        entry: dotnet format --check
        language: system
        types: [csharp]
        require_serial: true

    -   id: dotnet-secrets-check
        name: Check for .NET User Secrets
        entry: bash -c '! find . -name "*.cs" -exec grep -l "AddUserSecrets" {} \; | grep -q .'
        language: system
        always_run: true

    -   id: aws-credentials-check
        name: Check for AWS Credentials
        entry: bash -c '! grep -r "AKIA[0-9A-Z]\{16\}" --include="*.cs" --include="*.json" --include="*.config" --include="*.ps1" --include="*.sh" .'
        language: system
        always_run: true

    -   id: connection-strings-check
        name: Check for Hardcoded Connection Strings
        entry: bash -c '! grep -r "Server=[^;]*;Database=[^;]*;User Id=[^;]*;Password=[^;]*;" --include="*.cs" --include="*.json" --include="*.config" .'
        language: system
        always_run: true
EOL

# Install pre-commit hooks
echo "Installing pre-commit hooks..."
pre-commit install --install-hooks

# Make the setup script executable
chmod +x scripts/setup-pre-commit.sh

echo "Pre-commit hooks have been installed successfully!"
echo "The following checks will run before each commit:"
echo "- Trailing whitespace and end-of-file fixes"
echo "- YAML and JSON validation"
echo "- Large file detection"
echo "- AWS credentials detection"
echo "- Private key detection"
echo "- Terraform formatting and validation"
echo "- Shell script validation"
echo "- .NET code formatting"
echo "- .NET User Secrets detection"
echo "- Hardcoded connection strings detection"

echo "\nTo run the checks manually, use: pre-commit run --all-files"
