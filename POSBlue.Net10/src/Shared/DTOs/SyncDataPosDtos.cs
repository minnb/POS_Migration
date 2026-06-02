namespace VCM.POSBLUE.Shared.DTOs;

public class DeleteFileModel
{
    public string? UrlServer { get; set; }
    public string? FileName { get; set; }
}

public class PathFileAPIModel
{
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public string? IPServer { get; set; }
    public string? PathFileIPServer { get; set; }
    public string? NetworkPathDisc { get; set; }
    public string? FolderAPI { get; set; }
}

public class ListFileNameModel
{
    public string? FileName { get; set; }
}
