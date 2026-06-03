using POS.Application.Gift.DTOs;

namespace POS.Application.Gift.Services;

public interface IWinXService
{
    Task<(WinXQrCodeResult? Result, string Message)> PosPostTransactionsAsync(MMLSchemeRequest request);
}
