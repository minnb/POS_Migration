	ALTER TABLE [OfferHeader] ADD [MinValue] [float] NULL;
	ALTER TABLE [OfferHeader] ADD [TotalDiscountType] [int] NULL;
	ALTER TABLE [OfferHeader] ADD [TotalDiscountValue] [float] NULL;
	ALTER TABLE [OfferHeader] ADD [IsVoucher] [bit] NULL;
	ALTER TABLE [OfferHeader] ADD [IsTotalBill] [bit] NULL;
	ALTER TABLE [OfferHeader] ADD [IsGift] [bit] NULL;
	ALTER TABLE [OfferHeader] ADD [MemberCode] [varchar](50) NULL;
	ALTER TABLE [OfferHeader] ADD [DiscountAmountMax] [float] NULL;
	ALTER TABLE [OfferHeader] ADD [IsFullPrice] [bit] NULL;
	ALTER TABLE [OfferHeader] ADD [Remark] [nvarchar](500) NULL;
	ALTER TABLE [OfferHeader] ADD [SalesType] [varchar](50) NULL

    