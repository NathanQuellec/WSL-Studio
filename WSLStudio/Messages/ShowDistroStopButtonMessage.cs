using WSLStudio.Models;

namespace WSLStudio.Messages;

public class ShowDistroStopButtonMessage
{
    public Distribution distribution { get; }
    public ShowDistroStopButtonMessage(Distribution distribution) { this.distribution = distribution; }
}