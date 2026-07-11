namespace POS.Web.Services;

public sealed record LogFileInfo(
    string RelativePath,
    string FileName,
    long SizeBytes,
    DateTime LastModifiedUtc);
