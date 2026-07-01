namespace POS.Infrastructure.Files;

/// <summary>
/// Nén thư mục thành .zip. Tách khỏi service để giữ I/O ở Infrastructure.
/// Convention: System.IO.Compression (giống DataRawService.CreateFileSODFakeAsync).
/// </summary>
public interface IFileArchiveService
{
    /// <summary>
    /// Nén toàn bộ file trong sourceDir thành destZipPath (không kèm thư mục gốc).
    /// destZipPath nên là tên tạm — caller tự File.Move sang tên chính thức để publish atomic.
    /// </summary>
    void CreateZipFromDirectory(string sourceDir, string destZipPath);

    /// <summary>
    /// Giải nén zipPath vào destDir (tạo destDir nếu chưa có, ghi đè file trùng tên).
    /// Dùng cho PosFileImportWorker: bung .zip ra các .txt để insert DB.
    /// </summary>
    void ExtractToDirectory(string zipPath, string destDir);
}
