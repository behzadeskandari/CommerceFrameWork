CREATE TABLE [dbo].[Language] ([Id] INT, [Name] NVARCHAR(200), [UniqueSeoCode] NVARCHAR(10), [LanguageCulture] NVARCHAR(20), [Rtl] BIT, [DisplayOrder] INT, [Published] BIT);
CREATE TABLE [dbo].[Currency] ([Id] INT, [Name] NVARCHAR(200), [CurrencyCode] NVARCHAR(5), [Rate] DECIMAL(18,8), [DisplayOrder] INT, [Published] BIT);
CREATE TABLE [dbo].[Store] ([Id] INT, [Name] NVARCHAR(400), [Url] NVARCHAR(400), [DisplayOrder] INT, [PrimaryStoreCurrencyId] INT);
CREATE TABLE [dbo].[Product] ([Id] INT, [Name] NVARCHAR(400), [Sku] NVARCHAR(400), [ShortDescription] NVARCHAR(1000), [FullDescription] NVARCHAR(MAX), [Published] BIT, [Deleted] BIT, [DisplayOrder] INT, [ProductTypeId] INT, [Price] DECIMAL(18,4), [Weight] DECIMAL(18,4));
CREATE TABLE [dbo].[MediaFile] ([Id] INT, [Name] NVARCHAR(400), [MimeType] NVARCHAR(200), [Extension] NVARCHAR(50), [Size] INT, [Path] NVARCHAR(500), [Width] INT);
CREATE TABLE [dbo].[Product_MediaFile_Mapping] ([ProductId] INT, [MediaFileId] INT, [DisplayOrder] INT);

INSERT INTO [dbo].[Language] ([Id], [Name], [UniqueSeoCode], [LanguageCulture], [Rtl], [DisplayOrder], [Published]) VALUES (1, N'English', N'en', N'en-US', 0, 0, 1);
INSERT INTO [dbo].[Currency] ([Id], [Name], [CurrencyCode], [Rate], [DisplayOrder], [Published]) VALUES (1, N'US Dollar', N'USD', 1.00000000, 0, 1);
INSERT INTO [dbo].[Store] ([Id], [Name], [Url], [DisplayOrder], [PrimaryStoreCurrencyId]) VALUES (1, N'Media Store', N'https://media.local/', 0, 1);
INSERT INTO [dbo].[Product] ([Id], [Name], [Sku], [ShortDescription], [FullDescription], [Published], [Deleted], [DisplayOrder], [ProductTypeId], [Price], [Weight]) VALUES (1, N'No Image Product', N'NOIMG-1', NULL, NULL, 1, 0, 0, 5, 1.0000, 0.0000);
INSERT INTO [dbo].[MediaFile] ([Id], [Name], [MimeType], [Extension], [Size], [Path], [Width]) VALUES (1, N'missing-path.jpg', N'image/jpeg', N'jpg', 0, NULL, NULL);
INSERT INTO [dbo].[Product_MediaFile_Mapping] ([ProductId], [MediaFileId], [DisplayOrder]) VALUES (1, 99, 0);
