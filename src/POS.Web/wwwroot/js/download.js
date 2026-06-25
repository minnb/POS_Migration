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

// Tạo blob URL từ .NET stream để preview PDF trong iframe — KHÔNG tự download.
// Caller phải gọi posRevokeBlobUrl khi đóng preview để giải phóng memory.
window.posCreatePdfBlobUrl = async (streamRef) => {
    const arrayBuffer = await streamRef.arrayBuffer();
    const blob = new Blob([arrayBuffer], { type: 'application/pdf' });
    return URL.createObjectURL(blob);
};

// Giải phóng blob URL đã tạo bởi posCreatePdfBlobUrl.
window.posRevokeBlobUrl = (url) => {
    if (url) URL.revokeObjectURL(url);
};

// Download từ blob URL đã có (dùng lại URL, không cần stream lại từ server).
window.posDownloadFromBlobUrl = (url, fileName) => {
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName || 'report.pdf';
    document.body.appendChild(a);
    a.click();
    a.remove();
};
