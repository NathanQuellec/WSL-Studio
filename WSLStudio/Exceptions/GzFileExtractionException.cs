namespace WSLStudio.Exceptions;

public class GzFileExtractionException : Exception
{
    public GzFileExtractionException(){}

    public GzFileExtractionException(string message) : base(message) { }
}