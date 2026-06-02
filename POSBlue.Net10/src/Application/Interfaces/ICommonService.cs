using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Use case cho nhóm API Common. Controller chỉ gọi service này, không chứa business logic.
/// </summary>
public interface ICommonService
{
    DateTime GetCurrentTime();
    Task<PosTerminalDto?> CheckIpAddressPosAsync(string ipAddress);
}
