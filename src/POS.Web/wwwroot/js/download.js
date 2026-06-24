// Tải file từ .NET (DotNetStreamReference) về máy người dùng — dùng chung cho mọi page.
// Gọi từ C#: await JS.SaveAsFileAsync(fileName, bytes, contentType) (xem JsDownloadExtensions).
window.posDownloadFileFromStream = async (fileName, contentType, streamRef) => {
    const arrayBuffer = await streamRef.arrayBuffer();
    const blob = new Blob([arrayBuffer], { type: contentType || 'application/octet-stream' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName || 'download';
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
};
