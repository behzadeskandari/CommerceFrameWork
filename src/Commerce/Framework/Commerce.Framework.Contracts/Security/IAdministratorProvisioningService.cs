using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.Core.Results;

namespace Commerce.Framework.Contracts.Security;

public interface IAdministratorProvisioningService
{
    Task<Result> CreateAdministratorAsync(AdministratorSetupRequest request, CancellationToken cancellationToken = default);

    Task<bool> HasAdministratorAsync(CancellationToken cancellationToken = default);
}
