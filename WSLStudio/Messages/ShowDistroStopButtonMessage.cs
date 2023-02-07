using WSLStudio.Models;

namespace WSLStudio.Messages;

public class ShowDistroStopButtonMessage
{
    public Distribution Distribution { get; }

    public ShowDistroStopButtonMessage(Distribution distribution)
    {
        this.Distribution = distribution; 

    }
}