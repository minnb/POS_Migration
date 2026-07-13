namespace POS.Web.Services;

public sealed record LogFolderInfo(
    string Name,
    string RelativePath,
    bool HasSubfolders = false);
