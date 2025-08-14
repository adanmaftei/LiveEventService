# Scalability Improvements Guide

This document outlines scalability enhancements for the Live Event Service.

## Current Scalability Assessment

### ✅ Current State
- Local: Docker Compose; Cloud: ECS Fargate + ALB (WAF), autoscaling on CPU and request count
- Database: RDS PostgreSQL (Multi-AZ); optional Aurora PostgreSQL Global Database (primary + replica stack)
- Caching: Output caching for public GETs, read‑through caching helpers with Redis (`IDistributedCache`)
- Async processing: SQS worker with autoscaling based on backlog; transactional outbox publishes to SNS

### ⚠️ Scalability Limitations
- Application read‑split not implemented (writes/reads go to primary); infra for read replicas is available via Aurora Global
- Connection pool tuning not customized yet
- CDN not provisioned in repo (can add CloudFront in front of ALB)

## High-Priority Improvements

### 1. Database Read Replicas (Aurora Global)
- Infra: Enable `-c EnableAuroraGlobal=true` in primary region and deploy `LiveEventReplicaStack` in secondary/reader regions.
- App: Introduce an optional read‑only `DbContext`/connection string for query paths to target the Aurora reader endpoint.

### 2. Redis Caching Tuning
- Tune TTLs for event list/detail and user detail based on traffic patterns.
- Consider cache key tagging/invalidation patterns if TTLs increase.

### 3. Edge Caching (CDN)
- Add CloudFront in front of ALB; cache public GETs for short TTLs; respect `Cache-Control` headers.

### 4. Autoscaling & Backpressure
- Keep ECS request‑based and CPU autoscaling; add SQS backlog‑based scaling policies (already configured) for the worker.

### 5. Connection Pooling
- Tune Npgsql pool size/timeouts for peak concurrency; validate with load testing.

## Expected Gains
- **Phase 1**: 2-3x capacity (caching TTL tuning, connection pool tuning)
- **Phase 2**: Additional 2-3x (CDN + read‑split to Aurora reader)
- **Total**: 5-10x capacity depending on traffic mix