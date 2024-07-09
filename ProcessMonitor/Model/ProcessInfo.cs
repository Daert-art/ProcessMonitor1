namespace ProcessMonitor.Model;
public class ProcessInfo
{
    public uint Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Cpu { get; set; } = string.Empty;
    public string Memory {  get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

}
