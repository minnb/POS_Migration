using Dapper;
using POS.Common.Dtos.Vouchers;
using POS.Infrastructure.Database;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Repositories;

public sealed class SAPVoucherRepository(CentralMDConnectionFactory connectionFactory)
    : BaseRepository(connectionFactory), ISAPVoucherRepository
{
    public async Task<VoucherStatusResponse?> GetByVoucherNumberAsync(string voucherNumber, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT TOP 1
                [Status],
                [Return]              = CAST([Return] AS VARCHAR),
                [ActicleNo],
                [ActicleType],
                [VoucherNumber],
                [Value]               = CAST([Value] AS VARCHAR),
                [Voucher_Currency],
                [Validity_From_Date]  = CONVERT(VARCHAR, Validity_From_Date, 103),
                [Expiry_Date]         = CONVERT(VARCHAR, Expiry_Date, 103),
                [CompanyCode],
                [Partner],
                [IsEmployee],
                [PhoneNumber],
                [VoucherType]
            FROM [dbo].[Internal_Voucher] (NOLOCK)
            WHERE VoucherNumber = @voucherNumber";

        return await QueryFirstOrDefaultAsync<VoucherStatusResponse>(sql, new { voucherNumber }, ct: ct);
    }

    public async Task<bool> InsertAsync(VoucherStatusResponse data, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO [dbo].[Internal_Voucher]
                ([VoucherNumber], [Status], [Return], [ActicleNo], [ActicleType],
                 [Value], [Voucher_Currency], [Validity_From_Date], [Expiry_Date],
                 [CompanyCode], [Partner], [IsEmployee], [PhoneNumber], [VoucherType])
            VALUES
                (@VoucherNumber, @Status, @Return, @ActicleNo, @ActicleType,
                 @Value, @Voucher_Currency,
                 TRY_CONVERT(DATE, @Validity_From_Date, 103),
                 TRY_CONVERT(DATE, @Expiry_Date, 103),
                 @CompanyCode, @Partner, @IsEmployee, @PhoneNumber, @VoucherType)";

        try
        {
            var rows = await ExecuteAsync(sql, new
            {
                data.VoucherNumber,
                data.Status,
                Return          = data.Return,
                ActicleNo       = data.ActicleNo,
                ActicleType     = data.ActicleType,
                Value           = data.Value,
                Voucher_Currency = data.Voucher_Currency,
                Validity_From_Date = data.Validity_From_Date,
                Expiry_Date     = data.Expiry_Date,
                data.CompanyCode,
                data.Partner,
                data.IsEmployee,
                data.PhoneNumber,
                data.VoucherType
            }, ct: ct);
            return rows > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, string Message, List<VoucherStatusResponse> Results)> RedeemVouchersAsync(
        List<string> voucherNumbers, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();
        try
        {
            const string selectSql = @"
                SELECT
                    [Status],
                    [Return]             = CAST([Return] AS VARCHAR),
                    [ActicleNo],
                    [ActicleType],
                    [VoucherNumber],
                    [Value]              = CAST([Value] AS VARCHAR),
                    [Voucher_Currency],
                    [Validity_From_Date] = CONVERT(VARCHAR, Validity_From_Date, 103),
                    [Expiry_Date]        = CONVERT(VARCHAR, Expiry_Date, 103),
                    [CompanyCode],
                    [Partner],
                    [IsEmployee],
                    [PhoneNumber],
                    [VoucherType]
                FROM [dbo].[Internal_Voucher] WITH (UPDLOCK)
                WHERE VoucherNumber IN @voucherNumbers";

            var vouchers = (await conn.QueryAsync<VoucherStatusResponse>(
                new CommandDefinition(selectSql, new { voucherNumbers }, tx, cancellationToken: ct))).ToList();

            if (vouchers.Count != voucherNumbers.Count)
            {
                tx.Rollback();
                return (false, "Một hoặc nhiều voucher không tồn tại trong hệ thống", []);
            }

            var invalid = vouchers.FirstOrDefault(v => v.Status != "SOLD");
            if (invalid != null)
            {
                tx.Rollback();
                return (false, $"Voucher {invalid.VoucherNumber} không ở trạng thái SOLD (hiện tại: {invalid.Status})", []);
            }

            const string updateSql = @"
                UPDATE [dbo].[Internal_Voucher]
                SET [Status] = 'RDM'
                WHERE VoucherNumber IN @voucherNumbers";

            await conn.ExecuteAsync(
                new CommandDefinition(updateSql, new { voucherNumbers }, tx, cancellationToken: ct));

            tx.Commit();

            foreach (var v in vouchers) v.Status = "RDM";
            return (true, "OK", vouchers);
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
