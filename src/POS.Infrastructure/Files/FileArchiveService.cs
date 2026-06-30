using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace POS.Infrastructure.Files;

/// <inheritdoc cref="IFileArchiveService"/>
public sealed class FileArchiveService(IOptions<MasterDataSyncOptions> options) : IFileArchiveService
{
    private readonly MasterDataSyncOptions _opt = options.Value;

    public void CreateZipFromDirectory(string sourceDir, string destZipPath)
    {
        // includeBaseDirectory:false → entry là tên file trực tiếp (vd STORE_Table_xx_1.txt),
        // khớp cách CreateFileSODFakeAsync đặt entry. ZipFile đọc từng file từ disk → không nạp toàn bộ vào RAM.
        // Mức nén theo cấu hình (mặc định Fastest) — Optimal tốn CPU/chậm với master data JSON lớn.
        ZipFile.CreateFromDirectory(sourceDir, destZipPath, _opt.ResolveCompressionLevel(), includeBaseDirectory: false);
    }
}
