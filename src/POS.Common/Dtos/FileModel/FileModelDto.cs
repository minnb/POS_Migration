namespace POS.Common.Dtos.FileModel;

// Migrated từ VCM.POSBLUE.Model.FileModel/FilePathModel.cs
// Tên property = tên JSON field — contract với POS client, KHÔNG đổi.

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
