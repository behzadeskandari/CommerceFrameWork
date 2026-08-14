using Commerce.Framework.Contracts.Security;



namespace Commerce.Payments.Infrastructure.Security;



public static class PaymentsPermissions

{

    public const string View = "Payments.View";

    public const string Manage = "Payments.Manage";

    public const string Refund = "Payments.Refund";

    public const string Configure = "Payments.Configure";

    public const string GiftCardsView = "Payments.GiftCards.View";

    public const string GiftCardsManage = "Payments.GiftCards.Manage";
}



public sealed class PaymentsPermissionContributor : IModulePermissionContributor

{

    public string ModuleSystemName => "Commerce.Payments";



    public IReadOnlyList<PermissionDefinition> GetPermissions() =>

    [

        new(PaymentsPermissions.View, "View payments.", ModuleSystemName),

        new(PaymentsPermissions.Manage, "Manage payments and capture/void.", ModuleSystemName),

        new(PaymentsPermissions.Refund, "Refund payments.", ModuleSystemName),

        new(PaymentsPermissions.Configure, "Configure payment methods and providers.", ModuleSystemName),

        new(PaymentsPermissions.GiftCardsView, "View gift cards.", ModuleSystemName),

        new(PaymentsPermissions.GiftCardsManage, "Manage gift cards.", ModuleSystemName)

    ];

}

