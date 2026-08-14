using Commerce.SmartstoreImport.Contracts;
using Commerce.SmartstoreImport.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Tests.Unit.SmartstoreImport;

public sealed class SmartstoreReconciliationWorkflowTests
{
    [Fact]
    public async Task ReconcileAsync_AfterSmallImport_ReportsFullyReconciled()
    {
        using var provider = SmartstoreImportTestComposition.BuildProvider();
        var importService = provider.GetRequiredService<ISmartstoreImportService>();
        var reconciliationService = provider.GetRequiredService<ISmartstoreReconciliationService>();
        var path = SmartstoreImportTestComposition.FixturePath("small-sample.sql");

        var import = await importService.ImportAsync(new SmartstoreImportOptions(path));
        var result = await reconciliationService.ReconcileAsync(new SmartstoreReconciliationOptions(path));

        Assert.True(import.IsSuccess);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsFullyReconciled);
        Assert.Contains(result.Value.CheckSummaries, c => c.CheckName == "Products" && c.OverallClassification == ReconciliationClassification.Match);
        Assert.Contains(result.Value.CheckSummaries, c => c.CheckName == "StoreData" && c.MatchCount >= 3);
    }

    [Fact]
    public async Task ReconcileAsync_AfterFullImport_ReportsManufacturerNotApplicable()
    {
        using var provider = SmartstoreImportTestComposition.BuildProvider();
        var importService = provider.GetRequiredService<ISmartstoreImportService>();
        var reconciliationService = provider.GetRequiredService<ISmartstoreReconciliationService>();
        var path = SmartstoreImportTestComposition.FixturePath("full-sample.sql");

        await importService.ImportAsync(new SmartstoreImportOptions(path));
        var result = await reconciliationService.ReconcileAsync(new SmartstoreReconciliationOptions(path));

        Assert.True(result.IsSuccess);
        var manufacturers = result.Value!.CheckSummaries.Single(c => c.CheckName == "Manufacturers");
        Assert.Equal(ReconciliationClassification.NotApplicable, manufacturers.OverallClassification);
        Assert.Contains(result.Value.Discrepancies, d =>
            d.Classification == ReconciliationClassification.NotApplicable &&
            d.EntityType == "Manufacturer" &&
            !string.IsNullOrWhiteSpace(d.Remediation));
        Assert.Contains(result.Value.CheckSummaries, c => c.CheckName == "Media" && c.MatchCount == 1);
        Assert.Contains(result.Value.CheckSummaries, c => c.CheckName == "SeoUrls" && c.MatchCount == 1);
    }

    [Fact]
    public async Task ReconcileAsync_BrokenReferences_ReportsMissingDiscrepancies()
    {
        using var provider = SmartstoreImportTestComposition.BuildProvider();
        var importService = provider.GetRequiredService<ISmartstoreImportService>();
        var reconciliationService = provider.GetRequiredService<ISmartstoreReconciliationService>();
        var path = SmartstoreImportTestComposition.FixturePath("broken-references.sql");

        await importService.ImportAsync(new SmartstoreImportOptions(path));
        var result = await reconciliationService.ReconcileAsync(new SmartstoreReconciliationOptions(path));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsFullyReconciled);
        Assert.Contains(result.Value.Discrepancies, d => d.Classification == ReconciliationClassification.Missing);
        Assert.True(result.Value.Discrepancies.All(d =>
            !string.IsNullOrWhiteSpace(d.Explanation) && !string.IsNullOrWhiteSpace(d.Remediation)));
    }

    [Fact]
    public async Task ReconcileAsync_WithoutImportRun_Fails()
    {
        using var provider = SmartstoreImportTestComposition.BuildProvider();
        var reconciliationService = provider.GetRequiredService<ISmartstoreReconciliationService>();
        var path = SmartstoreImportTestComposition.FixturePath("small-sample.sql");

        var result = await reconciliationService.ReconcileAsync(new SmartstoreReconciliationOptions(path));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ReconcileAsync_AllDiscrepanciesHaveRemediation()
    {
        using var provider = SmartstoreImportTestComposition.BuildProvider();
        var importService = provider.GetRequiredService<ISmartstoreImportService>();
        var reconciliationService = provider.GetRequiredService<ISmartstoreReconciliationService>();
        var path = SmartstoreImportTestComposition.FixturePath("full-sample.sql");

        await importService.ImportAsync(new SmartstoreImportOptions(path));
        var result = await reconciliationService.ReconcileAsync(new SmartstoreReconciliationOptions(path));

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!.Discrepancies);
        Assert.All(result.Value.Discrepancies, d =>
        {
            Assert.False(string.IsNullOrWhiteSpace(d.Explanation));
            Assert.False(string.IsNullOrWhiteSpace(d.Remediation));
        });
    }
}
