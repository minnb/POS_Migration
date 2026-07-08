namespace POS.Web.Services;

public sealed record LogFileInfo(
    string RelativePath,
    string FileName,
    string FolderName,
    long SizeBytes,
    DateTime LastModifiedUtc);
