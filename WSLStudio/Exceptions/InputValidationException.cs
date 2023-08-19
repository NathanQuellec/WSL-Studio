namespace WSLStudio.Exceptions;

public class InputValidationException : Exception
{
    public InputValidationException() { }

    public InputValidationException(string message) : base(message) { }
}