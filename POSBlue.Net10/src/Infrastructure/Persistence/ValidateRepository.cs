using Dapper;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Infrastructure.Persistence;

/// <summary>
/// Dapper implementation của IValidateRepository.
/// Kết nối per-store qua ISqlConnectionFactory.CreateConnectionFromRaw(connStr).
/// </summary>
public sealed class ValidateRepository : IValidateRepository
{
    private readonly ISqlConnectionFactory _db;

    public ValidateRepository(ISqlConnectionFactory db) => _db = db;

    public async Task<(ValidateTransactionDto? Header, List<ValidateTransactionLine> Lines,
                       CashOrderSentToEInvoice? CashSent, InvoiceCreatedItem? ExistingTemp)>
        GetTransactionDataAsync(string connStr, string orderNo)
    {
        using var conn = _db.CreateConnectionFromRaw(connStr);
        conn.Open();

        var existingTemp = (await conn.QueryAsync<InvoiceCreatedItem>(
            "SELECT OrderNo, TaxCode, CustomerName FROM EInvoice.[dbo].[InvoiceCreated_Temp] (NOLOCK) WHERE OrderNo = @orderNo;",
            new { orderNo })).FirstOrDefault();

        var header = (await conn.QueryAsync<ValidateTransactionDto>(
            @"SELECT OrderNo, OrderDate, OrderTime, TransactionType, DeliveringMethod, SalesIsReturn,
                     ReturnedOrderNo, CreatedDate, RefKey1, Ref4 AS CQT, CustomerName, MemberCardNo,
                     ISNULL(MemberPointsEarn,0) MemberPointsEarn
              FROM TransHeader (NOLOCK) WHERE OrderNo = @orderNo;", new { orderNo })).FirstOrDefault();

        if (header == null) return (null, [], null, existingTemp);

        var lines = (await conn.QueryAsync<ValidateTransactionLine>(
            @"SELECT DISTINCT [LineNo], ItemNo, [Description] ItemName, UnitOfMeasure,
                     Quantity, UnitPrice, DiscountAmount, VATPercent, VATAmount,
                     LineAmountIncVAT, DivisionCode, Barcode
              FROM TransLine (NOLOCK) WHERE [LineType] = 0 AND DocumentNo = @orderNo;",
            new { orderNo })).ToList();

        var cashSent = (await conn.QueryAsync<CashOrderSentToEInvoice>(
            "SELECT * FROM EInvoice.[dbo].Cash_OrderNoSentToEInvoice (NOLOCK) WHERE OrderNo = @orderNo;",
            new { orderNo })).FirstOrDefault();

        return (header, lines, cashSent, existingTemp);
    }

    public async Task<(bool Success, string Message, string? Technical)> InsertInvoiceCreatedAsync(
        List<InvoiceCreatedItem> items, string connStr)
    {
        using var conn = _db.CreateConnectionFromRaw(connStr);
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            const string sql = @"INSERT INTO EInvoice.[dbo].[InvoiceCreated_Temp]
                ([SiteNo],[OrderNo],[CustomerName],[CompanyName],[TaxCode],[PhoneNumber],[Email],
                 [Address],[IsNotVAT],[IsNotVATPartner],[CreatedUser],[CreatedDate],[Id],Passport,CCCD,DVQHNS)
                VALUES (@SiteNo,@OrderNo,@CustomerName,@CompanyName,@TaxCode,@PhoneNumber,@Email,
                        @Address,@IsNotVAT,@IsNotVATPartner,'WEB',getdate(),@Id,@Passport,@CCCD,@DVQHNS)";
            await conn.ExecuteAsync(sql, items, tx, commandTimeout: 3600);
            tx.Commit();
            return (true, "OK", "OK");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, ex.Message, ex.ToString());
        }
    }

    public async Task<MemberBusinessDbDto?> GetMemberBusinessAsync(string memberCard, string connStr)
    {
        using var conn = _db.CreateConnectionFromRaw(connStr);
        conn.Open();
        return (await conn.QueryAsync<MemberBusinessDbDto>(
            @"SELECT MemberCard, MemberStore, StoreNo, CardLevel, Blocked
              FROM MemberBusiness (NOLOCK) WHERE Blocked = 0 AND MemberCard = @memberCard AND CardLevel = 'HVDN';",
            new { memberCard })).FirstOrDefault();
    }

    public async Task<List<MemberBusinessItemDb>> GetMemberBusinessItemsAsync(
        string memberCard, string month, string connStr)
    {
        using var conn = _db.CreateConnectionFromRaw(connStr);
        conn.Open();
        const string sql = @"
            SELECT MemberCard = @memberCard, A.[Month], A.CardLevel, A.ItemNo, A.Uom, A.MaxValue,
                   IIF(ISNULL(B.RemnValue, 0) > 0, (A.MaxValue - ISNULL(B.RemnValue, 0)), 0) UsedValue,
                   IIF(ISNULL(B.RemnValue, 0) = 0, A.MaxValue, ISNULL(B.RemnValue, 0)) RemnValue
            FROM MemberItemDiscount (NOLOCK) A
            LEFT JOIN (
                SELECT T0.* FROM MemberRemnItem (NOLOCK) T0
                INNER JOIN (
                    SELECT T2.MemberCard, T2.[Month], T2.CardLevel, T2.ItemNo, T2.Uom, MAX(T2.Id) Id
                    FROM MemberRemnItem (NOLOCK) T2 GROUP BY T2.MemberCard, T2.[Month], T2.CardLevel, T2.ItemNo, T2.Uom
                ) T1 ON T0.Id = T1.Id AND T0.MemberCard = @memberCard
            ) B ON A.ItemNo = B.ItemNo AND A.Uom = B.Uom AND A.CardLevel = B.CardLevel
                AND B.MemberCard = @memberCard AND B.[Month] = @month
            WHERE A.MaxValue > 0 AND A.Blocked = 0 AND A.[Month] = @month;";
        return (await conn.QueryAsync<MemberBusinessItemDb>(sql, new { memberCard, month })).ToList();
    }
}
