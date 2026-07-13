using Microsoft.JSInterop;

namespace POS.Web.Services;

/// <summary>
/// Helper download file từ Blazor Server về trình duyệt — dùng chung cho mọi page.
/// Stream byte qua <see cref="DotNetStreamReference"/> (không base64) tới hàm JS
/// <c>posDownloadFileFromStream</c> (wwwroot/js/download.js).
/// </summary>
public static class JsDownloadExtensions
{
    public static async Task SaveAsFileAsync(
        this IJSRuntime js,
        string fileName,
        byte[] bytes,
        string contentType = "application/octet-stream")
    {
        using var stream = new MemoryStream(bytes);
        using var streamRef = new DotNetStreamReference(stream);
        await js.InvokeVoidAsync("posDownloadFileFromStream", fileName, contentType, streamRef);
    }

    /// <summary>
    /// Overload streaming thật — dùng cho file có thể lớn (vd log server): nhận thẳng 1 Stream đang
    /// mở (vd FileStream) thay vì byte[], tránh nạp toàn bộ nội dung vào RAM server trước khi gửi.
    /// Caller giữ quyền sở hữu <paramref name="stream"/> (leaveOpen: true) — phải tự dispose sau khi
    /// gọi xong.
    /// </summary>
    public static async Task SaveAsFileAsync(
        this IJSRuntime js,
        string fileName,
        Stream stream,
        string contentType = "application/octet-stream")
    {
        using var streamRef = new DotNetStreamReference(stream, leaveOpen: true);
        await js.InvokeVoidAsync("posDownloadFileFromStream", fileName, contentType, streamRef);
    }

    public static async Task<string> CreatePdfBlobUrlAsync(this IJSRuntime js, byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var streamRef = new DotNetStreamReference(stream);
        return await js.InvokeAsync<string>("posCreatePdfBlobUrl", streamRef);
    }

    public static async Task RevokeBlobUrlAsync(this IJSRuntime js, string? url)
    {
        if (!string.IsNullOrEmpty(url))
            await js.InvokeVoidAsync("posRevokeBlobUrl", url);
    }

    public static async Task DownloadFromBlobUrlAsync(this IJSRuntime js, string url, string fileName)
        => await js.InvokeVoidAsync("posDownloadFromBlobUrl", url, fileName);
}
