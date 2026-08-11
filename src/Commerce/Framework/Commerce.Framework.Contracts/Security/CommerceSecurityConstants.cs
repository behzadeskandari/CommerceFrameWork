namespace Commerce.Framework.Contracts.Security;

public static class CommerceClaimTypes
{
    public const string Permission = "commerce:permission";

    public const string CustomerId = "commerce:customer_id";
}

public static class CommerceRoles
{
    public const string Administrator = "Administrator";

    public const string Customer = "Customer";
}

public static class CommercePolicies
{
    public const string Prefix = "Permission:";

    public static string ForPermission(string permission) => Prefix + permission;
}
