# Scalability Improvements Guide

This document outlines scalability enhancements for the Live Event Service.

## Current Scalability Assessment

### ✅ Current State
- Docker Compose for local; proposed ECS features listed here are future-state
- Database connection pooling: default EF Core/Npgsql
- Baseline read-through caching at the API/GraphQL layer via Redis (IDistributedCache)

### ⚠️ Scalability Limitations
- Single DB instance (local dev): no read replicas
- No caching layer
- No API Gateway in repo; no request caching
- No CDN in repo

## High-Priority Improvements

### 1. Database Read Replicas
```csharp
var readReplica = new DatabaseInstanceReadReplica(this, "ReadReplica", new DatabaseInstanceReadReplicaProps
{
    SourceDatabaseInstance = database,
    InstanceType = Amazon.CDK.AWS.EC2.InstanceType.Of(InstanceClass.T3, InstanceSize.MICRO)
});
```

### 2. Redis Caching Layer
```csharp
var redisCluster = new CfnCacheCluster(this, "RedisCluster", new CfnCacheClusterProps
{
    Engine = "redis",
    CacheNodeType = "cache.t3.micro",
    NumCacheNodes = 1
});
```

### 3. API Gateway Caching
```csharp
DefaultMethodOptions = new MethodOptions
{
    CachingEnabled = true,
    CacheTtl = Duration.Minutes(5)
}
```

## Expected Gains
- **Phase 1**: 3-5x capacity increase
- **Phase 2**: Additional 2-3x capacity increase
- **Total**: 10-50x capacity increase 