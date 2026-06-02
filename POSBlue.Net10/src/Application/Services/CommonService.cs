using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Services;

/// <summary>
/// Triển khai use case Common — orchestration, gọi repository qua interface.
/// Thay thế CommonBLO (Business) + CommonData (Data EF6) của bản cũ.
/// </summary>
public sealed class CommonService : ICommonService
{
    private readonly IPosTerminalRepository _posTerminalRepository;

    public CommonService(IPosTerminalRepository posTerminalRepository)
    {
        _posTerminalRepository = posTerminalRepository;
    }

    public DateTime GetCurrentTime() => DateTime.Now;

    public Task<PosTerminalDto?> CheckIpAddressPosAsync(string ipAddress)
        => _posTerminalRepository.GetByIpAddressAsync(ipAddress);
}
