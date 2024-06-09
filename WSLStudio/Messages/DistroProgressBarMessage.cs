namespace WSLStudio.Messages;

public class DistroProgressBarMessage
{
    public string ProgressInfo { get; }

    public DistroProgressBarMessage(string progressInfo) { this.ProgressInfo = progressInfo; }
}