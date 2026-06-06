using System.Reflection;
using NetArchTest.Rules;

namespace FluentPdf.ArchitectureTests;

public sealed class LayeringTests
{
    private const string Kernel = "FluentPdf.Kernel";
    private const string Domain = "FluentPdf.Domain";
    private const string Application = "FluentPdf.Application";
    private const string Infrastructure = "FluentPdf.Infrastructure";

    private static readonly Assembly KernelAssembly =
        typeof(FluentPdf.Kernel.AssemblyReference).Assembly;
    private static readonly Assembly DomainAssembly =
        typeof(FluentPdf.Domain.AssemblyReference).Assembly;
    private static readonly Assembly ApplicationAssembly =
        typeof(FluentPdf.Application.AssemblyReference).Assembly;

    [Fact]
    public void Kernel_does_not_depend_on_any_other_layer()
    {
        var result = Types.InAssembly(KernelAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Domain, Application, Infrastructure)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BecauseOf(result));
    }

    [Fact]
    public void Domain_depends_only_inward_on_the_kernel()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Application, Infrastructure)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BecauseOf(result));
    }

    [Fact]
    public void Application_does_not_depend_on_infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(Infrastructure)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BecauseOf(result));
    }

    [Fact]
    public void Concrete_domain_classes_are_sealed()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BecauseOf(result));
    }

    [Fact]
    public void Use_cases_are_named_with_the_use_case_suffix()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace("FluentPdf.Application.UseCases")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("UseCase")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BecauseOf(result));
    }

    private static string BecauseOf(TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : "the following types violate the rule: " +
              string.Join(", ", result.FailingTypeNames ?? []);
}
