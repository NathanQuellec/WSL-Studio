namespace WSLStudio.Messages;

public class ProgressBarMessage
{
    public string ProgressInfo
    {
        get;
    }

    public ProgressBarMessage(string progressInfo)
    {
        this.ProgressInfo = progressInfo;
    }
}