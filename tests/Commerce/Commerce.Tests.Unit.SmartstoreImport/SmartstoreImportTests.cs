using Commerce.SmartstoreImport.Contracts;
using Commerce.SmartstoreImport.Infrastructure.Parsing;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Tests.Unit.SmartstoreImport;

public sealed class SmartstoreSqlParserTests
{
    [Fact]
    public void ParseFile_DiscoversTablesAndRows_FromSmallSample()
    {
        var parser = new SmartstoreSqlParser();
        var path = SmartstoreImportTestComposition.FixturePath("small-sample.sql");

        var result = parser.ParseFile(path);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Tables.ContainsKey("Product"));
        Assert.Equal(1, result.Value.Tables["Product"].Rows.Count);
        Assert.Equal(1, result.Value.Tables["Order"].Rows.Count);
        Assert.Equal("Sample Product", result.Value.Tables["Product"].Rows[0].Values["Name"]);
    }

    [Fact]
    public void InspectSchema_ReportsDiscoveredTables()
    {
        using var provider = SmartstoreImportTestComposition.BuildProvider();
        var service = provider.GetRequiredService<ISmartstoreImportService>();
        var path = SmartstoreImportTestComposition.FixturePath("full-sample.sql");

        var result = service.InspectSchemaAsync(path).GetAwaiter().GetResult();

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.Tables, t => t.TableName == "Manufacturer" && t.RowCount == 1);
        Assert.Contains(result.Value.Tables, t => t.TableName == "LocaleStringResource" && t.RowCount == 1);
    }
}

public sealed class SmartstoreImportWorkflowTests
{
    [Fact]
    public async Task ImportAsync_SmallMigration_ImportsCoreEntities()
    {
        using var provider = SmartstoreImportTestComposition.BuildProvider();
        var service = provider.GetRequiredService<ISmartstoreImportService>();
        var path = SmartstoreImportTestComposition.FixturePath("small-sample.sql");

        var result = await service.ImportAsync(new SmartstoreImportOptions(path));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.RecordsImported > 0);
        Assert.Contains(result.Value.EntitySummaries, s => s.EntityType == "Product" && s.ImportedCount == 1);
        Assert.Contains(result.Value.EntitySummaries, s => s.EntityType == "Store" && s.ImportedCount == 1);
    }

    [Fact]
    public async Task ImportAsync_FullMigration_ImportsExtendedEntities()
    {
        using var provider = SmartstoreImportTestComposition.BuildProvider();
        var service = provider.GetRequiredService<ISmartstoreImportService>();
        var path = SmartstoreImportTestComposition.FixturePath("full-sample.sql");

        var result = await service.ImportAsync(new SmartstoreImportOptions(path));

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.EntitySummaries, s => s.EntityType == "MediaAsset" && s.ImportedCount == 1);
        Assert.Contains(result.Value.EntitySummaries, s => s.EntityType == "UrlRecord" && s.ImportedCount == 1);
        Assert.Contains(result.Value.EntitySummaries, s => s.EntityType == "Manufacturer" && s.WarningCount == 1);
    }

    [Fact]
    public async Task ImportAsync_DuplicateMigration_IsBlockedByDefault()
    {
        using var provider = SmartstoreImportTestComposition.BuildProvider();
        var service = provider.GetRequiredService<ISmartstoreImportService>();
        var path = SmartstoreImportTestComposition.FixturePath("small-sample.sql");

        var first = await service.ImportAsync(new SmartstoreImportOptions(path));
        var second = await service.ImportAsync(new SmartstoreImportOptions(path));

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Contains("duplicate", second.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImportAsync_DuplicateMigration_AllowedWhenConfigured()
    {
        using var provider = SmartstoreImportTestComposition.BuildProvider();
        var service = provider.GetRequiredService<ISmartstoreImportService>();
        var path = SmartstoreImportTestComposition.FixturePath("small-sample.sql");

        var first = await service.ImportAsync(new SmartstoreImportOptions(path));
        var second = await service.ImportAsync(new SmartstoreImportOptions(path, AllowDuplicateRun: true));

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.True(second.Value.EntitySummaries.Sum(s => s.SkippedCount) > 0);
    }

    [Fact]
    public async Task ImportAsync_BrokenReferences_ReportsWarningsWithoutSilentDiscard()
    {
        using var provider = SmartstoreImportTestComposition.BuildProvider();
        var service = provider.GetRequiredService<ISmartstoreImportService>();
        var path = SmartstoreImportTestComposition.FixturePath("broken-references.sql");

        var result = await service.ImportAsync(new SmartstoreImportOptions(path));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.WarningCount > 0);
        Assert.Contains(result.Value.Issues, i => i.Code is "customer_ref_missing" or "entity_ref_missing");
    }

    [Fact]
    public async Task ImportAsync_MissingMedia_ReportsWarnings()
    {
        using var provider = SmartstoreImportTestComposition.BuildProvider();
        var service = provider.GetRequiredService<ISmartstoreImportService>();
        var path = SmartstoreImportTestComposition.FixturePath("missing-media.sql");

        var result = await service.ImportAsync(new SmartstoreImportOptions(path));

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.Issues, i => i.Code is "missing_media" or "media_ref_missing");
    }

    [Fact]
    public async Task ImportAsync_InvalidValues_ReportsWarnings()
    {
        using var provider = SmartstoreImportTestComposition.BuildProvider();
        var service = provider.GetRequiredService<ISmartstoreImportService>();
        var path = SmartstoreImportTestComposition.FixturePath("invalid-values.sql");

        var result = await service.ImportAsync(new SmartstoreImportOptions(path));

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.Issues, i => i.Code is "invalid_rate" or "invalid_rating");
    }
}
