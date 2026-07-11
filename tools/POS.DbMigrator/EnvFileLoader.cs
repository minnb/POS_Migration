namespace POS.DbMigrator;

/// <summary>
/// Nạp biến môi trường từ file .env (KEY=VALUE mỗi dòng, giống .env.example) đặt cạnh binary — bù
/// cho việc chạy bare-metal không có docker-compose tự đọc .env. CHỈ set khi biến đó CHƯA có sẵn
/// trong process (ưu tiên giá trị export/systemd Environment=/docker -e đã có, .env chỉ là fallback).
/// </summary>
public static class EnvFileLoader
{
    /// <summary>No-op nếu envFilePath không tồn tại.</summary>
    public static void LoadIfPresent(string envFilePath)
    {
        if (!File.Exists(envFilePath)) return;

        foreach (var raw in File.ReadAllLines(envFilePath))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            var idx = line.IndexOf('=');
            if (idx <= 0) continue;

            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();
            if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
                value = value[1..^1];

            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }
    }
}
