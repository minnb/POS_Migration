namespace POS.Web.Services;

/// <summary>
/// Kết quả tải 1 file log. <see cref="Success"/> = true khi có nội dung; ngược lại
/// <see cref="ErrorMessage"/> mô tả lý do (quyền, quá lớn, không tồn tại...) để hiển thị lên UI.
/// <see cref="Content"/> là stream thật mở trên đĩa (không buffer toàn bộ file vào RAM) — caller
/// PHẢI dispose sau khi dùng xong (xem LogFilePage.razor.DownloadAsync).
/// </summary>
public sealed record LogFileDownload(Stream? Content, string? FileName, string? ErrorMessage)
{
    public bool Success => Content is not null && FileName is not null;

    public static LogFileDownload Ok(Stream content, string fileName) => new(content, fileName, null);
    public static LogFileDownload Fail(string error) => new(null, null, error);
}
