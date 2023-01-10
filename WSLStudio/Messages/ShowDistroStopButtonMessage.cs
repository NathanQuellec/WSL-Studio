using WSLStudio.Models;

namespace WSLStudio.Messages;

public class ShowDistroStopButtonMessage
{
    public ShowDistroStopButtonMessage(Distribution distribution) { this.distribution = distribution; }
    public Distribution distribution { get; }
}