namespace WSLStudio.Models;

public class Snapshot : IBaseModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string CreationDate { get; set; }
    public string Size { get; set; } // size of the compressed snapshot distro
    public string DistroSize { get; set; } // real size of the distro
    public string Path { get; set; }
}