using System.Text.RegularExpressions;

namespace WSLStudio.Helpers;

public class TextInputValidation
{
    private readonly string _textInput;

    public TextInputValidation(string textInput)
    {
        _textInput = textInput;
    }

    public TextInputValidation NotNullOrWhiteSpace()
    {
        if (string.IsNullOrWhiteSpace(_textInput))
        {
            throw new ArgumentException($"cannot be empty.");
        }

        return this;
    }

    public TextInputValidation IncludeWhiteSpaceChar()
    {
        if (_textInput.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException($"cannot include white spaces.");
        }
        return this;
    }

    // only min length -> max length is set with the xaml property of textbox
    public TextInputValidation MinimumLength(int minLength)
    {
        if (_textInput.Length < minLength)
        {
            throw new ArgumentException($"must be at least {minLength} characters long.");
        }
        return this;
    }

    public TextInputValidation InvalidCharacters(Regex regex, string regexDescr)
    {
        if (!regex.IsMatch(_textInput))
        {
            throw new ArgumentException($"cannot include {regexDescr}.");
        }
        return this;
    }

    public TextInputValidation DataAlreadyExist(List<string> collection)
    {
        if (collection.Contains(_textInput, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"already exists.");
        }
        return this;
    }
}