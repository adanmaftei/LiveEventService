using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace LiveEventService.UnitTests.Common;

/// <summary>
/// Base class for all unit tests providing common functionality
/// </summary>
public abstract class TestBase
{
    protected readonly IFixture Fixture;
    protected readonly Mock<ILogger> MockLogger;

    protected TestBase()
    {
        Fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new AutoFixtureCustomization());
        
        MockLogger = new Mock<ILogger>();
    }

    /// <summary>
    /// Creates a mock logger for a specific type
    /// </summary>
    protected Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    /// <summary>
    /// Asserts that a collection contains the expected number of items
    /// </summary>
    protected void AssertCollectionCount<T>(IEnumerable<T> collection, int expectedCount, string? message = null)
    {
        collection.Should().HaveCount(expectedCount, message);
    }

    /// <summary>
    /// Asserts that a collection is empty
    /// </summary>
    protected void AssertCollectionEmpty<T>(IEnumerable<T> collection, string? message = null)
    {
        collection.Should().BeEmpty(message);
    }

    /// <summary>
    /// Asserts that a collection is not empty
    /// </summary>
    protected void AssertCollectionNotEmpty<T>(IEnumerable<T> collection, string? message = null)
    {
        collection.Should().NotBeEmpty(message);
    }

    /// <summary>
    /// Asserts that an object is not null
    /// </summary>
    protected void AssertNotNull<T>(T? obj, string? message = null) where T : class
    {
        obj.Should().NotBeNull(message);
    }

    /// <summary>
    /// Asserts that an object is null
    /// </summary>
    protected void AssertNull<T>(T? obj, string? message = null) where T : class
    {
        obj.Should().BeNull(message);
    }

    /// <summary>
    /// Asserts that a nullable value type is null
    /// </summary>
    protected void AssertNull<T>(T? obj, string? message = null) where T : struct
    {
        obj.Should().BeNull(message);
    }

    /// <summary>
    /// Asserts that a nullable value type is not null
    /// </summary>
    protected void AssertNotNull<T>(T? obj, string? message = null) where T : struct
    {
        obj.Should().NotBeNull(message);
    }

    /// <summary>
    /// Asserts that two objects are equal
    /// </summary>
    protected void AssertEqual<T>(T expected, T actual, string? message = null)
    {
        actual.Should().Be(expected, message);
    }

    /// <summary>
    /// Asserts that two objects are not equal
    /// </summary>
    protected void AssertNotEqual<T>(T expected, T actual, string? message = null)
    {
        actual.Should().NotBe(expected, message);
    }

    /// <summary>
    /// Asserts that a condition is true
    /// </summary>
    protected void AssertTrue(bool condition, string? message = null)
    {
        condition.Should().BeTrue(message);
    }

    /// <summary>
    /// Asserts that a condition is false
    /// </summary>
    protected void AssertFalse(bool condition, string? message = null)
    {
        condition.Should().BeFalse(message);
    }
}

/// <summary>
/// Custom AutoFixture customization for the LiveEventService domain
/// </summary>
public class AutoFixtureCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        // Configure AutoFixture to create realistic test data
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Configure string generation to avoid nulls
        fixture.Customize<string>(composer => composer.FromFactory(() => Guid.NewGuid().ToString("N")));
        
        // Configure DateTime generation to use UTC
        fixture.Customize<DateTime>(composer => composer.FromFactory(() => DateTime.UtcNow));
        
        // Configure Guid generation
        fixture.Customize<Guid>(composer => composer.FromFactory(() => Guid.NewGuid()));
    }
}

/// <summary>
/// Custom AutoData attribute that uses our customizations
/// </summary>
public class CustomAutoDataAttribute : AutoDataAttribute
{
    public CustomAutoDataAttribute() : base(() => new Fixture()
        .Customize(new AutoMoqCustomization())
        .Customize(new AutoFixtureCustomization()))
    {
    }
}

/// <summary>
/// Custom InlineAutoData attribute that uses our customizations
/// </summary>
public class CustomInlineAutoDataAttribute : InlineAutoDataAttribute
{
    public CustomInlineAutoDataAttribute(params object[] values) : base(new CustomAutoDataAttribute(), values)
    {
    }
}