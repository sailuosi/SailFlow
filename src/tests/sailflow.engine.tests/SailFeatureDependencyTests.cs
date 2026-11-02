using saiflow.engine.Client.Hosting;
using Xunit;

namespace saiflow.engine.tests;

public class SailFeatureDependencyTests
{
    [Fact]
    public void Validate_WhenDependencyMissing_Throws()
    {
        var services = new SailCollection();
        services.AddFeature(new SailFeatureDescriptor
        {
            Name = "child",
            Phase = SailFeaturePhase.ConfigureClient,
            DependsOn = new[] { "parent" },
        });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            SailFeatureDependencyValidator.Validate(services));

        Assert.Contains("parent", ex.Message, StringComparison.Ordinal);
        Assert.Contains("child", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WhenDependencyPresent_AcrossPhases_Ok()
    {
        var services = new SailCollection();
        services.ProvideFeature("parent", SailFeaturePhase.ConfigureServices);
        services.AddFeature(new SailFeatureDescriptor
        {
            Name = "child",
            Phase = SailFeaturePhase.PostBuild,
            DependsOn = new[] { "parent" },
        });

        SailFeatureDependencyValidator.Validate(services);
    }

    [Fact]
    public void ProvideFeature_IsIdempotent_SameNamePhase()
    {
        var services = new SailCollection();
        services.ProvideFeature("json", order: 1);
        services.ProvideFeature("json", order: 2);

        Assert.Single(services);
        Assert.Equal(2, services[0].Order);
    }

    [Fact]
    public void Validate_EmptyDependsOn_Ok()
    {
        var services = new SailCollection();
        services.ProvideFeature("solo");
        SailFeatureDependencyValidator.Validate(services);
    }
}
