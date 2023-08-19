
using System.Collections;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace WSLStudio.Helpers;

public class InputValidationHelper
{
    public InputValidationHelper NotNullOrWhiteSpace(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException($"cannot be empty.");
        }

        return this;
    }

    public InputValidationHelper IncludeWhiteSpaceChar(string input)
    {
        if (input.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException($"cannot include white spaces.");
        }
        return this;
    }

    // only min length -> max length is set with the xaml property of textbox
    public InputValidationHelper MinimumLength(string input, int minLength)
    {
        if (input.Length <= minLength)
        {
            throw new ArgumentException($"cannot be shorter than {minLength} characters.");
        }
        return this;
    }

    public InputValidationHelper InvalidCharacters(string input, Regex regex, string regexDescr)
    {
        if (!regex.IsMatch(input))
        {
            throw new ArgumentException($"cannot include {regexDescr}.");
        }
        return this;
    }

    public InputValidationHelper DataAlreadyExist(string input, IList collection)
    {
        if (collection.Contains(input))
        {
            throw new ArgumentException($"already exists.");
        }
        return this;
    }

    public InputValidationHelper ControlNotNull(object? control)
    {
        if (control == null)
        {
            throw new ArgumentException($"You must select one option.");
        }
        return this;
    }
}