using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Repository truy vấn POS Terminal. Application định nghĩa interface, Infrastructure triển khai (Dapper).
/// Thay thế truy cập EF6 (CentralMDContainer.POSTerminals) trong CommonData cũ.
/// </summary>
public interface IPosTerminalRepository
{
    Task<PosTerminalDto?> GetByIpAddressAsync(string ipAddress);
}
