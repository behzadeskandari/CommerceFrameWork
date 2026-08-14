using Commerce.SmartstoreImport.Application.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.SmartstoreImport.Application.Services;

// Import orchestration is implemented in Commerce.SmartstoreImport.Infrastructure.Import.SmartstoreImportService.

public static class SmartstoreImportApplicationMarker
{
    public static IServiceCollection AddSmartstoreImportApplication(this IServiceCollection services) =>
        ServiceCollectionExtensions.AddSmartstoreImportApplication(services);
}
