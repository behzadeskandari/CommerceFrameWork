namespace Commerce.Plugin.Payment.ZarinPal;

internal static class ZarinPalSettingKeys
{
    public const string MerchantId = "Payment.ZarinPal.MerchantId";
    public const string Sandbox = "Payment.ZarinPal.Sandbox";
    public const string CallbackBaseUrl = "Payment.ZarinPal.CallbackBaseUrl";
}

internal static class ZarinPalEndpoints
{
    public const string ProductionApi = "https://api.zarinpal.com/pg/v4/payment";
    public const string SandboxApi = "https://sandbox.zarinpal.com/pg/v4/payment";
    public const string ProductionStartPay = "https://www.zarinpal.com/pg/StartPay";
    public const string SandboxStartPay = "https://sandbox.zarinpal.com/pg/StartPay";
}
