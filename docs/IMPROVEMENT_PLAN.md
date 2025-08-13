# LiveEventService Improvement Plan

This plan tracks prioritized improvements across scalability, resilience, performance, security, observability, CI/CD, and cost. It consolidates concrete tasks, owners, status, and acceptance criteria.

## Legend
- Priority: P0 (critical), P1 (high), P2 (medium)
- Status: Todo, In-Progress, Done, Blocked

## 1) Scalability

- [x] P0 Replace in-memory domain event queue with SQS-backed processing (worker service)
  - Owner: TBD
  - Status: Done
  - Details:
    - Replace `IMessageQueue` in API with an SQS producer implementation; remove `DomainEventBackgroundService` from the API process
    - Create a new Worker project to consume SQS and execute `IDomainEventProcessor` handlers
    - Use DLQ for poison messages
    - Autoscale Worker Fargate service by SQS backlog (age/depth)
    - Parameterize Worker desired/min/max capacity via CDK parameters for env-specific tuning
    - Integration tests run SQS worker in-process and enable `AWS:SQS:UseSqsForDomainEvents=true` with LocalStack
  - Acceptance:
    - API horizontally scales without duplicate processing (UseInProcess=false in Production; UseSqsForDomainEvents=true)
    - SQS queue + DLQ provisioned; worker consumes and deletes after successful processing; failures retried via visibility timeout
    - Dedicated SQS integration tests against LocalStack pass end-to-end (enqueue, worker handles, observable state change)
    - Baseline integration suite remains independent of SQS; SQS tests use separate fixture to avoid flakiness
  - References: `src/LiveEventService.Application/Common/InMemoryMessageQueue.cs`, `DomainEventBackgroundService`, `MediatRDomainEventDispatcher`, `src/LiveEventService.Infrastructure/Messaging/SqsMessageQueue.cs`, `src/LiveEventService.Worker/*`, `tests/LiveEventService.IntegrationTests/Infrastructure/Sqs/SqsTestApplicationFactory.cs`, `tests/LiveEventService.IntegrationTests/Sqs/SqsFlowTests.cs`, `tests/LiveEventService.IntegrationTests/Sqs/SqsMultiPromotionTests.cs`
  - Notes: Config flag `Performance:BackgroundProcessing:UseInProcess` added to allow disabling in-proc background service during transition; `AWS:SQS:UseSqsForDomainEvents` flips to SQS producer; new worker at `src/LiveEventService.Worker/`. CDK provisions SQS + DLQ and queue alarms (age, depth, DLQ) wired to SNS; LocalStack init creates both queues. Tests run an in-process SQS worker and use strongly-typed DTOs (no dynamic).

- [x] P0 Make GraphQL subscriptions scalable (Redis backplane)
  - Owner: TBD
  - Status: Done
  - Details: Switched to Redis-backed subscriptions using StackExchange.Redis connection
  - Acceptance: Subscriptions received across multiple API instances
  - References: `src/LiveEventService.API/Program.cs`, `LiveEventService.API.csproj`

## 2) Resilience

- [x] P0 Add outbox leasing/claiming to prevent double processing
  - Owner: TBD
  - Status: Done
  - Details: Added `Processing` status and leasing fields (`ClaimedBy`, `ClaimedAt`); `OutboxProcessor` atomically claims Pending records, processes, and marks `Processed` or requeues with backoff
  - Acceptance: Prevents duplicate processing across concurrent workers by leasing and state transitions
  - References: `src/LiveEventService.Infrastructure/Data/OutboxProcessor.cs`, `LiveEventService.Infrastructure/Data/OutboxMessage.cs`, `LiveEventService.Infrastructure/Migrations/20250808132000_AddOutbox.cs`, `LiveEventDbContext.cs`
  - Notes: Follow-up tests for high-concurrency validation can be added to integration suite

- [x] P1 Add idempotency for write endpoints
  - Owner: TBD
  - Status: Done (basic key-based claim)
  - Details: Added `IIdempotencyStore` with distributed cache fallback; applied to create event, register, confirm, cancel
  - Acceptance: Replays within 10 minutes return 409 Conflict
  - References: `EventEndpoints`, `Utilities/IdempotencyStore`

- [x] P1 Make rate-limiting proxy-aware and remove rate limits from health endpoints
  - Owner: Implemented
  - Status: Done
  - Details: Added `UseForwardedHeaders()` and trusted proxies; removed `.RequireRateLimiting(...)` from health endpoints
  - Acceptance: Real client IP used behind ALB/API GW; health probes unaffected by rate limits
  - References: `src/LiveEventService.API/Program.cs`

- [x] P1 Move DB initialization/migrations out of API startup
  - Owner: TBD
  - Status: Done
  - Details: Startup DB initialization is gated to Development only; Production never runs migrations at startup. CI/CD `deploy.yml` runs `dotnet ef database update` as a separate step to apply migrations. Optional one-shot migration job deferred.
  - Acceptance: Zero startup migration races; blue/green deploy safe
  - References: `src/LiveEventService.API/Program.cs`, `.github/workflows/deploy.yml`

## 3) Performance

- [x] P1 Add read-through caching for event detail/user lookups (API/GraphQL via Redis IDistributedCache)
  - Owner: TBD
  - Status: Done (event detail + user detail + event list)
  - Details: Added `CacheHelper` + read-through cache for event detail (5m TTL), user detail (10m TTL), event list (2m TTL)
  - Next: Tune TTLs per traffic; consider cached 404s if needed
  - Acceptance: 95th percentile latency improved; cache hit ratio visible in Grafana
  - References: `EventEndpoints`, `UserEndpoints`, `GraphQL/EventQueries`, `Utilities/CacheHelper`

- [x] P1 Cache invalidation for event mutations
  - Owner: TBD
  - Status: Done (update/delete/publish/unpublish)
  - Details: Remove `event:{id}` and `event:{id}:graphql` on event changes
  - Acceptance: Subsequent reads reflect latest data
  - References: `EventEndpoints`

- [x] P1 Add/verify DB indexes for common filters and joins
  - Owner: TBD
  - Status: Done (baseline hot-path indexes)
  - Details: Added composite indexes to support filtered chronological lists and registration queries
    - `Events(IsPublished, StartDate)` and `Events(OrganizerId, StartDate)`
    - `EventRegistrations(EventId, Status, RegistrationDate)`
    - Fixed non-sargable status filter to enum comparison
  - Acceptance: Reduced query latency under load; planner uses index scans for common filters
  - References: EF configurations and migrations (`EventConfiguration`, `EventRegistrationConfiguration`, `20250807122320_AddPerformanceIndexes`)

- [x] P1 GraphQL cost controls
  - Owner: TBD
  - Status: Done
  - Details: Enforced MaxDepth (10 via AddMaxExecutionDepthRule), short execution timeout (10s), disabled GraphQL IDE in non-development. Optimized DataLoader to reduce N+1s: `UserByIdentityIdDataLoader` injects `IUserRepository`, uses `MaxBatchSize=250`, normalizes keys, and avoids per-batch scope creation. Persisted queries deferred.
  - Acceptance: Malicious/expensive queries rejected; steady resource use under load
  - References: `Program.cs` GraphQL config

## 4) Security

- [x] P1 Implement audit logging persistence (admin-sensitive actions)
  - Owner: TBD
  - Status: Done
  - Details: Dedicated audit logger sink to CloudWatch Logs (separate log group `/live-event-service/audit`) with environment-driven region. In Production, `IAuditLogger` writes to the dedicated group; in non-prod it writes to console/app logs. Dashboards via CloudWatch already exist; retention configured at log group. 
  - Acceptance: Audit trail persisted out-of-band from DB; visible in CloudWatch; separable from application logs.
  - References: `Program.cs` (audit sink wiring), `src/LiveEventService.API/Logging/AuditLogger.cs`, CDK log retention.

- [x] P1 Harden CORS and CSP per environment
  - Owner: TBD
  - Status: Done
  - Details: Env-driven CORS (dev/testing allow-all; prod restricts to `Security:Cors:AllowedOrigins`). Middleware applies X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy. Production CSP finalized to API-only: `default-src 'none'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'`. GraphQL playground/dev tools disabled outside Development.
  - Acceptance: Headers present on health endpoint; CORS enforced; ZAP baseline clean.
  - References: `src/LiveEventService.API/Program.cs`, `src/LiveEventService.API/Middleware/SecurityHeadersMiddleware.cs`, `src/LiveEventService.API/appsettings.json`, `src/LiveEventService.API/appsettings.Production.json`

## 5) Cost Optimization

- [x] P1 Simplify edge stack (choose ALB or API Gateway) and revisit WAF scope
  - Owner: TBD
  - Status: Done
  - Details: Chosen ALB-only. Removed API Gateway proxy and usage plan; associated WAF with ALB; monitoring updated to drop API Gateway widgets/alarms; outputs switched to `AlbDnsName`. Request-based autoscaling and CPU autoscaling on ECS retained.
  - Acceptance: Edge cost reduced without functional loss; WAF protection active on ALB
  - References: `src/infrastructure/LiveEventService.Infrastructure.CDK/LiveEventServiceStack.cs`, `MonitoringConstruct.cs`

- [x] P2 Right-size autoscaling and NAT usage
  - Owner: TBD
  - Status: Done
  - Details: Lowered API min capacity; shortened log retention (application logs 30 days, audit logs 90 days); added ALB request-based autoscaling to API service; worker autoscaling parameterized; API desired/min/max capacity parameterized via CDK. Added CDK parameter `NatGateways` (0 for dev, 1 for prod). Added VPC Endpoints: Interface (SQS, Secrets Manager, CloudWatch Logs, X-Ray, ECR API, ECR DKR, STS) and Gateway (S3) to reduce NAT data processing. API defaults tuned (desired=1, min=0, max=3).
  - Acceptance: Reduced monthly baseline cost; no SLO impact
  - References: `LiveEventServiceStack.cs`, CloudWatch retention

## 6) Observability

- [x] P2 Start with CloudWatch logs/metrics + X-Ray; defer AMP/AMG unless required
  - Owner: TBD
  - Status: Done
  - Details: ADOT collector exports traces to X-Ray and metrics to CloudWatch EMF only. AMP remote-write and Grafana provisioning removed from infra to cut cost. Use CloudWatch dashboards/alarms.
  - Acceptance: Traces visible in X-Ray; key service metrics viewable in CloudWatch. No AMP/AMG resources deployed.
  - References: `src/infrastructure/LiveEventService.Infrastructure.CDK/LiveEventServiceStack.cs`

## 7) CI/CD

- [x] P1 Add GitHub Actions workflows (build, test, lint, security scan)
  - Owner: TBD
  - Status: Done (initial pipeline)
  - Details: Added lint/format verification job (`dotnet format --verify-no-changes`), unit tests with coverage, integration tests (Testcontainers), deploy job gated on lint/tests, security testing workflow (NuGet audit, Dependency-Check, SonarCloud, SCS, gitleaks, optional ZAP). Smoke test added to hit health endpoint (`/health`) using `API_ENDPOINT` secret.
  - Acceptance: Green pipeline required for deployment; health endpoint returns 200

---

## Changelog of applied edits

- 2025-08-08: Proxy-aware rate limiting and health checks
  - Added `UseForwardedHeaders()` and trusted proxies configuration; removed rate limiting from health endpoints
  - File: `src/LiveEventService.API/Program.cs`
- 2025-08-08: Configurable DB initialization and GraphQL guardrails
  - Added `Database:InitializeOnStartup` flag (defaults to true in Development, false otherwise)
  - Added GraphQL execution guardrails: `MaxAllowedExecutionDepth = 8`, `MaxAllowedComplexity = 200`
  - File: `src/LiveEventService.API/Program.cs`
- 2025-08-08: Read-through caching for event details
  - Implemented cache for GET event by id in REST and GraphQL (5m TTL)
  - Files: `src/LiveEventService.API/Endpoints/EventEndpoints.cs`, `src/LiveEventService.API/GraphQL/Queries/EventQueries.cs`, `src/LiveEventService.API/Utilities/CacheHelper.cs`
- 2025-08-08: Idempotency and cache invalidation
  - Implemented `IIdempotencyStore` and applied to event write endpoints; implemented cache invalidation on event updates
  - Files: `src/LiveEventService.API/Endpoints/EventEndpoints.cs`, `src/LiveEventService.API/Utilities/IdempotencyStore.cs`
  - Notes: Supports optional `Idempotency-Key` header; falls back to deterministic keys when missing
- 2025-08-08: Redis-backed GraphQL subscriptions
  - Replaced in-memory subscriptions with Redis backplane
  - Files: `src/LiveEventService.API/Program.cs`, `src/LiveEventService.API/LiveEventService.API.csproj`
  - 2025-08-08: SQS scaffolding and worker service
    - Added SQS-backed `IMessageQueue` implementation and a new Worker project polling SQS with LocalStack-aware config; added worker autoscaling (age/depth) and CDK parameters for desired/min/max
    - Files: `src/LiveEventService.Infrastructure/Messaging/SqsMessageQueue.cs`, `src/LiveEventService.Worker/*`, infra DI updates, appsettings changes, CDK SQS+DLQ + alarms wired to SNS, LocalStack init script, worker autoscaling + parameters
  - 2025-08-08: API autoscaling by request count
    - Added ALB request-count-per-target target tracking policy alongside CPU-based scaling
    - Files: `src/infrastructure/LiveEventService.Infrastructure.CDK/LiveEventServiceStack.cs`
  - 2025-08-08: API scaling parameters (desired/min/max)
    - Added CDK parameters `ApiDesiredCount`, `ApiMinCapacity`, `ApiMaxCapacity` to tune per environment
    - Files: `src/infrastructure/LiveEventService.Infrastructure.CDK/LiveEventServiceStack.cs`
  - 2025-08-09: Outbox leasing/claiming
    - Implemented `OutboxStatus.Processing`, `ClaimedBy`, `ClaimedAt`, and atomic claim → process → finalize logic
    - Files: `src/LiveEventService.Infrastructure/Data/OutboxProcessor.cs`, `Infrastructure/Data/OutboxMessage.cs`, `Infrastructure/Migrations/20250808132000_AddOutbox.cs`
  - 2025-08-09: SQS E2E tests (strongly-typed) and fixture
    - Added dedicated SQS fixture using LocalStack SQS, with in-process worker; tests verify promotion on cancellation and FIFO order for multiple waitlisted users
    - Files: `src/tests/LiveEventService.IntegrationTests/Infrastructure/Sqs/SqsTestApplicationFactory.cs`, `Sqs/SqsFlowTests.cs`, `Sqs/SqsMultiPromotionTests.cs`
  - 2025-08-09: Performance indexes and query sargability
    - Added `Events(IsPublished, StartDate)`, `Events(OrganizerId, StartDate)`, `EventRegistrations(EventId, Status, RegistrationDate)`; replaced string status comparison with enum to enable index usage
    - Files: `src/LiveEventService.Infrastructure/Configurations/EventConfiguration.cs`, `src/LiveEventService.Infrastructure/Configurations/EventRegistrationConfiguration.cs`, `src/LiveEventService.Infrastructure/Migrations/20250807122320_AddPerformanceIndexes.cs`, `src/LiveEventService.Application/Features/Events/Queries/GetEventRegistrations/GetEventRegistrationsSpecification.cs`
 - 2025-08-09: CI/CD lint and smoke tests
   - Added `lint-format` job and health smoke test in deploy workflow; wired deploy to depend on lint + unit + integration tests
   - Files: `.github/workflows/deploy.yml`
 - 2025-08-09: Env-driven CORS and baseline security headers
   - Added env-driven CORS policy (dev/testing allow-all; prod restricted); confirmed security headers middleware for XFO, XCTO, Referrer-Policy, Permissions-Policy; CSP outside development
   - Files: `src/LiveEventService.API/Program.cs`, `src/LiveEventService.API/Middleware/SecurityHeadersMiddleware.cs`

## Notes
- Add `Networking:KnownProxies` under configuration to list trusted reverse proxies when deploying behind ALB/API Gateway.
- Add `Database:InitializeOnStartup` per environment; keep `false` in production.


