using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using POS.Application;
using POS.Application.Features.SpAudit;
using POS.Common.Dtos.Ops.SpAudit;
using POS.Common.Enums;
using POS.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

using var host = builder.Build();
var auditService = host.Services.GetRequiredService<ISpAuditService>();

try
{
    var snapshot = await auditService.RunAuditAsync();
    PrintReport(snapshot);

    if (!string.IsNullOrEmpty(snapshot.ErrorMessage))
        Console.Error.WriteLine($"[SqlAuditCli] Cảnh báo (1 phần database lỗi): {snapshot.ErrorMessage}");

    // v1: không so sánh lịch sử để tránh over-engineer trend tracking — chỉ báo "có phát hiện
    // đáng chú ý" ngay trong lần chạy này. Sẵn sàng cắm vào CI sau này (exit code != 0 = cần xem lại).
    var hasFindings = snapshot.Items.Exists(i =>
        i.Recommendation is SpRecommendation.MigrationCandidate or SpRecommendation.CleanupCandidate);

    return hasFindings ? 1 : 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[SqlAuditCli] Audit thất bại: {ex.Message}");
    return 2;
}

static void PrintReport(SpAuditSnapshotDto snapshot)
{
    Console.WriteLine($"=== SQL Audit — {snapshot.RunFinishedUtc:u} ===");
    Console.WriteLine($"Databases: {string.Join(", ", snapshot.DatabasesScanned)}");
    Console.WriteLine(
        $"Tổng procedure: {snapshot.TotalProcedures}" +
        (snapshot.ProceduresTruncated ? " (đã cắt bớt theo MaxProceduresPerDatabase)" : ""));
    Console.WriteLine();
    Console.WriteLine($"{"Procedure",-50} {"Complexity",-10} {"Recommendation",-20} {"LastExecutionAt",-20}");
    Console.WriteLine(new string('-', 105));

    foreach (var item in snapshot.Items)
    {
        var name = $"{item.DatabaseKey}.{item.Schema}.{item.ProcedureName}";
        var lastExec = item.LastExecutionAt?.ToString("u") ?? "-";
        Console.WriteLine($"{name,-50} {item.Complexity,-10} {item.Recommendation,-20} {lastExec,-20}");
    }
}
