namespace Commerce.Audit.Domain.Enums;

public enum AuditCategory
{
    Security = 0,
    Admin = 1,
    Order = 2,
    Payment = 3,
    Customer = 4,
    Settings = 5,
    Plugin = 6,
    Authorization = 7
}

public enum AuditActorType
{
    Anonymous = 0,
    Administrator = 1,
    Customer = 2,
    System = 3,
    ApiClient = 4
}
