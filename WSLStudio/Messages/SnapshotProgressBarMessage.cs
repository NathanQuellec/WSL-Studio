namespace WSLStudio.Messages;

public class SnapshotProgressBarMessage
{
    public string ProgressInfo { get; }

    public SnapshotProgressBarMessage(string progressInfo) { this.ProgressInfo = progressInfo; }
}