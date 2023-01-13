using WSLStudio.Models;

namespace WSLStudio.Messages;

public class HideDistroStopButtonMessage
{
    public Distribution distribution { get; }
    public HideDistroStopButtonMessage(Distribution distribution) { this.distribution = distribution; }
}