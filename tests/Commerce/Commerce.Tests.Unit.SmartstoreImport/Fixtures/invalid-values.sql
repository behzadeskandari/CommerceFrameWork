CREATE TABLE [dbo].[Language] ([Id] INT, [Name] NVARCHAR(200), [UniqueSeoCode] NVARCHAR(10), [LanguageCulture] NVARCHAR(20), [Rtl] BIT, [DisplayOrder] INT, [Published] BIT);
CREATE TABLE [dbo].[Currency] ([Id] INT, [Name] NVARCHAR(200), [CurrencyCode] NVARCHAR(5), [Rate] DECIMAL(18,8), [DisplayOrder] INT, [Published] BIT);
CREATE TABLE [dbo].[Store] ([Id] INT, [Name] NVARCHAR(400), [Url] NVARCHAR(400), [DisplayOrder] INT, [PrimaryStoreCurrencyId] INT);
CREATE TABLE [dbo].[Product] ([Id] INT, [Name] NVARCHAR(400), [Sku] NVARCHAR(400), [ShortDescription] NVARCHAR(1000), [FullDescription] NVARCHAR(MAX), [Published] BIT, [Deleted] BIT, [DisplayOrder] INT, [ProductTypeId] INT, [Price] DECIMAL(18,4), [Weight] DECIMAL(18,4));
CREATE TABLE [dbo].[Customer] ([Id] INT, [Email] NVARCHAR(500), [FirstName] NVARCHAR(225), [LastName] NVARCHAR(225), [Active] BIT, [Deleted] BIT, [IsSystemAccount] BIT);
CREATE TABLE [dbo].[ProductReview] ([Id] INT, [ProductId] INT, [CustomerId] INT, [Rating] INT, [Title] NVARCHAR(200), [ReviewText] NVARCHAR(MAX), [IsApproved] BIT, [IsVerifiedPurchase] BIT);

INSERT INTO [dbo].[Language] ([Id], [Name], [UniqueSeoCode], [LanguageCulture], [Rtl], [DisplayOrder], [Published]) VALUES (1, N'English', N'en', N'en-US', 0, 0, 1);
INSERT INTO [dbo].[Currency] ([Id], [Name], [CurrencyCode], [Rate], [DisplayOrder], [Published]) VALUES (1, N'Broken Rate', N'XXX', 0.00000000, 0, 1);
INSERT INTO [dbo].[Store] ([Id], [Name], [Url], [DisplayOrder], [PrimaryStoreCurrencyId]) VALUES (1, N'Invalid Store', N'https://invalid.local/', 0, 1);
INSERT INTO [dbo].[Product] ([Id], [Name], [Sku], [ShortDescription], [FullDescription], [Published], [Deleted], [DisplayOrder], [ProductTypeId], [Price], [Weight]) VALUES (1, N'Bad Price Product', N'BAD-1', NULL, NULL, 1, 0, 0, 5, -5.0000, 0.0000);
INSERT INTO [dbo].[Customer] ([Id], [Email], [FirstName], [LastName], [Active], [Deleted], [IsSystemAccount]) VALUES (1, N'reviewer@example.com', N'Re', N'Viewer', 1, 0, 0);
INSERT INTO [dbo].[ProductReview] ([Id], [ProductId], [CustomerId], [Rating], [Title], [ReviewText], [IsApproved], [IsVerifiedPurchase]) VALUES (1, 1, 1, 9, N'Bad rating', N'Rating out of range', 1, 0);
