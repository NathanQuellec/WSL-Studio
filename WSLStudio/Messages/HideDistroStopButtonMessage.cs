using WSLStudio.Models;

namespace WSLStudio.Messages;

public class HideDistroStopButtonMessage
{
    public HideDistroStopButtonMessage(Distribution distribution) { this.distribution = distribution; }
    public Distribution distribution { get; }
}