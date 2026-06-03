using POS.Application.Shared.DTOs;

namespace POS.Application.Shared.Services;

public interface ISysWebApiConfigService
{
    Task<SysWebApiDto?> GetByAppCodeAsync(string appCode);
}
