namespace WSLStudio.Models;

public record RenameDistroCmd
{
    public Distribution distribution { get; set; }
    public string newDistroName { get; set; }

}