using System.Text.RegularExpressions;

namespace WSLStudio.Helpers;

public class TextInputValidationHelper
{
    private readonly string _textInput;

    public TextInputValidationHelper(string textInput)
    {
        _textInput = textInput;
    }

    public TextInputValidationHelper NotNullOrWhiteSpace()
    {
        if (string.IsNullOrWhiteSpace(_textInput))
        {
            throw new ArgumentException($"cannot be empty.");
        }

        return this;
    }

    public TextInputValidationHelper IncludeWhiteSpaceChar()
    {
        if (_textInput.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException($"cannot include white spaces.");
        }
        return this;
    }

    // only min length -> max length is set with the xaml property of textbox
    public TextInputValidationHelper MinimumLength(int minLength)
    {
        if (_textInput.Length < minLength)
        {
            throw new ArgumentException($"must be at least {minLength} characters long.");
        }
        return this;
    }

    public TextInputValidationHelper InvalidCharacters(Regex regex, string regexDescr)
    {
        if (!regex.IsMatch(_textInput))
        {
            throw new ArgumentException($"cannot include {regexDescr}.");
        }
        return this;
    }

    public TextInputValidationHelper DataAlreadyExist(List<string> collection)
    {
        if (collection.Contains(_textInput, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"already exists.");
        }
        return this;
    }
}