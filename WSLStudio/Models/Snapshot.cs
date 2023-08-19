namespace WSLStudio.Models;

public class Snapshot 
{

    public Guid Id { get; init; }
    public string Name { get; set; }
    public string CreationDate { get; set; }
    public string Description { get; set; }
}