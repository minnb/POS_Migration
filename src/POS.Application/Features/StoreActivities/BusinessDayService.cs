using POS.Common.Dtos.CentralSale;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Application.Features.StoreActivities;

public sealed class BusinessDayService(
    ICentralSaleRepository centralSaleRepository,
    ICentralMDRepository centralMDRepository) : IBusinessDayService
{
    public async Task<List<PosDayStagingDto>> GetPosDayStagingAsync(
        string storeNo, DateTime businessDate, CancellationToken ct = default)
    {
        var staging = await centralSaleRepository.GetPosDayStagingAsync(storeNo, businessDate, ct);
        var terminals = (await centralMDRepository.GetPosTerminalListAsync(ct))
            .Where(t => t.StoreNo == storeNo && t.Status == true)
            .ToList();

        // Terminal master (CentralMD) là gốc — đảm bảo hiện đủ máy POS đang hoạt động của store,
        // kể cả máy chưa có giao dịch/chưa đóng ngày. Bổ sung Placement từ master vào staging.
        var byTerminal = staging.ToDictionary(s => s.PosTerminal);
        var result = new List<PosDayStagingDto>();

        foreach (var terminal in terminals.OrderBy(t => t.No))
        {
            if (byTerminal.TryGetValue(terminal.No, out var row))
            {
                row.Placement = terminal.Placement;
                result.Add(row);
            }
            else
            {
                result.Add(new PosDayStagingDto { PosTerminal = terminal.No, Placement = terminal.Placement });
            }
        }

        return result;
    }

    public Task<BusinessDayConfirmDto?> GetConfirmStatusAsync(
        string storeNo, DateTime businessDate, CancellationToken ct = default)
        => centralSaleRepository.GetBusinessDayConfirmAsync(storeNo, businessDate, ct);

    public async Task<DateTime?> GetCurrentBusinessDateAsync(string storeNo, CancellationToken ct = default)
        => (await centralSaleRepository.GetBusinessDateAsync(storeNo, ct))?.BussinessDate;

    public async Task<ConfirmBusinessDayResult> ConfirmBusinessDayAsync(
        string storeNo, DateTime businessDate, string confirmedBy,
        bool allowForceConfirm = false, CancellationToken ct = default)
    {
        var staging = await GetPosDayStagingAsync(storeNo, businessDate, ct);
        if (staging.Count == 0)
        {
            return new ConfirmBusinessDayResult
            {
                Success = false,
                Message = "Cửa hàng chưa có máy POS nào đang hoạt động."
            };
        }

        var notClosed = staging.Where(s => !s.IsClosed).Select(s => s.PosTerminal).ToList();
        // ITOps/SystemAdmin (allowForceConfirm) được force EOD kể cả khi còn POS chưa đóng ngày.
        if (!allowForceConfirm && notClosed.Count > 0)
        {
            return new ConfirmBusinessDayResult
            {
                Success = false,
                Message = $"Còn {notClosed.Count} máy POS chưa đóng ngày: {string.Join(", ", notClosed)}"
            };
        }

        var existing = await centralSaleRepository.GetBusinessDayConfirmAsync(storeNo, businessDate, ct);
        if (existing != null)
        {
            return new ConfirmBusinessDayResult
            {
                Success = false,
                Message = $"Ngày kinh doanh này đã được xác nhận bởi {existing.ConfirmedBy} lúc {existing.ConfirmedDate:dd/MM/yyyy HH:mm}."
            };
        }

        var request = new ConfirmBusinessDayRequest
        {
            StoreNo = storeNo,
            BusinessDate = businessDate.Date,
            TotalRevenue = staging.Sum(s => s.TotalRevenue),
            TotalShifts = staging.Count,
            ConfirmedBy = confirmedBy
        };

        return await centralSaleRepository.ConfirmBusinessDayAsync(request, ct);
    }
}
