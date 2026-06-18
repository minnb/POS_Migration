namespace POS.Common.Dtos.Ops;

public class PosMonitorStatusDto
{
    public string   StoreNo               { get; set; } = string.Empty;
    public string   PosTerminalID         { get; set; } = string.Empty;
    public string   IpAddress             { get; set; } = string.Empty;
    public string   ComputerName          { get; set; } = string.Empty;
    public string   BluePosVersion        { get; set; } = string.Empty;
    public string   BluePosVersionUpdate  { get; set; } = string.Empty;
    public string   BluePosDatabaseStatus { get; set; } = string.Empty;
    public bool     IsOpenBluePos         { get; set; }
    public DateTime DateTimePos           { get; set; }
    public DateTime LastTimeInsertAll     { get; set; }
    public DateTime LastTimeInsertChange  { get; set; }
    public DateTime UpdatedAt             { get; set; }

    public bool IsOnline    => (DateTime.Now - UpdatedAt).TotalMinutes < 10;
    public bool NeedsUpdate => !string.IsNullOrEmpty(BluePosVersionUpdate)
                               && BluePosVersion != BluePosVersionUpdate;
}
