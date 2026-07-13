namespace POS.Web.Services;

public sealed record LogDirectoryListing(
    string CurrentRelativePath,
    IReadOnlyList<LogFolderInfo> Folders,
    IReadOnlyList<LogFileInfo> Files,
    string? ErrorMessage = null);
