namespace Commerce.Tests.Architecture;

public sealed class PluginArchitectureTests
{
    [Fact]
    public void Host_DoesNotReferenceManualPaymentPlugin()
    {
        var references = typeof(Program).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, x => x.Equals("Commerce.Plugin.Payment.Manual", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Plugin.Test", StringComparison.Ordinal));
    }

    [Fact]
    public void Host_DoesNotReferenceConcreteProviderPlugins()
    {
        var references = typeof(Program).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(x => x.StartsWith("Commerce.Plugin.", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(references);
    }

    [Fact]
    public void FrameworkPlugins_DoesNotReferenceAnyPlugin()
    {
        var references = typeof(Commerce.Framework.Plugins.DependencyInjection.ServiceCollectionExtensions).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(x => x.StartsWith("Commerce.Plugin.", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(references);
    }

    [Fact]
    public void ManualPlugin_ReferencesOnlyPaymentsContractsAndPluginContracts()
    {
        var references = typeof(Commerce.Plugin.Payment.Manual.ManualPaymentProvider).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(x => x.StartsWith("Commerce.", StringComparison.Ordinal))
            .ToList();

        Assert.Contains(references, x => x.Equals("Commerce.Payments.Contracts", StringComparison.Ordinal));
        Assert.Contains(references, x => x.Equals("Commerce.Framework.PluginContracts", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Payments.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Payments.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Host", StringComparison.Ordinal));
    }

    [Fact]
    public void TestPlugin_ReferencesOnlyPluginContractsAndFrameworkData()
    {
        var references = typeof(Commerce.Plugin.Test.TestPlugin).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(x => x.StartsWith("Commerce.", StringComparison.Ordinal))
            .ToList();

        Assert.Contains(references, x => x.Equals("Commerce.Framework.PluginContracts", StringComparison.Ordinal));
        Assert.Contains(references, x => x.Equals("Commerce.Framework.Data", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.StartsWith("Commerce.Host", StringComparison.Ordinal));
    }

    [Fact]
    public void ZarinPalPlugin_ReferencesOnlyPaymentsContractsAndPluginContracts()
    {
        var references = typeof(Commerce.Plugin.Payment.ZarinPal.ZarinPalPaymentProvider).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(x => x.StartsWith("Commerce.", StringComparison.Ordinal))
            .ToList();

        Assert.Contains(references, x => x.Equals("Commerce.Payments.Contracts", StringComparison.Ordinal));
        Assert.Contains(references, x => x.Equals("Commerce.Framework.PluginContracts", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Host", StringComparison.Ordinal));
    }

    [Fact]
    public void StripePlugin_ReferencesOnlyPaymentsContractsAndPluginContracts()
    {
        var references = typeof(Commerce.Plugin.Payment.Stripe.StripePaymentProvider).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(x => x.StartsWith("Commerce.", StringComparison.Ordinal))
            .ToList();

        Assert.Contains(references, x => x.Equals("Commerce.Payments.Contracts", StringComparison.Ordinal));
        Assert.Contains(references, x => x.Equals("Commerce.Framework.PluginContracts", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.Equals("Commerce.Host", StringComparison.Ordinal));
    }

    [Fact]
    public void PluginMvcExtensions_ExistsForDynamicControllerDiscovery()
    {
        var method = typeof(Commerce.Framework.Plugins.Mvc.PluginMvcExtensions)
            .GetMethod(nameof(Commerce.Framework.Plugins.Mvc.PluginMvcExtensions.AddCommercePluginControllers));

        Assert.NotNull(method);
    }
}
