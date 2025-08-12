using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LiveEventService.Infrastructure.HealthChecks;

public sealed class S3BucketHealthCheck : IHealthCheck
{
    private readonly IAmazonS3 _s3Client;
    private readonly string? _bucketName;

    public S3BucketHealthCheck(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["AWS:S3BucketName"];
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_bucketName))
        {
            return HealthCheckResult.Healthy("S3 bucket name not configured");
        }

        try
        {
            var response = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucketName,
                MaxKeys = 1
            }, cancellationToken);

            return HealthCheckResult.Healthy($"Bucket '{_bucketName}' reachable (objects listed: {response.KeyCount})");
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return HealthCheckResult.Unhealthy($"Bucket '{_bucketName}' not found", ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("S3 health check failed", ex);
        }
    }
}


