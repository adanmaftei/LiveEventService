using NetArchTest.Rules;
using Xunit;

namespace LiveEventService.Architecture.Tests;

public class ArchitectureTests
{
    private const string CoreNamespace = "LiveEventService.Core";
    private const string ApplicationNamespace = "LiveEventService.Application";
    private const string InfrastructureNamespace = "LiveEventService.Infrastructure";
    private const string ApiNamespace = "LiveEventService.API";

    [Fact]
    public void Core_Should_Not_Have_Dependencies_On_Other_Layers()
    {
        var result = Types.InNamespace(CoreNamespace)
            .That()
            .DoNotHaveDependencyOn(ApplicationNamespace)
            .And()
            .DoNotHaveDependencyOn(InfrastructureNamespace)
            .And()
            .DoNotHaveDependencyOn(ApiNamespace)
            .Should()
            .BeClasses()
            .GetResult();

        Assert.True(result.IsSuccessful, "Core layer should not depend on other layers");
    }

    [Fact]
    public void Application_Should_Not_Have_Dependencies_On_Infrastructure_Or_API()
    {
        var result = Types.InNamespace(ApplicationNamespace)
            .That()
            .DoNotHaveDependencyOn(InfrastructureNamespace)
            .And()
            .DoNotHaveDependencyOn(ApiNamespace)
            .Should()
            .BeClasses()
            .GetResult();

        Assert.True(result.IsSuccessful, "Application layer should not depend on Infrastructure or API layers");
    }

    [Fact]
    public void Infrastructure_Should_Not_Have_Dependencies_On_API()
    {
        var result = Types.InNamespace(InfrastructureNamespace)
            .That()
            .DoNotHaveDependencyOn(ApiNamespace)
            .Should()
            .BeClasses()
            .GetResult();

        Assert.True(result.IsSuccessful, "Infrastructure layer should not depend on API layer");
    }

    [Fact]
    public void Core_Should_Not_Have_Framework_Dependencies()
    {
        var result = Types.InNamespace(CoreNamespace)
            .That()
            .AreClasses()
            .And()
            .DoNotHaveDependencyOn("MediatR")
            .And()
            .DoNotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .And()
            .DoNotHaveDependencyOn("HotChocolate")
            .And()
            .DoNotHaveDependencyOn("Microsoft.AspNetCore")
            .Should()
            .BeClasses()
            .GetResult();

        Assert.True(result.IsSuccessful, $"Core layer should not have framework dependencies. Violating types: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Application_Should_Not_Have_Data_Access_Dependencies()
    {
        var result = Types.InNamespace(ApplicationNamespace)
            .That()
            .AreClasses()
            .And()
            .DoNotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .And()
            .DoNotHaveDependencyOn("Npgsql")
            .And()
            .DoNotHaveDependencyOn("Microsoft.EntityFrameworkCore.Design")
            .Should()
            .BeClasses()
            .GetResult();

        Assert.True(result.IsSuccessful, $"Application layer should not have data access dependencies. Violating types: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Command_Handlers_Should_Implement_ICommandHandler()
    {
        var result = Types.InNamespace(ApplicationNamespace)
            .That()
            .HaveNameEndingWith("CommandHandler")
            .Should()
            .ImplementInterface(typeof(LiveEventService.Application.Common.Interfaces.ICommandHandler<,>))
            .GetResult();

        Assert.True(result.IsSuccessful, "Command handlers should implement ICommandHandler interface");
    }

    [Fact]
    public void Query_Handlers_Should_Implement_IQueryHandler()
    {
        var result = Types.InNamespace(ApplicationNamespace)
            .That()
            .HaveNameEndingWith("QueryHandler")
            .Should()
            .ImplementInterface(typeof(LiveEventService.Application.Common.Interfaces.IQueryHandler<,>))
            .GetResult();

        Assert.True(result.IsSuccessful, "Query handlers should implement IQueryHandler interface");
    }

    [Fact]
    public void Commands_Should_Implement_IRequest()
    {
        var result = Types.InNamespace(ApplicationNamespace)
            .That()
            .HaveNameEndingWith("Command")
            .And()
            .DoNotHaveNameEndingWith("Handler")
            .Should()
            .ImplementInterface(typeof(MediatR.IRequest<>))
            .GetResult();

        Assert.True(result.IsSuccessful, "Commands should implement IRequest interface");
    }

    [Fact]
    public void Queries_Should_Implement_IRequest()
    {
        var result = Types.InNamespace(ApplicationNamespace)
            .That()
            .HaveNameEndingWith("Query")
            .And()
            .DoNotHaveNameEndingWith("Handler")
            .Should()
            .ImplementInterface(typeof(MediatR.IRequest<>))
            .GetResult();

        Assert.True(result.IsSuccessful, "Queries should implement IRequest interface");
    }

    [Fact]
    public void Domain_Entities_Should_Inherit_From_Entity_Base_Class()
    {
        var result = Types.InNamespace(CoreNamespace)
            .That()
            .AreClasses()
            .And()
            .HaveName("Event")
            .Or()
            .HaveName("User")
            .Or()
            .HaveName("EventRegistration")
            .Should()
            .Inherit(typeof(LiveEventService.Core.Common.Entity))
            .GetResult();

        Assert.True(result.IsSuccessful, "Domain entities should inherit from Entity base class");
    }

    [Fact]
    public void Domain_Events_Should_Inherit_From_DomainEvent()
    {
        var result = Types.InNamespace(CoreNamespace)
            .That()
            .HaveName("EventRegistrationCreatedDomainEvent")
            .Or()
            .HaveName("EventRegistrationPromotedDomainEvent")
            .Or()
            .HaveName("EventRegistrationCancelledDomainEvent")
            .Should()
            .Inherit(typeof(LiveEventService.Core.Common.DomainEvent))
            .GetResult();

        Assert.True(result.IsSuccessful, "Domain events should inherit from DomainEvent base class");
    }

    [Fact]
    public void Validators_Should_Inherit_From_AbstractValidator()
    {
        var result = Types.InNamespace(ApplicationNamespace)
            .That()
            .HaveNameEndingWith("Validator")
            .Should()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .GetResult();

        Assert.True(result.IsSuccessful, "Validators should inherit from AbstractValidator");
    }

    [Fact]
    public void Infrastructure_Should_Not_Reference_Application()
    {
        var result = Types.InNamespace(InfrastructureNamespace)
            .That()
            .DoNotHaveDependencyOn(ApplicationNamespace)
            .Should()
            .BeClasses()
            .GetResult();

        Assert.True(result.IsSuccessful, "Infrastructure should not reference Application layer");
    }

    [Fact]
    public void Core_Should_Only_Have_Acceptable_External_Dependencies()
    {
        // Core should only have utility libraries, not framework dependencies
        var result = Types.InNamespace(CoreNamespace)
            .That()
            .HaveDependencyOn("Ardalis.GuardClauses")
            .Or()
            .HaveDependencyOn("System.Text.Json")
            .Should()
            .BeClasses()
            .GetResult();

        // These are acceptable dependencies
        Assert.True(result.IsSuccessful, "Core should only have acceptable utility dependencies");
    }
}
