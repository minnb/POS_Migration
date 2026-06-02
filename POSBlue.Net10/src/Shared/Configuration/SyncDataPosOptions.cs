namespace VCM.POSBLUE.Shared.Configuration;

public class SyncDataPosOptions
{
    public const string SectionName = "SyncDataPos";
    
    public string FtpRootPath { get; set; } = "FTPBLUEPOS";
    public string RemoteSvrUser { get; set; } = string.Empty;
    public string RemoteSvrPass { get; set; } = string.Empty;
    public string UploadFileFTP { get; set; } = "YES";
    public string FolderShare { get; set; } = string.Empty;
    public string FolderShareUpdSource { get; set; } = string.Empty;
    public string FolderShareAPIBluePOS { get; set; } = string.Empty;
    public string BootstrapServers { get; set; } = string.Empty;
}
