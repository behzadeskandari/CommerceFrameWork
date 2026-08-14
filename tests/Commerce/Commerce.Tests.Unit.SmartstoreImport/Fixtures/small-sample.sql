-- Representative Smartstore export subset for Phase 46 tests.
-- Schema columns discovered from CREATE TABLE sections below; do not extend without inspecting source SQL.

CREATE TABLE [dbo].[Language] (
    [Id] INT NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [UniqueSeoCode] NVARCHAR(10) NOT NULL,
    [LanguageCulture] NVARCHAR(20) NOT NULL,
    [Rtl] BIT NOT NULL,
    [DisplayOrder] INT NOT NULL,
    [Published] BIT NOT NULL
);

CREATE TABLE [dbo].[Currency] (
    [Id] INT NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [CurrencyCode] NVARCHAR(5) NOT NULL,
    [Rate] DECIMAL(18,8) NOT NULL,
    [DisplayOrder] INT NOT NULL,
    [Published] BIT NOT NULL
);

CREATE TABLE [dbo].[Store] (
    [Id] INT NOT NULL,
    [Name] NVARCHAR(400) NOT NULL,
    [Url] NVARCHAR(400) NOT NULL,
    [DisplayOrder] INT NOT NULL,
    [PrimaryStoreCurrencyId] INT NOT NULL
);

CREATE TABLE [dbo].[Category] (
    [Id] INT NOT NULL,
    [Name] NVARCHAR(400) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [Published] BIT NOT NULL,
    [DisplayOrder] INT NOT NULL,
    [ParentCategoryId] INT NOT NULL
);

CREATE TABLE [dbo].[Product] (
    [Id] INT NOT NULL,
    [Name] NVARCHAR(400) NOT NULL,
    [Sku] NVARCHAR(400) NOT NULL,
    [ShortDescription] NVARCHAR(1000) NULL,
    [FullDescription] NVARCHAR(MAX) NULL,
    [Published] BIT NOT NULL,
    [Deleted] BIT NOT NULL,
    [DisplayOrder] INT NOT NULL,
    [ProductTypeId] INT NOT NULL,
    [Price] DECIMAL(18,4) NOT NULL,
    [Weight] DECIMAL(18,4) NOT NULL
);

CREATE TABLE [dbo].[Customer] (
    [Id] INT NOT NULL,
    [Email] NVARCHAR(500) NULL,
    [FirstName] NVARCHAR(225) NULL,
    [LastName] NVARCHAR(225) NULL,
    [Active] BIT NOT NULL,
    [Deleted] BIT NOT NULL,
    [IsSystemAccount] BIT NOT NULL
);

CREATE TABLE [dbo].[Order] (
    [Id] INT NOT NULL,
    [OrderNumber] NVARCHAR(400) NOT NULL,
    [StoreId] INT NOT NULL,
    [CustomerId] INT NOT NULL,
    [CustomerCurrencyCode] NVARCHAR(5) NOT NULL,
    [OrderSubtotalInclTax] DECIMAL(18,4) NOT NULL,
    [OrderDiscount] DECIMAL(18,4) NOT NULL,
    [OrderShippingInclTax] DECIMAL(18,4) NOT NULL,
    [OrderTax] DECIMAL(18,4) NOT NULL,
    [OrderTotal] DECIMAL(18,4) NOT NULL,
    [OrderStatusId] INT NOT NULL,
    [PaymentStatusId] INT NOT NULL,
    [ShippingStatusId] INT NOT NULL
);

CREATE TABLE [dbo].[OrderItem] (
    [Id] INT NOT NULL,
    [OrderId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [Quantity] INT NOT NULL,
    [UnitPriceInclTax] DECIMAL(18,4) NOT NULL,
    [PriceInclTax] DECIMAL(18,4) NOT NULL,
    [ProductName] NVARCHAR(400) NOT NULL,
    [Sku] NVARCHAR(400) NOT NULL
);

INSERT INTO [dbo].[Language] ([Id], [Name], [UniqueSeoCode], [LanguageCulture], [Rtl], [DisplayOrder], [Published]) VALUES (1, N'English', N'en', N'en-US', 0, 0, 1);
INSERT INTO [dbo].[Currency] ([Id], [Name], [CurrencyCode], [Rate], [DisplayOrder], [Published]) VALUES (1, N'US Dollar', N'USD', 1.00000000, 0, 1);
INSERT INTO [dbo].[Store] ([Id], [Name], [Url], [DisplayOrder], [PrimaryStoreCurrencyId]) VALUES (1, N'Demo Store', N'https://demo.local/', 0, 1);
INSERT INTO [dbo].[Category] ([Id], [Name], [Description], [Published], [DisplayOrder], [ParentCategoryId]) VALUES (1, N'Root Category', N'Root', 1, 0, 0);
INSERT INTO [dbo].[Product] ([Id], [Name], [Sku], [ShortDescription], [FullDescription], [Published], [Deleted], [DisplayOrder], [ProductTypeId], [Price], [Weight]) VALUES (1, N'Sample Product', N'SKU-1', N'Short', N'Full description', 1, 0, 0, 5, 19.9900, 0.5000);
INSERT INTO [dbo].[Customer] ([Id], [Email], [FirstName], [LastName], [Active], [Deleted], [IsSystemAccount]) VALUES (1, N'customer@example.com', N'Jane', N'Doe', 1, 0, 0);
INSERT INTO [dbo].[Order] ([Id], [OrderNumber], [StoreId], [CustomerId], [CustomerCurrencyCode], [OrderSubtotalInclTax], [OrderDiscount], [OrderShippingInclTax], [OrderTax], [OrderTotal], [OrderStatusId], [PaymentStatusId], [ShippingStatusId]) VALUES (1, N'SS-1001', 1, 1, N'USD', 19.9900, 0.0000, 0.0000, 0.0000, 19.9900, 30, 30, 10);
INSERT INTO [dbo].[OrderItem] ([Id], [OrderId], [ProductId], [Quantity], [UnitPriceInclTax], [PriceInclTax], [ProductName], [Sku]) VALUES (1, 1, 1, 1, 19.9900, 19.9900, N'Sample Product', N'SKU-1');
