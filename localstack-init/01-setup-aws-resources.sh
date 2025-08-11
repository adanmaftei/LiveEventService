#!/bin/bash

echo "Setting up LocalStack AWS resources..."

# Wait for LocalStack to be ready
sleep 5

# Set AWS CLI to use LocalStack
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test
export AWS_DEFAULT_REGION=us-east-1

# Create S3 bucket
echo "Creating S3 bucket..."
aws --endpoint-url=http://localhost:4566 s3 mb s3://local-bucket

# Create Cognito User Pool
echo "Creating Cognito User Pool..."
USER_POOL_ID=$(aws --endpoint-url=http://localhost:4566 cognito-idp create-user-pool \
    --pool-name "local-test-pool" \
    --policies '{
        "PasswordPolicy": {
            "MinimumLength": 8,
            "RequireUppercase": true,
            "RequireLowercase": true,
            "RequireNumbers": true,
            "RequireSymbols": true
        }
    }' \
    --auto-verified-attributes email \
    --username-attributes email \
    --query 'UserPool.Id' --output text)

echo "User Pool ID: $USER_POOL_ID"

# Create User Pool Client
echo "Creating User Pool Client..."
CLIENT_ID=$(aws --endpoint-url=http://localhost:4566 cognito-idp create-user-pool-client \
    --user-pool-id $USER_POOL_ID \
    --client-name "local-test-client" \
    --explicit-auth-flows ADMIN_NO_SRP_AUTH USER_PASSWORD_AUTH \
    --query 'UserPoolClient.ClientId' --output text)

echo "Client ID: $CLIENT_ID"

# Create a test user
echo "Creating test user..."
aws --endpoint-url=http://localhost:4566 cognito-idp admin-create-user \
    --user-pool-id $USER_POOL_ID \
    --username "testuser@example.com" \
    --user-attributes Name=email,Value=testuser@example.com Name=given_name,Value=Test Name=family_name,Value=User \
    --temporary-password "TempPass123!" \
    --message-action SUPPRESS

# Set permanent password
echo "Setting permanent password..."
aws --endpoint-url=http://localhost:4566 cognito-idp admin-set-user-password \
    --user-pool-id $USER_POOL_ID \
    --username "testuser@example.com" \
    --password "TestPass123!" \
    --permanent

# Create admin user
echo "Creating admin user..."
aws --endpoint-url=http://localhost:4566 cognito-idp admin-create-user \
    --user-pool-id $USER_POOL_ID \
    --username "admin@example.com" \
    --user-attributes Name=email,Value=admin@example.com Name=given_name,Value=Admin Name=family_name,Value=User \
    --temporary-password "TempPass123!" \
    --message-action SUPPRESS

aws --endpoint-url=http://localhost:4566 cognito-idp admin-set-user-password \
    --user-pool-id $USER_POOL_ID \
    --username "admin@example.com" \
    --password "AdminPass123!" \
    --permanent

# Create CloudWatch Log Group
echo "Creating CloudWatch Log Group..."
aws --endpoint-url=http://localhost:4566 logs create-log-group \
    --log-group-name "/live-event-service/logs"

echo "LocalStack setup complete!"
echo "User Pool ID: $USER_POOL_ID"
echo "Client ID: $CLIENT_ID"
echo "S3 Bucket: local-bucket"
echo "Test User: testuser@example.com / TestPass123!"
echo "Admin User: admin@example.com / AdminPass123!" 