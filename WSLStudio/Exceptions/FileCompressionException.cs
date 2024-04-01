namespace WSLStudio.Exceptions;

public class FileCompressionException : Exception
{
    public FileCompressionException()
    {
    }

    public FileCompressionException(string message) : base(message) { }
}