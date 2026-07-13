using DbUp;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Helpers;
using Microsoft.Extensions.Configuration;
using POS.DbMigrator;
using POS.Infrastructure.Security;

EnvFileLoader.LoadIfPresent(Path.Combine(AppContext.BaseDirectory, ".env"));

var argSet = args.ToHashSet(StringComparer.OrdinalIgnoreCase);
var configPath = GetOptionValue(args, "--config");
var sqlDirOverride = GetOptionValue(args, "--sql-dir");
var dryRun = argSet.Contains("--dry-run");

string SqlDir() => ManifestScriptProvider.ResolveSqlDirectory(AppContext.BaseDirectory, sqlDirOverride);

if (argSet.Contains("--normalize-alter-proc"))
{
    return RunNormalizeAlterProc(SqlDir(), dryRun);
}

if (argSet.Contains("--whatif"))
{
    return RunWhatIf(SqlDir());
}

if (argSet.Contains("--verify"))
{
    return await RunVerifyAsync(SqlDir(), ResolveConfigPath(configPath));
}

if (argSet.Contains("--apply"))
{
    return await RunApplyAsync(SqlDir(), ResolveConfigPath(configPath));
}

Console.WriteLine("""
    POS.DbMigrator — su dung:
      --whatif [--sql-dir <path>]                       Xem truoc Track A se chay + Track B phai chay tay (khong can DB)
      --verify [--config <path>] [--sql-dir <path>]     Doc SchemaVersions tren DB dich, bao Track B con thieu
      --apply [--config <path>] [--sql-dir <path>]      Chay Track A (idempotent, luon chay lai toan bo)
      --normalize-alter-proc [--dry-run] [--sql-dir]    Chuan hoa ALTER PROC bare -> CREATE OR ALTER PROCEDURE

    --sql-dir <path>   Thu muc chua manifest.json + *.sql (BAT BUOC khi chay binary da publish ngoai
                        git checkout — Docker/Ubuntu bare-metal). Khong truyen -> tu do POS.slnx tu
                        AppContext.BaseDirectory (chi dung duoc luc dev, chay bang `dotnet run` trong repo).
    --config <path>    File appsettings kieu POS.Api (chi can muc ConnectionStrings). Khong truyen ->
                        tu suy ra src/POS.Api/appsettings.{Environment}.json (Environment lay tu
                        ASPNETCORE_ENVIRONMENT -> DOTNET_ENVIRONMENT -> mac dinh "Production"), CHI hoat
                        dong khi chay trong git checkout (dung POS.slnx nhu --sql-dir). Ngoai git checkout
                        BAT BUOC truyen --config tuong minh.
                        POS_SECRET_KEY (khi appsettings co token enc:...) tu doc tu file .env dat canh
                        binary (AppContext.BaseDirectory) neu bien do chua co san trong process.
    """);
return 1;

static string? GetOptionValue(string[] args, string name)
{
    var idx = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
    return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
}

static int RunNormalizeAlterProc(string sqlDir, bool dryRun)
{
    var preview = NormalizeAlterProc.Preview(sqlDir);

    foreach (var r in preview)
    {
        if (r.ReplacementCount == 0)
        {
            Console.WriteLine($"[SKIP]  {r.File} — không tìm thấy 'ALTER PROC' bare nào (đã chuẩn hóa?).");
            continue;
        }
        Console.WriteLine($"[{(dryRun ? "DRY-RUN" : "APPLY")}] {r.File} — {r.ReplacementCount} chỗ 'ALTER PROC...' → 'CREATE OR ALTER PROCEDURE'");
    }

    if (dryRun)
    {
        Console.WriteLine();
        Console.WriteLine("Dry-run — chưa ghi file nào. Bỏ --dry-run để áp dụng thật.");
        return 0;
    }

    NormalizeAlterProc.Apply(sqlDir, preview);
    Console.WriteLine();
    Console.WriteLine("Đã ghi đè các file có thay đổi.");
    return 0;
}

static int RunWhatIf(string sqlDir)
{
    var manifest = ManifestScriptProvider.Load(sqlDir);

    foreach (var target in ManifestScriptProvider.ValidTargets)
    {
        var trackA = ManifestScriptProvider.TrackAFor(manifest, target);
        var trackB = ManifestScriptProvider.TrackBFor(manifest, target);
        if (trackA.Count == 0 && trackB.Count == 0) continue;

        Console.WriteLine($"=== Target: {target} ===");
        Console.WriteLine($"-- Track A (tu dong chay lai TOAN BO moi lan --apply, {trackA.Count} file) --");
        foreach (var e in trackA)
            Console.WriteLine($"  [{e.Order,5}] {e.File}{(e.Note is null ? "" : $"  // {e.Note}")}");

        Console.WriteLine($"-- Track B (PHAI chay tay, {trackB.Count} file) --");
        foreach (var e in trackB)
            Console.WriteLine($"  [{e.Order,5}] {e.File}  (phase={e.Phase}){(e.Note is null ? "" : $"  // {e.Note}")}");
        Console.WriteLine();
    }

    Console.WriteLine("-- Content-guard (quét Track A tìm DDL/DML nguy hiểm chưa guard) --");
    var anyDanger = false;
    foreach (var e in manifest.Scripts.Where(s => !s.RunOnce))
    {
        var content = ManifestScriptProvider.ReadScriptContent(sqlDir, e);
        var findings = DangerousStatementGuard.Scan(e.File, content);
        foreach (var f in findings)
        {
            anyDanger = true;
            Console.WriteLine($"  [NGUY HIEM] {f.ScriptFile}: {f.Description} — \"{f.BatchPreview}\"");
        }
    }
    if (!anyDanger) Console.WriteLine("  Không phát hiện gì — mọi Track A đều an toàn.");

    return anyDanger ? 1 : 0;
}

static async Task<int> RunVerifyAsync(string sqlDir, string configPath)
{
    var configuration = LoadConfiguration(configPath);
    var manifest = ManifestScriptProvider.Load(sqlDir);
    var exitCode = 0;

    foreach (var target in ManifestScriptProvider.ValidTargets)
    {
        var trackB = ManifestScriptProvider.TrackBFor(manifest, target);
        if (trackB.Count == 0) continue;

        if (target == ManifestScriptProvider.TargetCentralMD)
        {
            var cs = RequireConnectionString(configuration, "CentralMD");
            var missing = await TrackBJournalVerifier.FindMissingAsync(cs, trackB, CancellationToken.None);
            exitCode |= ReportMissing(target, missing);
        }
        else if (target == ManifestScriptProvider.TargetCentralSaleShards)
        {
            var centralGeneral = RequireConnectionString(configuration, "CentralGeneral");
            var template = RequireConnectionString(configuration, "CentralSaleTemplate");
            var shards = await ShardResolver.GetDistinctServerIPsAsync(centralGeneral, CancellationToken.None);
            foreach (var ip in shards)
            {
                var cs = ShardResolver.BuildShardConnectionString(template, ip);
                var missing = await TrackBJournalVerifier.FindMissingAsync(cs, trackB, CancellationToken.None);
                exitCode |= ReportMissing($"{target} (shard {ip})", missing);
            }
        }
    }
    return exitCode;
}

static int ReportMissing(string label, List<TrackBJournalVerifier.MissingEntry> missing)
{
    if (missing.Count == 0)
    {
        Console.WriteLine($"[{label}] OK — mọi script Track B đã có mặt trong SchemaVersions.");
        return 0;
    }

    Console.WriteLine($"[{label}] CANH BAO — can chay tay cac file sau va update vao SchemaVersions:");
    foreach (var m in missing)
        Console.WriteLine($"  - {m.Entry.File} (phase={m.Entry.Phase}){(m.Entry.Note is null ? "" : $" — {m.Entry.Note}")}");
    return 1;
}

static async Task<int> RunApplyAsync(string sqlDir, string configPath)
{
    var configuration = LoadConfiguration(configPath);
    var manifest = ManifestScriptProvider.Load(sqlDir);
    var overallFailed = false;

    // Content-guard bắt buộc trước mọi apply — xem Program.RunWhatIf để cùng logic.
    foreach (var e in manifest.Scripts.Where(s => !s.RunOnce))
    {
        var content = ManifestScriptProvider.ReadScriptContent(sqlDir, e);
        var findings = DangerousStatementGuard.Scan(e.File, content);
        if (findings.Count > 0)
        {
            Console.Error.WriteLine($"[CONTENT-GUARD] Phát hiện DDL/DML nguy hiểm trong Track A — DỪNG, không chạy gì:");
            foreach (var f in findings)
                Console.Error.WriteLine($"  {f.ScriptFile}: {f.Description} — \"{f.BatchPreview}\"");
            return 1;
        }
    }

    var centralMdEntries = ManifestScriptProvider.TrackAFor(manifest, ManifestScriptProvider.TargetCentralMD);
    if (centralMdEntries.Count > 0)
    {
        var cs = RequireConnectionString(configuration, "CentralMD");
        overallFailed |= !ApplyTrackA(ManifestScriptProvider.TargetCentralMD, cs, sqlDir, centralMdEntries);
    }

    var shardEntries = ManifestScriptProvider.TrackAFor(manifest, ManifestScriptProvider.TargetCentralSaleShards);
    if (shardEntries.Count > 0)
    {
        var centralGeneral = RequireConnectionString(configuration, "CentralGeneral");
        var template = RequireConnectionString(configuration, "CentralSaleTemplate");
        var shards = await ShardResolver.GetDistinctServerIPsAsync(centralGeneral, CancellationToken.None);

        var failures = new List<(string ServerIP, string Error)>();
        foreach (var ip in shards)
        {
            var cs = ShardResolver.BuildShardConnectionString(template, ip);
            try
            {
                if (!ApplyTrackA($"{ManifestScriptProvider.TargetCentralSaleShards} (shard {ip})", cs, sqlDir, shardEntries))
                    failures.Add((ip, "PerformUpgrade().Successful == false — xem log phía trên"));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Shard {ip}] FAILED: {ex.Message}");
                failures.Add((ip, ex.Message));
            }
        }

        Console.WriteLine($"-- Tổng kết shard: {shards.Count - failures.Count}/{shards.Count} thành công --");
        foreach (var (ip, error) in failures)
            Console.WriteLine($"  [LOI] Shard {ip}: {error}");

        overallFailed |= failures.Count > 0;
    }

    return overallFailed ? 1 : 0;
}

static bool ApplyTrackA(string label, string connectionString, string sqlDir, List<ManifestEntry> entries)
{
    // DbUp tu sap xep lai script theo SqlScript.Name (alphabet) truoc khi thuc thi, bat ke thu tu
    // duoc dua vao qua .WithScripts() - "entries" da OrderBy(Order) o ManifestScriptProvider.TrackAFor
    // se bi vo hieu neu Name chi la ten file. Prefix Name bang Order (zero-pad) de alphabet-sort
    // cua DbUp trung khop voi thu tu Order trong manifest.json (khong doi ten file that tren dia -
    // duong dan doc file van dung e.File nguyen ven).
    var scripts = entries
        .Select(e => new DbUp.Engine.SqlScript($"{e.Order:D6}_{e.File}", File.ReadAllText(Path.Combine(sqlDir, e.File))))
        .ToArray();

    Console.WriteLine($"=== Apply Track A — {label} ({scripts.Length} script) ===");

    var engine = DeployChanges.To
        .SqlDatabase(connectionString)
        .WithScripts(scripts)
        .JournalTo(new NullJournal())
        .WithTransactionPerScript()
        .LogTo(new ConsoleUpgradeLog())
        .Build();

    var result = engine.PerformUpgrade();
    if (!result.Successful)
    {
        Console.Error.WriteLine($"[{label}] THAT BAI o script: {result.ErrorScript?.Name} — {result.Error?.Message}");
        return false;
    }

    Console.WriteLine($"[{label}] OK — {scripts.Length} script da chay lai.");
    return true;
}

static string ResolveConfigPath(string? configOverride)
{
    if (!string.IsNullOrWhiteSpace(configOverride))
    {
        if (!File.Exists(configOverride))
            throw new FileNotFoundException($"--config chỉ tới file không tồn tại: '{configOverride}'.");
        return configOverride;
    }

    var repoRoot = RepoRootLocator.FindRepoRoot(AppContext.BaseDirectory);
    if (repoRoot is null)
        throw new InvalidOperationException(
            "Không tìm thấy POS.slnx để tự suy ra appsettings.json của POS.Api, và không có --config " +
            "<path>. Khi chạy binary đã publish/deploy ngoài git checkout (Docker, Ubuntu bare-metal) " +
            "BẮT BUỘC truyền --config trỏ tới file appsettings.{Environment}.json.");

    var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
        ?? "Production";
    var candidate = Path.Combine(repoRoot, "src", "POS.Api", $"appsettings.{env}.json");
    if (!File.Exists(candidate))
        throw new FileNotFoundException(
            $"Không tìm thấy '{candidate}' để tự dùng làm --config mặc định (env='{env}'). Truyền " +
            "--config <path> trỏ đúng file appsettings cần dùng.", candidate);

    Console.WriteLine($"[--config] Không truyền --config — tự dùng '{candidate}' (env='{env}').");
    return candidate;
}

static string RequireConnectionString(IConfiguration configuration, string key) =>
    configuration.GetConnectionString(key)
        ?? throw new InvalidOperationException($"Thiếu ConnectionStrings:{key} trong file config.");

static ConfigurationManager LoadConfiguration(string configPath)
{
    var configuration = new ConfigurationManager();
    configuration.AddJsonFile(configPath, optional: false, reloadOnChange: false);
    configuration.DecryptEncryptedSecrets();
    return configuration;
}

sealed class ConsoleUpgradeLog : IUpgradeLog
{
    public void WriteInformation(string format, params object[] args) => Console.WriteLine(string.Format(format, args));
    public void WriteError(string format, params object[] args) => Console.Error.WriteLine(string.Format(format, args));
    public void WriteWarning(string format, params object[] args) => Console.WriteLine("[WARN] " + string.Format(format, args));
}
