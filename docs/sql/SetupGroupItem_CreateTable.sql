/* ============================================================================
   11.1 Cài đặt CTKM — CREATE TABLE dbo.SetupGroupItem (RPOSMasterData)
   Bảng lưu "nhóm sản phẩm" dùng cho dòng Buy/Get khi Loại = "Nhóm SP" (LineType=1),
   thao tác qua modal ItemGroupSetupDialog.razor (trang PromotionSetupPage).

   BỐI CẢNH: định nghĩa gốc của bảng này nằm trong file dump legacy
   `src/legacy/Database/CentralMD.sql` (chỉ đọc, không tự chạy) — SP
   `docs/sql/SetupGroupItem_Save.sql` trước đây giả định bảng "đã tồn tại sẵn
   trong DB" nên KHÔNG kèm CREATE TABLE. Môi trường nào chưa từng chạy full
   script legacy sẽ thiếu bảng này → lỗi "Invalid object name 'dbo.SetupGroupItem'"
   khi bấm Lưu ở dialog trên.

   Idempotent — chạy lại nhiều lần an toàn (chỉ tạo nếu chưa có).
   CHẠY 1 LẦN trên RPOSMasterData, TRƯỚC `docs/sql/SetupGroupItem_Save.sql`.
   ============================================================================ */
USE [RPOSMasterData];
GO

IF OBJECT_ID(N'dbo.SetupGroupItem', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SetupGroupItem](
        [ID]             [int] IDENTITY(1,1) NOT NULL,
        [GroupCode]      [varchar](50) NULL,
        [GroupName]      [nvarchar](250) NULL,
        [ListItemNo]     [varchar](max) NULL,
        [CreatedUser]    [varchar](50) NULL,
        [CreatedDate]    [datetime] NULL,
        [LastUpdateUser] [varchar](50) NULL,
        [LastUpdateDate] [datetime] NULL,
        [Status]         [bit] NULL,
     CONSTRAINT [PK_BuyGroupItem] PRIMARY KEY CLUSTERED
    (
        [ID] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF,
            ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
END
GO
