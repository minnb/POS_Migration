using System.Data;
using Dapper;
using POS.Common.Dtos.Vouchers;
using POS.Infrastructure.Database;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Repositories;

/// <summary>
/// SAP Internal Voucher — bảng CpnVchBOMCodeIssue (Source='SAP'), dùng chung với Setup Coupon.
/// Port từ SAPVoucherRepository (raw SQL trên Internal_Voucher, nay legacy) sang SP usp_Voucher_*.
/// </summary>
public sealed class VoucherCodeRepository(CentralMDConnectionFactory connectionFactory)
    : BaseRepository(connectionFactory), IVoucherCodeRepository
{
    public async Task<VoucherStatusResponse?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<VoucherStatusResponse>(new CommandDefinition(
            "dbo.usp_Voucher_GetByCode", new { Code = code },
            commandType: CommandType.StoredProcedure, commandTimeout: 30, cancellationToken: ct));
    }

    public async Task<VoucherStatusResponse> CreateOrGetAsync(VoucherStatusResponse data, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@Code", data.VoucherNumber);
        p.Add("@ActicleNo", data.ActicleNo);
        p.Add("@ActicleType", data.ActicleType);
        p.Add("@Value", data.Value);
        p.Add("@Voucher_Currency", data.Voucher_Currency);
        p.Add("@Validity_From_Date", data.Validity_From_Date);
        p.Add("@Expiry_Date", data.Expiry_Date);
        p.Add("@CompanyCode", data.CompanyCode);
        p.Add("@Partner", data.Partner);
        p.Add("@PhoneNumber", data.PhoneNumber);
        p.Add("@VoucherType", data.VoucherType);

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        return await conn.QueryFirstAsync<VoucherStatusResponse>(new CommandDefinition(
            "dbo.usp_Voucher_Create", p,
            commandType: CommandType.StoredProcedure, commandTimeout: 30, cancellationToken: ct));
    }

    public async Task<(bool Success, string Message, List<VoucherStatusResponse> Results)> RedeemAsync(
        List<(string VoucherNumber, double AmountRedeem)> serials,
        string orderNo,
        string? requiredVoucherType = null,
        CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@Lines", BuildRedeemTable(serials).AsTableValuedParameter("dbo.VoucherRedeemTVP"));
        p.Add("@OrderNo", orderNo);
        p.Add("@RequiredVoucherType", requiredVoucherType);

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        using var multi = await conn.QueryMultipleAsync(new CommandDefinition(
            "dbo.usp_Voucher_Redeem", p,
            commandType: CommandType.StoredProcedure, commandTimeout: 30, cancellationToken: ct));

        var header = await multi.ReadFirstAsync<RedeemHeader>();
        var results = (await multi.ReadAsync<VoucherStatusResponse>()).ToList();
        return (header.Success, header.Message, results);
    }

    private static DataTable BuildRedeemTable(IEnumerable<(string VoucherNumber, double AmountRedeem)> serials)
    {
        var t = new DataTable();
        t.Columns.Add("Code", typeof(string));
        t.Columns.Add("AmountRedeem", typeof(decimal));
        foreach (var s in serials)
            t.Rows.Add(s.VoucherNumber, (decimal)s.AmountRedeem);
        return t;
    }

    private sealed class RedeemHeader
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
