namespace ChatApp.Models;

public class DbStatusInfo
{
    public string FilePath { get; set; } = "";
    public int TableCount { get; set; }
    public int TotalRowCount { get; set; }
    public Dictionary<string,int> TableRowCounts { get; set; } = new();
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}
