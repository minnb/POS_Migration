using POS.Infrastructure.Logging;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Web.Auth;

public interface IAuditLogger
{
    Task LogAsync(string actor, string action, string entityType,
                  string entityKey, string? oldValueJson = null, string? newValueJson = null);
}

public sealed class DbAuditLogger(
    ICentralMDRepository centralMDRepo,
    IFileLogHelper fileLog) : IAuditLogger
{
    public async Task LogAsync(string actor, string action, string entityType,
                               string entityKey, string? oldValueJson = null, string? newValueJson = null)
    {
        fileLog.WriteLogs($"[Audit.{action}] entityKey={entityKey} actor={actor} entity={entityType}");

        await centralMDRepo.InsertDashboardAuditLogAsync(
            actor, action, entityType, entityKey, oldValueJson, newValueJson);
    }
}
