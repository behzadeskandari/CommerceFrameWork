namespace Commerce.Cms.Domain.Enums;

public enum WidgetType
{
    HtmlBlock = 1,
    TopicEmbed = 2,
    MenuEmbed = 3
}

public enum MenuItemLinkType
{
    Url = 1,
    Page = 2,
    Topic = 3,
    Category = 4,
    Product = 5
}

public static class WidgetZoneNames
{
    public const string Header = "header";
    public const string MainContent = "main-content";
    public const string Sidebar = "sidebar";
    public const string Footer = "footer";
    public const string Homepage = "homepage";
    public const string ProductPage = "product-page";
    public const string CategoryPage = "category-page";

    public const string ProductBefore = "product-before";
    public const string ProductAfter = "product-after";
    public const string CategoryBefore = "category-before";
    public const string CategoryAfter = "category-after";

    public static IReadOnlyList<string> All =>
    [
        Header, MainContent, Sidebar, Footer, Homepage, ProductPage, CategoryPage,
        ProductBefore, ProductAfter, CategoryBefore, CategoryAfter
    ];
}
