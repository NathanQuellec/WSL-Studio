
using System.Collections;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace WSLStudio.Helpers;

public class TextInputValidationHelper
{
    private readonly string _textInputl;

    public TextInputValidationHelper(string textInput)
    {
        _textInputl = textInput;
    }

    public TextInputValidationHelper NotNullOrWhiteSpace()
    {
        if (string.IsNullOrWhiteSpace(_textInputl))
        {
            throw new ArgumentException($"cannot be empty.");
        }

        return this;
    }

    public TextInputValidationHelper IncludeWhiteSpaceChar()
    {
        if (_textInputl.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException($"cannot include white spaces.");
        }
        return this;
    }

    // only min length -> max length is set with the xaml property of textbox
    public TextInputValidationHelper MinimumLength(int minLength)
    {
        if (_textInputl.Length < minLength)
        {
            throw new ArgumentException($"must be at least {minLength} characters long.");
        }
        return this;
    }

    public TextInputValidationHelper InvalidCharacters(Regex regex, string regexDescr)
    {
        if (!regex.IsMatch(_textInputl))
        {
            throw new ArgumentException($"cannot include {regexDescr}.");
        }
        return this;
    }

    public TextInputValidationHelper DataAlreadyExist(IList collection)
    {
        if (collection.Contains(_textInputl))
        {
            throw new ArgumentException($"already exists.");
        }
        return this;
    }
}