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
}
