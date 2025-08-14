# Solution Structure Documentation

## Overview

The Live Event Service solution is organized using **Clean Architecture** and **Domain-Driven Design (DDD)** principles with clear separation of concerns. The solution follows a layered architecture pattern with proper dependency direction and domain event-driven communication.

## Architecture Overview

### 🏗️ **Clean Architecture Layers**

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              LiveEventService.API                   │   │
│  │  • REST Controllers                                 │   │
│  │  • GraphQL Resolvers                                │   │
│  │  • Middleware (Auth, Logging, etc.)                 │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
│  ┌─────────────────────────────────────────────────────┐   │
│  │          LiveEventService.Application               │   │
│  │  • CQRS Commands & Queries                          │   │
│  │  • Domain Event Handlers                            │   │
│  │  • MediatR Notifications                            │   │
│  │  • Validation & Business Rules                      │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Domain Layer                           │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              LiveEventService.Core                  │   │
│  │  • Domain Entities (Event, EventRegistration)      │   │
│  │  • Domain Events                                    │   │
│  │  • Domain Services                                  │   │
│  │  • Value Objects                                    │   │
│  │  • Repository Interfaces                            │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                       │
│  ┌─────────────────────────────────────────────────────┐   │
│  │          LiveEventService.Infrastructure            │   │
│  │  • Entity Framework Core                            │   │
│  │  • Repository Implementations                       │   │
│  │  • Database Migrations                              │   │
│  │  • External Service Integrations                    │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## Solution Organization

### 📁 **Solution Folders Structure**

```
LiveEventService.sln
├── 📁 src/                           # Source Code Projects
│   ├── 🎯 LiveEventService.API       # REST & GraphQL API
│   ├── 🔧 LiveEventService.Application # CQRS, Commands, Queries, Domain Event Handlers
│   ├── ⭐ LiveEventService.Core      # Domain Models & Business Logic
│   └── 🗄️ LiveEventService.Infrastructure # Data Access & External Services
├── 📁 infrastructure/                # AWS CDK Infrastructure
│   └── 🏗️ LiveEventService.Infrastructure.CDK
├── 📁 tests/                        # Test Projects
│   ├── 🧪 LiveEventService.UnitTests
│   └── 🔗 LiveEventService.IntegrationTests
├── 📁 docs/                         # All documentation files
├── 📁 .github/workflows/            # CI/CD workflows
└── 📁 observability/                # Local observability configs (Prometheus, Grafana, Loki, ADOT)
```

### 🏗️ **Project Structure Details**

#### 🎯 **LiveEventService.API**
```
LiveEventService.API/
├── Endpoints/                      # Minimal API endpoint mappings (events, users)
├── GraphQL/                        # GraphQL Types & Subscriptions
│   └── Subscriptions/EventRegistrationNotifier.cs
├── Middleware/                     # Global exception middleware
├── Program.cs                      # Application Entry Point
└── appsettings.json                # Configuration
```

#### 🔧 **LiveEventService.Application**
```
LiveEventService.Application/
├── Common/                         # Shared Application Concerns
│   ├── Behaviors/                  # MediatR Pipeline Behaviors
│   ├── Notifications/              # MediatR Notification Adapters
│   └── MediatRDomainEventDispatcher.cs
├── Features/                       # Feature-based Organization
│   ├── Events/                     # Event-related Features
│   │   ├── Commands/               # Event Commands
│   │   ├── Queries/                # Event Queries
│   │   └── DomainEventHandlers/    # Domain Event Handlers
│   │       ├── EventCapacityIncreasedDomainEventHandler.cs
│   │       ├── EventRegistrationCancelledDomainEventHandler.cs
│   │       ├── EventRegistrationCreatedDomainEventHandler.cs
│   │       ├── EventRegistrationPromotedDomainEventHandler.cs
│   │       ├── RegistrationWaitlistedDomainEventHandler.cs
│   │       ├── WaitlistPositionChangedDomainEventHandler.cs
│   │       └── WaitlistRemovalDomainEventHandler.cs
│   └── Users/                      # User-related Features
│       ├── Commands/
│       └── Queries/
├── DependencyInjection.cs          # Application Layer DI Configuration
└── Validators/                     # FluentValidation Validators
```

#### ⭐ **LiveEventService.Core**
```
LiveEventService.Core/
├── Common/                         # Shared Domain Concerns
│   ├── BaseEntity.cs
│   ├── IDomainEventDispatcher.cs
│   ├── IDomainEvent.cs
│   └── IRepository.cs
├── Events/                         # Event Domain
│   ├── Event/
│   │   ├── Event.cs                # Event Entity
│   │   └── EventDomainEvents.cs    # Event-related Domain Events
│   └── EventRegistration/
│       ├── EventRegistration.cs    # EventRegistration Entity
│       └── EventRegistrationDomainEvents.cs
├── Registrations/                  # Registration Domain
│   └── EventRegistration/
│       ├── EventRegistration.cs    # EventRegistration Entity
│       ├── RegistrationStatus.cs   # Enum
│       └── EventRegistrationDomainEvents.cs
└── Users/                          # User Domain
    └── User/
        └── User.cs                 # User Entity
```

#### 🗄️ **LiveEventService.Infrastructure**
```
LiveEventService.Infrastructure/
├── Configurations/                 # EF Core Entity Configurations
│   ├── EventConfiguration.cs
│   ├── EventRegistrationConfiguration.cs
│   └── UserConfiguration.cs
├── Data/                          # Database Context & Migrations
│   ├── ApplicationDbContext.cs
│   └── Migrations/
├── Repositories/                  # Repository Implementations
│   ├── EventRepository.cs
│   ├── Registrations/EventRegistrationRepository.cs
│   └── UserRepository.cs
├── Users/                         # User-related Infrastructure
├── DependencyInjection.cs         # Infrastructure Layer DI Configuration
└── Specifications/                # EF Core Specifications
```

### 🧪 **Test Projects Structure**

#### **LiveEventService.UnitTests**
```
LiveEventService.UnitTests/
├── Application/                   # Application Layer Tests
│   ├── Commands/                  # Command Handler Tests
│   ├── Queries/                   # Query Handler Tests
│   └── Features/                  # Feature Tests
│       └── Events/
│           └── DomainEventHandlers/ # Domain Event Handler Tests
├── Core/                          # Domain Layer Tests
│   ├── Domain/                    # Entity Tests
│   └── Specifications/            # Specification Tests
└── Infrastructure/                # Infrastructure Layer Tests
    ├── Repositories/              # Repository Tests
    └── Events/                    # Infrastructure Event Tests
```

#### **LiveEventService.IntegrationTests**
```
LiveEventService.IntegrationTests/
├── Infrastructure/                # Test host factories (incl. SQS)
├── Sqs/                           # SQS integration tests
├── GraphQL/                       # GraphQL integration tests
└── Api/                           # REST API tests
```

## Domain Event Flow Architecture

### 🔄 **Domain Event Processing Pipeline**

```
1. Domain Entity (Event/EventRegistration)
   ↓ Raises Domain Event
2. MediatRDomainEventDispatcher (Application Layer)
   ↓ Converts to MediatR Notification
3. Domain Event Handler (Application Layer)
   ↓ Processes Business Logic
4. Notification Adapter (Application Layer)
   ↓ Converts to External Notification
5. External Notification Service (Infrastructure Layer)
   ↓ SNS publish via Outbox Processor (topic per event type) for cross-service fan-out
```

### 📋 **Domain Event Handler Locations**

**✅ Correctly Located in Application Layer:**
- `LiveEventService.Application/Features/Events/DomainEventHandlers/`
  - `EventCapacityIncreasedDomainEventHandler.cs`
  - `EventRegistrationCancelledDomainEventHandler.cs`
  - `EventRegistrationCreatedDomainEventHandler.cs`
  - `EventRegistrationPromotedDomainEventHandler.cs`
  - `RegistrationWaitlistedDomainEventHandler.cs`
  - `WaitlistPositionChangedDomainEventHandler.cs`
  - `WaitlistRemovalDomainEventHandler.cs`

**✅ Correctly Located in Application Layer:**
- `LiveEventService.Application/Common/Notifications/`
  - `EventRegistrationDomainEventAdapters.cs`
  - `WaitlistDomainEventAdapters.cs`

**✅ Correctly Located in Application Layer:**
- `LiveEventService.Application/Common/MediatRDomainEventDispatcher.cs`

## Key Architectural Principles

### 🎯 **Dependency Direction**
- **API** → **Application** → **Core** ← **Infrastructure**
- Domain layer has no dependencies on other layers
- Infrastructure depends on Core interfaces
- Application orchestrates domain logic

### 🔄 **Domain Event Pattern**
- Domain entities raise domain events for significant state changes
- Application layer handles domain events through MediatR
- Domain events trigger side effects (notifications, updates)
- Maintains loose coupling between components

### 📦 **CQRS Pattern**
- Commands: Modify state (CreateEvent, RegisterForEvent, CancelRegistration)
- Queries: Read data (GetEvent, ListRegistrations)
- Separate models for read and write operations
- Optimized for different use cases

### 🧪 **Testing Strategy**
- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test component interactions with database
- **Architecture Tests**: Ensure architectural boundaries are respected

## Benefits of This Architecture

### 🎯 **Maintainability**
- Clear separation of concerns
- Domain logic isolated from infrastructure
- Easy to modify business rules without affecting other layers

### 🔄 **Testability**
- Domain logic can be tested without database
- Infrastructure can be mocked for unit tests
- Integration tests verify real component interactions

### 📈 **Scalability**
- Domain events enable asynchronous processing
- CQRS allows read/write optimization
- Clean boundaries enable microservice extraction

### 🛡️ **Reliability**
- Domain events ensure consistency
- Repository pattern abstracts data access
- Validation at multiple layers

## Recent Architectural Improvements

### ✅ **Completed Refactoring**
1. **Moved Domain Event Handlers**: From Infrastructure to Application layer
2. **Moved Notification Adapters**: From Infrastructure to Application layer  
3. **Moved MediatR Dispatcher**: From Infrastructure to Application layer
4. **Updated Namespaces**: All moved components now use Application layer namespaces
5. **Fixed Dependency Injection**: Proper registration of moved components
6. **Updated Unit Tests**: All tests now reference correct namespaces
7. **Implemented Outbox Processor**: Background service publishes outbox entries to AWS SNS

### 🎯 **Architectural Benefits Achieved**
- **Proper Layer Separation**: Domain event handlers now in correct layer
- **Clean Dependencies**: Infrastructure no longer contains application logic
- **Better Testability**: Domain event handlers can be tested independently
- **Consistent Patterns**: All domain event processing follows same pattern
- **External Fan-out**: SNS integration enables decoupled subscribers

## Configuration Files Explained

### 🎨 **`.editorconfig`**
**Purpose**: Ensures consistent code formatting across all editors and IDEs.

**Key Settings**:
- **C# Formatting**: Brace placement, indentation, spacing
- **Naming Conventions**: Interface prefixes, Pascal case rules
- **Code Style**: File-scoped namespaces, braces requirements
- **Multi-Language**: Different settings for JSON, YAML, XML

**Benefits**:
- Consistent formatting regardless of IDE/editor
- Automatic enforcement of coding standards
- Reduces code review discussions about formatting

### 🔧 **`global.json`**
**Purpose**: Specifies exact .NET SDK version for consistent builds.

**Key Settings**:
```json
{
  "sdk": {
    "version": "9.0.0",
    "rollForward": "latestMinor",
    "allowPrerelease": false
  }
}
```

**Benefits**:
- Ensures all team members use same .NET version
- Prevents "works on my machine" issues
- CI/CD uses same SDK version as development

### 📦 **`nuget.config`**
**Purpose**: Configures NuGet package sources and security settings.

**Key Features**:
- **Package Sources**: Official NuGet.org + private feeds
- **Security**: Trusted signers and package verification
- **Source Mapping**: Prevents package confusion attacks
- **Global Settings**: Shared package folder configuration

**Benefits**:
- Centralized package source management
- Enhanced security through trusted signers
- Consistent package restore across team

### 🎯 **`LiveEventService.sln.DotSettings`**
**Purpose**: ReSharper/Rider specific code analysis and formatting rules.

**Key Features**:
- **Code Formatting**: Method braces, spacing rules
- **Code Inspection**: Warning levels for different issues
- **File Headers**: Automatic copyright headers
- **Unit Testing**: Test session configurations

**Benefits**:
- Advanced code analysis beyond EditorConfig
- Team-wide code quality standards
- Automated code improvements

## Best Practices

### 📖 **Documentation Management**
1. **Keep Documentation Current**: Update docs with code changes
2. **Use Relative Links**: Link between documentation files
3. **Include Examples**: Provide code samples in documentation
4. **Version Documentation**: Tag documentation with releases

### ⚙️ **Configuration Management**
1. **Don't Override Standards**: Follow EditorConfig rules
2. **Team Consensus**: Discuss changes to coding standards
3. **IDE Agnostic**: Use EditorConfig over IDE-specific settings
4. **Security First**: Keep NuGet security settings enabled

### 🔄 **Maintenance**
1. **Regular Updates**: Update dependencies and SDK versions
2. **Review Settings**: Quarterly review of code standards
3. **Tool Updates**: Keep ReSharper/Rider rules current
4. **Documentation Review**: Monthly documentation accuracy check

### 🏗️ **Architecture Maintenance**
1. **Layer Boundaries**: Ensure dependencies flow in correct direction
2. **Domain Events**: Use domain events for cross-aggregate communication
3. **Repository Pattern**: Keep data access abstracted through interfaces
4. **CQRS Separation**: Maintain clear separation between commands and queries

## Quick Start for New Team Members

### 1. **Clone and Open**
```bash
git clone <repository-url>
cd LiveEventServiceDemo
# Open LiveEventService.sln in Visual Studio/Rider
```

### 2. **Verify Setup**
- ✅ Documentation appears in Solution Explorer
- ✅ EditorConfig formatting applied automatically
- ✅ ReSharper/Rider shows project-specific settings
- ✅ NuGet packages restore from configured sources
- ✅ All projects build successfully
- ✅ All tests pass

### 3. **Key Documentation to Read**
1. **`README.md`** - Project overview
2. **`LOCAL_DEVELOPMENT_SETUP.md`** - Environment setup
3. **`API_MINIMAL.md`** - API reference
4. **`WAITLIST_FUNCTIONALITY.md`** - Event waitlist features and implementation
5. **`DOMAIN_EVENTS_AND_GRAPHQL.md`** - Domain events and real-time notifications
6. **`CICD.md`** - Deployment process

### 4. **Understanding the Architecture**
1. **Start with Core**: Understand domain entities and business rules
2. **Review Application**: See how commands/queries orchestrate domain logic
3. **Examine Infrastructure**: Understand data persistence and external integrations
4. **Study Domain Events**: Learn how components communicate asynchronously

## Support

### 🆘 **Getting Help**
- **Documentation Issues**: Create GitHub issue with "documentation" label
- **Configuration Problems**: Check team standards in Configuration folder
- **IDE Setup**: Refer to EditorConfig and DotSettings files
- **DevOps Questions**: Review CICD.md and DevOps folder
- **Architecture Questions**: Review this document and domain events documentation

### 🔧 **Troubleshooting**
1. **Formatting Not Applied**: Check EditorConfig extension installed
2. **Wrong SDK Version**: Verify global.json SDK version
3. **Package Restore Issues**: Check nuget.config sources
4. **ReSharper Issues**: Reload DotSettings file
5. **Build Errors**: Ensure all projects reference correct namespaces
6. **Test Failures**: Check if domain event handlers are properly registered

---

**This organized solution structure provides enterprise-grade project organization with Clean Architecture principles, making the Live Event Service maintainable, testable, and developer-friendly!** 🌟 