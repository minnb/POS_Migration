using Dapper;
using POS.Common.Dtos.Loyalty.WinCode;
using POS.Infrastructure.Database;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Repositories;

public sealed class WincodeRepository(CentralMDConnectionFactory connectionFactory)
    : BaseRepository(connectionFactory), IWincodeRepository
{
    public async Task<List<WinCodeCustomerDto>?> GetWinCodeCustomerAsync(string phoneNumber, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM WinCodeCustomer (NOLOCK) WHERE MemberCard = @phoneNumber;";
        return (await QueryAsync<WinCodeCustomerDto>(sql, new { phoneNumber }, ct: ct)).ToList();
    }

    public async Task<Tuple<bool, string>> UpdateWincodeCustomerAsync(
        WinLife_UpdatePromotions_POS_Request request, CancellationToken ct = default)
    {
        const string queryCheck = @"SELECT * FROM WinCodeCustomer (NOLOCK) WHERE OrderNo = @orderNo AND MemberCard = @phoneNumber;";
        const string queryInsert = @"INSERT INTO [dbo].[WinCodeCustomer]
            ([ID],[WinCode],[MemberCard],[Csn],[QuantityRecieptedSum],[QuantityReciepted],[OrderNo],[StoreNo],[PosNo],[IsDeleted],
             [TransDate],[CreatedDate],[UpdatedDate],[CreatedBy],[UpdatedBy])
            VALUES (@ID,@WinCode,@MemberCard,@Csn,@QuantityRecieptedSum,@QuantityReciepted,@OrderNo,@StoreNo,@PosNo,@IsDeleted,
                    @TransDate,@CreatedDate,@UpdatedDate,@CreatedBy,@UpdatedBy)";

        try
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
            var existing = (await conn.QueryAsync<WinCodeCustomerDto>(
                new CommandDefinition(queryCheck,
                    new { orderNo = request.orderNo, phoneNumber = request.phoneNumber },
                    cancellationToken: ct))).ToList();

            using var tx = conn.BeginTransaction();
            try
            {
                if (existing.Count > 0)
                {
                    var insertList = (request.appliedCodes ?? []).Select(item => new WinCodeCustomerDto
                    {
                        ID                  = Guid.NewGuid(),
                        WinCode             = item.winCode,
                        MemberCard          = request.phoneNumber,
                        Csn                 = request.csn,
                        QuantityReciepted   = item.quantity * -1,
                        QuantityRecieptedSum = item.quantity * -1,
                        OrderNo             = request.orderNo + "9",
                        StoreNo             = request.storeCode,
                        PosNo               = request.posCode,
                        IsDeleted           = true,
                        TransDate           = DateTime.Now.Date,
                        CreatedBy           = request.posCode,
                        UpdatedBy           = request.posCode,
                        CreatedDate         = DateTime.Now,
                        UpdatedDate         = DateTime.Now
                    }).ToList();

                    await conn.ExecuteAsync(new CommandDefinition(queryInsert, insertList, tx, cancellationToken: ct));
                    tx.Commit();
                    return new Tuple<bool, string>(true, "OK");
                }
                else
                {
                    tx.Commit();
                    return new Tuple<bool, string>(false, $"Đơn hàng {request.orderNo} không tồn tại");
                }
            }
            catch (Exception ex)
            {
                tx.Rollback();
                string msg = ex.Message.Contains("UNIQUE KEY") || ex.Message.Contains("Cannot insert duplicate key in object")
                    ? $"Đơn hàng {request.orderNo} đã thực hiện hoàn trả số lượng"
                    : $"Exception: {ex.Message}";
                return new Tuple<bool, string>(false, msg);
            }
        }
        catch (Exception ex)
        {
            return new Tuple<bool, string>(false, ex.Message);
        }
    }

    public async Task<Tuple<bool, string>> InsertWincodeCustomerAsync(
        WinLife_UpdatePromotions_POS_Request request, CancellationToken ct = default)
    {
        const string queryInsert = @"INSERT INTO [dbo].[WinCodeCustomer]
            ([ID],[WinCode],[MemberCard],[Csn],[QuantityRecieptedSum],[QuantityReciepted],[OrderNo],[StoreNo],[PosNo],[IsDeleted],
             [TransDate],[CreatedDate],[UpdatedDate],[CreatedBy],[UpdatedBy])
            VALUES (@ID,@WinCode,@MemberCard,@Csn,@QuantityRecieptedSum,@QuantityReciepted,@OrderNo,@StoreNo,@PosNo,@IsDeleted,
                    @TransDate,@CreatedDate,@UpdatedDate,@CreatedBy,@UpdatedBy)";

        try
        {
            var insertList = (request.appliedCodes ?? []).Select(item => new WinCodeCustomerDto
            {
                ID                   = Guid.NewGuid(),
                WinCode              = item.winCode,
                MemberCard           = request.phoneNumber,
                Csn                  = request.csn,
                QuantityReciepted    = item.quantity,
                QuantityRecieptedSum = item.quantity,
                OrderNo              = request.orderNo,
                StoreNo              = request.storeCode,
                PosNo                = request.posCode,
                IsDeleted            = false,
                TransDate            = DateTime.Now.Date,
                CreatedBy            = request.posCode,
                UpdatedBy            = request.posCode,
                CreatedDate          = DateTime.Now,
                UpdatedDate          = DateTime.Now
            }).ToList();

            using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
            using var tx   = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync(new CommandDefinition(queryInsert, insertList, tx, cancellationToken: ct));
                tx.Commit();
                return new Tuple<bool, string>(true, "OK");
            }
            catch (Exception ex)
            {
                tx.Rollback();
                var firstCode = request.appliedCodes?.FirstOrDefault()?.winCode ?? string.Empty;
                string msg = ex.Message.Contains("UNIQUE KEY") || ex.Message.Contains("Cannot insert duplicate key in object")
                    ? $"Đơn hàng {request.orderNo} đã thực hiện nhận {firstCode}"
                    : $"Exception: {ex.Message}";
                return new Tuple<bool, string>(false, msg);
            }
        }
        catch (Exception ex)
        {
            return new Tuple<bool, string>(false, ex.Message);
        }
    }
}
