CREATE TABLE [dbo].[Language] ([Id] INT, [Name] NVARCHAR(200), [UniqueSeoCode] NVARCHAR(10), [LanguageCulture] NVARCHAR(20), [Rtl] BIT, [DisplayOrder] INT, [Published] BIT);
CREATE TABLE [dbo].[Currency] ([Id] INT, [Name] NVARCHAR(200), [CurrencyCode] NVARCHAR(5), [Rate] DECIMAL(18,8), [DisplayOrder] INT, [Published] BIT);
CREATE TABLE [dbo].[Store] ([Id] INT, [Name] NVARCHAR(400), [Url] NVARCHAR(400), [DisplayOrder] INT, [PrimaryStoreCurrencyId] INT);
CREATE TABLE [dbo].[Product] ([Id] INT, [Name] NVARCHAR(400), [Sku] NVARCHAR(400), [ShortDescription] NVARCHAR(1000), [FullDescription] NVARCHAR(MAX), [Published] BIT, [Deleted] BIT, [DisplayOrder] INT, [ProductTypeId] INT, [Price] DECIMAL(18,4), [Weight] DECIMAL(18,4));
CREATE TABLE [dbo].[Customer] ([Id] INT, [Email] NVARCHAR(500), [FirstName] NVARCHAR(225), [LastName] NVARCHAR(225), [Active] BIT, [Deleted] BIT, [IsSystemAccount] BIT);
CREATE TABLE [dbo].[Order] ([Id] INT, [OrderNumber] NVARCHAR(400), [StoreId] INT, [CustomerId] INT, [CustomerCurrencyCode] NVARCHAR(5), [OrderSubtotalInclTax] DECIMAL(18,4), [OrderDiscount] DECIMAL(18,4), [OrderShippingInclTax] DECIMAL(18,4), [OrderTax] DECIMAL(18,4), [OrderTotal] DECIMAL(18,4), [OrderStatusId] INT, [PaymentStatusId] INT, [ShippingStatusId] INT);
CREATE TABLE [dbo].[OrderItem] ([Id] INT, [OrderId] INT, [ProductId] INT, [Quantity] INT, [UnitPriceInclTax] DECIMAL(18,4), [PriceInclTax] DECIMAL(18,4), [ProductName] NVARCHAR(400), [Sku] NVARCHAR(400));
CREATE TABLE [dbo].[UrlRecord] ([Id] INT, [EntityName] NVARCHAR(200), [EntityId] INT, [Slug] NVARCHAR(400), [LanguageId] INT, [StoreId] INT, [IsActive] BIT);

INSERT INTO [dbo].[Language] ([Id], [Name], [UniqueSeoCode], [LanguageCulture], [Rtl], [DisplayOrder], [Published]) VALUES (1, N'English', N'en', N'en-US', 0, 0, 1);
INSERT INTO [dbo].[Currency] ([Id], [Name], [CurrencyCode], [Rate], [DisplayOrder], [Published]) VALUES (1, N'US Dollar', N'USD', 1.00000000, 0, 1);
INSERT INTO [dbo].[Store] ([Id], [Name], [Url], [DisplayOrder], [PrimaryStoreCurrencyId]) VALUES (1, N'Broken Ref Store', N'https://broken.local/', 0, 1);
INSERT INTO [dbo].[Product] ([Id], [Name], [Sku], [ShortDescription], [FullDescription], [Published], [Deleted], [DisplayOrder], [ProductTypeId], [Price], [Weight]) VALUES (1, N'Orphan Product', N'ORPH-1', NULL, NULL, 1, 0, 0, 5, 9.9900, 0.0000);
INSERT INTO [dbo].[Customer] ([Id], [Email], [FirstName], [LastName], [Active], [Deleted], [IsSystemAccount]) VALUES (1, N'valid@example.com', N'Valid', N'User', 1, 0, 0);
INSERT INTO [dbo].[Order] ([Id], [OrderNumber], [StoreId], [CustomerId], [CustomerCurrencyCode], [OrderSubtotalInclTax], [OrderDiscount], [OrderShippingInclTax], [OrderTax], [OrderTotal], [OrderStatusId], [PaymentStatusId], [ShippingStatusId]) VALUES (1, N'BROKEN-1', 1, 999, N'USD', 9.9900, 0.0000, 0.0000, 0.0000, 9.9900, 10, 10, 0);
INSERT INTO [dbo].[OrderItem] ([Id], [OrderId], [ProductId], [Quantity], [UnitPriceInclTax], [PriceInclTax], [ProductName], [Sku]) VALUES (1, 1, 999, 1, 9.9900, 9.9900, N'Missing Product', N'MISSING');
INSERT INTO [dbo].[UrlRecord] ([Id], [EntityName], [EntityId], [Slug], [LanguageId], [StoreId], [IsActive]) VALUES (1, N'Product', 999, N'missing-product', 1, 1, 1);
