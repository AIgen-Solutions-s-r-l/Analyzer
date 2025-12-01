using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace AnalyzerCore.Architecture.Tests;

public class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Entities.Token).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Api.Program).Assembly;

    #region Layer Dependency Tests

    [Fact]
    public void Domain_Should_Not_Have_Dependencies_On_Other_Layers()
    {
        // Arrange
        var otherProjects = new[]
        {
            "AnalyzerCore.Application",
            "AnalyzerCore.Infrastructure",
            "AnalyzerCore.Api"
        };

        // Act
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(otherProjects)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer should not depend on any other layer. " +
                     $"Failing types: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Application_Should_Not_Have_Dependencies_On_Infrastructure_Or_Api()
    {
        // Arrange
        var forbiddenProjects = new[]
        {
            "AnalyzerCore.Infrastructure",
            "AnalyzerCore.Api"
        };

        // Act
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenProjects)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "Application layer should only depend on Domain layer. " +
                     $"Failing types: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Infrastructure_Should_Not_Have_Dependencies_On_Api()
    {
        // Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("AnalyzerCore.Api")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "Infrastructure layer should not depend on Api layer. " +
                     $"Failing types: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Array.Empty<string>())}");
    }

    #endregion

    #region Naming Convention Tests

    [Fact]
    public void Commands_Should_Be_Named_With_Command_Suffix()
    {
        // Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(Application.Abstractions.Messaging.IBaseCommand))
            .Should()
            .HaveNameEndingWith("Command")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "All commands should be named with 'Command' suffix. " +
                     $"Failing types: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void CommandHandlers_Should_Be_Named_With_CommandHandler_Suffix()
    {
        // Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("CommandHandler")
            .Should()
            .BeClasses()
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Queries_Should_Be_Named_With_Query_Suffix()
    {
        // Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Query")
            .And()
            .AreNotInterfaces()
            .Should()
            .BeClasses()
            .Or()
            .BeSealed()
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Validators_Should_Be_Named_With_Validator_Suffix()
    {
        // Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "All validators should be named with 'Validator' suffix. " +
                     $"Failing types: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Repositories_Should_Be_Named_With_Repository_Suffix()
    {
        // Act
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace("AnalyzerCore.Infrastructure.Persistence.Repositories")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Repository")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    #endregion

    #region Domain Model Tests

    [Fact]
    public void Entities_Should_Have_Private_Parameterless_Constructor()
    {
        // Arrange
        var entityTypes = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(Domain.Abstractions.Entity<>))
            .GetTypes();

        // Assert
        foreach (var entityType in entityTypes)
        {
            var hasPrivateParameterlessConstructor = entityType
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .Any(c => c.GetParameters().Length == 0);

            hasPrivateParameterlessConstructor.Should().BeTrue(
                because: $"Entity {entityType.Name} should have a private parameterless constructor for EF Core");
        }
    }

    [Fact]
    public void Domain_Events_Should_Be_Sealed_Records()
    {
        // Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(Domain.Abstractions.IDomainEvent))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "Domain events should be sealed records for immutability. " +
                     $"Failing types: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Value_Objects_Should_Be_Sealed()
    {
        // Act
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("AnalyzerCore.Domain.ValueObjects")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "Value objects should be sealed for immutability. " +
                     $"Failing types: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Array.Empty<string>())}");
    }

    #endregion

    #region Application Layer Tests

    [Fact]
    public void Handlers_Should_Not_Have_Public_Methods_Other_Than_Handle()
    {
        // Arrange
        var handlerTypes = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .GetTypes();

        // Assert
        foreach (var handlerType in handlerTypes)
        {
            var publicMethods = handlerType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName) // Exclude property getters/setters
                .Select(m => m.Name)
                .ToList();

            publicMethods.Should().OnlyContain(
                name => name == "Handle",
                because: $"Handler {handlerType.Name} should only expose the Handle method. " +
                         $"Found: {string.Join(", ", publicMethods)}");
        }
    }

    [Fact]
    public void Commands_Should_Be_Sealed()
    {
        // Act
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(Application.Abstractions.Messaging.IBaseCommand))
            .Should()
            .BeSealed()
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            because: "Commands should be sealed to prevent inheritance. " +
                     $"Failing types: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Array.Empty<string>())}");
    }

    #endregion

    #region Infrastructure Layer Tests

    [Fact]
    public void Repository_Implementations_Should_Implement_Domain_Interfaces()
    {
        // Arrange
        var repositoryTypes = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveNameEndingWith("Repository")
            .And()
            .AreClasses()
            .GetTypes();

        // Assert
        foreach (var repoType in repositoryTypes)
        {
            var implementsDomainInterface = repoType
                .GetInterfaces()
                .Any(i => i.Namespace?.StartsWith("AnalyzerCore.Domain") == true);

            implementsDomainInterface.Should().BeTrue(
                because: $"Repository {repoType.Name} should implement a domain interface");
        }
    }

    [Fact]
    public void DbContext_Should_Implement_IUnitOfWork()
    {
        // Act
        var dbContextType = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveNameEndingWith("DbContext")
            .GetTypes()
            .FirstOrDefault();

        // Assert
        dbContextType.Should().NotBeNull();
        dbContextType!.GetInterfaces()
            .Should().Contain(typeof(Domain.Abstractions.IUnitOfWork),
                because: "DbContext should implement IUnitOfWork for transaction management");
    }

    #endregion
}
