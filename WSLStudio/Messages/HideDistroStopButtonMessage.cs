using WSLStudio.Models;

namespace WSLStudio.Messages;

public class HideDistroStopButtonMessage
{
    public Distribution Distribution { get; }

    public HideDistroStopButtonMessage(Distribution distribution)
    {
        this.Distribution = distribution; 

    }
}