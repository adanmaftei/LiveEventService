# Performance Improvements Implementation (Aligned)

## 🎯 **Overview**

This document tracks the implementation of performance improvements for the Live Event Service, focusing on the highest-impact changes based on current architecture analysis.

## ✅ **Completed Items**
- [x] Architectural refactoring (domain event handlers moved to Application layer)
- [x] Build and test fixes
- [x] Solution structure documentation updates

## 🚀 **Current Sprint: Performance Optimization**

### **Priority 0: Outbox Pattern Implementation (Critical Reliability)** (Completed via SQS worker + outbox)

#### **0.1 Outbox Table Schema**
- [ ] Create `OutboxMessages` table migration:
  ```sql
  CREATE TABLE OutboxMessages (
      Id UNIQUEIDENTIFIER PRIMARY KEY,
      Type NVARCHAR(255) NOT NULL,
      Data NVARCHAR(MAX) NOT NULL,
      CreatedAt DATETIME2 NOT NULL,
      ProcessedAt DATETIME2 NULL,
      Error NVARCHAR(MAX) NULL,
      RetryCount INT NOT NULL DEFAULT 0,
      MaxRetryCount INT NOT NULL DEFAULT 3,
      NextRetryAt DATETIME2 NULL
  );
  
  CREATE INDEX IX_OutboxMessages_Unprocessed 
  ON OutboxMessages (CreatedAt) 
  WHERE ProcessedAt IS NULL;
  ```

#### **0.2 Outbox Infrastructure**
- [ ] Create `IOutboxMessageRepository` interface
- [ ] Implement `OutboxMessageRepository` with EF Core
- [ ] Create `OutboxMessage` entity
- [ ] Add outbox message configuration to EF Core

#### **0.3 Outbox Service Implementation**
- [ ] Create `IOutboxService` interface
- [ ] Implement `OutboxService` for message persistence
- [ ] Update `MediatRDomainEventDispatcher` to use outbox for async events
- [ ] Create `OutboxMessageProcessor` background service
- [ ] Implement retry logic with exponential backoff

#### **0.4 Integration with Current Architecture**
- [ ] Update `DomainEventBackgroundService` to consume from outbox instead of in-memory queue
- [ ] Modify `IMessageQueue` to be outbox-based
- [ ] Add outbox health checks
- [ ] Implement outbox message cleanup (processed messages older than X days)

### **Priority 1: Async Domain Event Processing**

#### **1.1 Background Service Infrastructure**
- [ ] Create `IMessageQueue` interface in Application layer
- [ ] Implement `InMemoryMessageQueue` for development/testing
- [ ] Create `DomainEventBackgroundService` in Application layer
- [ ] Add `IDomainEventProcessor` interface
- [ ] Implement retry logic and dead letter queue handling

#### **1.2 Domain Event Handler Refactoring**
- [ ] Identify non-critical domain event handlers for async processing:
  - [ ] `WaitlistPositionChangedDomainEventHandler` (non-critical)
  - [ ] `EventRegistrationPromotedDomainEventHandler` (non-critical)
  - [ ] `EventRegistrationCreatedDomainEventHandler` (non-critical)
- [ ] Keep critical handlers synchronous:
  - [ ] `EventRegistrationCancelledDomainEventHandler` (critical - affects business logic)
  - [ ] `EventCapacityIncreasedDomainEventHandler` (critical - affects business logic)
- [ ] Add `[AsyncProcessing]` attribute for async handlers
- [ ] Update `MediatRDomainEventDispatcher` to route events appropriately

#### **1.3 Background Service Implementation**
- [ ] Create `DomainEventBackgroundService` class
- [ ] Implement event processing pipeline
- [ ] Add error handling and retry mechanisms
- [ ] Add health checks for background service
- [ ] Update dependency injection configuration

### **Priority: HybridCache/Redis Implementation (.NET 9)** (Baseline read-through implemented; continue tuning TTLs)

#### **2.1 Cache Infrastructure**
- [ ] Add `Microsoft.Extensions.Caching.HybridCache` package
- [ ] Create `ICacheService` interface in Application layer
- [ ] Implement `HybridCacheService` wrapper
- [ ] Configure cache options in `appsettings.json`
- [ ] Add cache health checks

#### **2.2 Caching Strategy Implementation**
- [ ] **Event Details Caching**:
  - [ ] Cache event information for 5 minutes
  - [ ] Implement cache invalidation on event updates
  - [ ] Add cache key generation utilities
- [ ] **User Information Caching**:
  - [ ] Cache user data for 10 minutes
  - [ ] Implement cache invalidation on user updates
- [ ] **Waitlist Positions Caching**:
  - [ ] Cache current waitlist state for 1 minute
  - [ ] Implement cache invalidation on waitlist changes
- [ ] **Registration Counts Caching**:
  - [ ] Cache confirmed registration counts for 2 minutes
  - [ ] Implement cache invalidation on registration changes

#### **2.3 Repository Layer Caching**
- [ ] Update `EventRepository` to use caching
- [ ] Update `UserRepository` to use caching
- [ ] Update `EventRegistrationRepository` to use caching
- [ ] Add cache invalidation in domain event handlers
- [ ] Implement cache warming strategies

### **Priority: Database Query Optimization**

#### **3.1 Database Indexes**
- [ ] Verify and augment EF Core migrations for performance indexes:
  ```sql
  -- Event registrations by event and status
  CREATE INDEX IX_EventRegistrations_EventId_Status 
  ON EventRegistrations (EventId, Status);
  
  -- Waitlist positions
  CREATE INDEX IX_EventRegistrations_EventId_Waitlisted 
  ON EventRegistrations (EventId, PositionInQueue) 
  WHERE Status = 'Waitlisted';
  
  -- User registrations
  CREATE INDEX IX_EventRegistrations_UserId 
  ON EventRegistrations (UserId);
  
  -- Event queries
  CREATE INDEX IX_Events_StartDate 
  ON Events (StartDate);
  
  -- User queries
  CREATE INDEX IX_Users_Email 
  ON Users (Email);
  ```

- [ ] Ensure `AsNoTracking()` for read-only queries and introduce compiled queries for hottest paths:
  - [ ] `GetRegistrationsForEventAsync`
  - [ ] `GetEventAsync`
  - [ ] `GetUserAsync`
  - [ ] `ListEventsAsync`
- [ ] Optimize `Include` statements:
  - [ ] Use projection instead of Include where possible
  - [ ] Implement separate queries for related data
- [ ] Add query performance logging
- [ ] Implement query result caching

#### **3.3 Connection Pooling**
- [ ] Configure optimal connection pool settings
- [ ] Add connection pool monitoring
- [ ] Implement connection health checks

### **Priority: Improved Logging & Metrics**

#### **4.1 Structured Logging Enhancements**
- [ ] Add performance metrics logging and metrics emission (OpenTelemetry):
  - [ ] Request duration tracking
  - [ ] Database query duration
  - [ ] Cache hit/miss ratios
  - [ ] Domain event processing duration
- [ ] Implement correlation ID propagation
- [ ] Add business metrics logging:
  - [ ] Registration conversion rates
  - [ ] Waitlist promotion rates
  - [ ] Event popularity metrics

#### **4.2 Logging Configuration**
- [ ] Configure log levels for different environments
- [ ] Add log filtering for sensitive data
- [ ] Implement log aggregation
- [ ] Add performance log sampling

## 📋 **Implementation Plan**

### **Week 1: Foundation & Async Processing**
- [ ] **Day 1-2**: Background service infrastructure
  - [ ] Create message queue interfaces
  - [ ] Implement basic background service
  - [ ] Add dependency injection configuration
- [ ] **Day 3-4**: Domain event handler refactoring
  - [ ] Identify critical vs non-critical handlers
  - [ ] Add async processing attributes
  - [ ] Update event dispatcher
- [ ] **Day 5**: Testing and validation
  - [ ] Unit tests for background service
  - [ ] Integration tests for async processing
  - [ ] Performance baseline measurement

### **Week 2: Caching Implementation**
- [ ] **Day 1-2**: HybridCache infrastructure
  - [ ] Add HybridCache package
  - [ ] Create cache service wrapper
  - [ ] Configure cache options
- [ ] **Day 3-4**: Caching strategy implementation
  - [ ] Implement event details caching
  - [ ] Implement user information caching
  - [ ] Implement waitlist positions caching
- [ ] **Day 5**: Repository layer integration
  - [ ] Update repositories to use caching
  - [ ] Add cache invalidation
  - [ ] Performance testing

### **Week 3: Database Optimization**
- [ ] **Day 1-2**: Database indexes
  - [ ] Create and apply index migrations
  - [ ] Test index performance impact
  - [ ] Monitor query performance
- [ ] **Day 3-4**: EF Core query optimization
  - [ ] Add AsNoTracking() to read queries
  - [ ] Optimize Include statements
  - [ ] Implement query result caching
- [ ] **Day 5**: Connection pooling
  - [ ] Configure connection pool settings
  - [ ] Add connection monitoring
  - [ ] Performance validation

### **Week 4: Logging & Monitoring**
- [ ] **Day 1-2**: Structured logging enhancements
  - [ ] Add performance metrics logging
  - [ ] Implement correlation ID propagation
  - [ ] Add business metrics logging
- [ ] **Day 3-4**: Logging configuration
  - [ ] Configure log levels
  - [ ] Add log filtering
  - [ ] Implement log aggregation
- [ ] **Day 5**: Final testing and optimization
  - [ ] End-to-end performance testing
  - [ ] Load testing
  - [ ] Documentation updates

## 📊 **Success Metrics**

### **Performance Targets**
- [ ] **Response Time Improvements**:
  - [ ] Event Registration: 200ms → 50ms (75% improvement)
  - [ ] Waitlist Operations: 500ms → 100ms (80% improvement)
  - [ ] Event Listing: 150ms → 30ms (80% improvement)
- [ ] **Throughput Improvements**:
  - [ ] Concurrent Registrations: 100/sec → 500/sec (5x improvement)
  - [ ] Database Queries: 50% reduction in query count
  - [ ] Memory Usage: 30% reduction through caching

### **Monitoring KPIs**
- [ ] **Cache Hit Rate**: > 80% for frequently accessed data
- [ ] **Database Query Time**: < 100ms average
- [ ] **Background Processing**: < 5s for non-critical events
- [ ] **Error Rate**: < 0.1% for all operations

## 🧪 **Testing Strategy**

### **Unit Tests**
- [ ] Background service tests
- [ ] Cache service tests
- [ ] Repository optimization tests
- [ ] Logging enhancement tests

### **Integration Tests**
- [ ] Async domain event processing tests
- [ ] Cache integration tests
- [ ] Database performance tests
- [ ] End-to-end performance tests

### **Load Tests**
- [ ] Concurrent registration tests
- [ ] Waitlist operation stress tests
- [ ] Cache performance under load
- [ ] Database performance under load

## 🔧 **Configuration Updates**

### **appsettings.json Updates**
```json
{
  "Performance": {
    "Caching": {
      "EventDetails": {
        "ExpirationMinutes": 5,
        "MaxSize": 1000
      },
      "UserInformation": {
        "ExpirationMinutes": 10,
        "MaxSize": 500
      },
      "WaitlistPositions": {
        "ExpirationMinutes": 1,
        "MaxSize": 100
      }
    },
    "BackgroundProcessing": {
      "MaxConcurrency": 4,
      "RetryAttempts": 3,
      "RetryDelaySeconds": 5
    },
    "Database": {
      "ConnectionPoolSize": 100,
      "CommandTimeoutSeconds": 30,
      "EnableQueryLogging": false
    }
  }
}
```

## 📚 **Documentation Updates**

### **Technical Documentation**
- [ ] Update `PERFORMANCE_AND_SCALABILITY.md` with implementation details
- [ ] Create `CACHING_STRATEGY.md` documentation
- [ ] Update `DOMAIN_EVENTS_AND_GRAPHQL.md` with async processing details
- [ ] Create `BACKGROUND_SERVICES.md` documentation

### **API Documentation**
- [ ] Update API response time expectations
- [ ] Document caching behavior
- [ ] Add performance considerations

## 🚀 **Next Steps After Completion**

### **Phase 2: Advanced Optimizations**
- [ ] **Microservices Preparation**: Edge remains ALB-only; evaluate service discovery and decomposition when needed
- [ ] **Event Sourcing**: Prepare for event sourcing implementation
- [ ] **CQRS Enhancement**: Separate read models optimization
- [ ] **Distributed Caching**: Redis cluster implementation

### **Phase 3: Monitoring & Observability**
- [ ] **APM Integration**: Application Performance Monitoring
- [ ] **Business Metrics Dashboard**: Real-time business metrics
- [ ] **Alerting**: Performance-based alerting rules
- [ ] **Capacity Planning**: Resource utilization optimization

---

This document reflects current status:

- Baseline indexes and EF optimizations: DONE
- Read-through/output caching: DONE (helpers + output cache)
- Outbox metrics and monitoring: DONE (OpenTelemetry metrics)
- Remaining: compiled queries for hottest read paths; connection pool tuning; load testing validation