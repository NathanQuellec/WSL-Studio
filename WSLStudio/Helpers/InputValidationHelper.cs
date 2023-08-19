
using System.Collections;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Controls.Primitives;
using WSLStudio.Exceptions;

namespace WSLStudio.Helpers;

public class InputValidationHelper
{
    public void NotNullOrWhiteSpace(string input, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new InputValidationException(errorMessage);
        }
    }

    public void IncludeWhiteSpaceChar(string input, string errorMessage)
    {
        if (input.Any(char.IsWhiteSpace))
        {
            throw new InputValidationException(errorMessage);
        }
    }

    // only min length -> max length is set with the xaml property of textbox
    public void MinimumLength(string input, int minLength, string errorMessage)
    {
        if (input.Length <= minLength)
        {
            throw new InputValidationException(errorMessage);
        }
    }

    public void ValidCharacters(string input, Regex regex, string errorMessage)
    {
        if (!regex.IsMatch(input))
        {
            throw new InputValidationException(errorMessage);
        }
    }

    public void DataAlreadyExist(string input, IList collection, string errorMessage)
    {
        if (collection.Contains(input))
        {
            throw new InputValidationException(errorMessage);
        }
    }

    public void SelectorNotNull(Selector selector, string errorMessage)
    {
        if (selector == null)
        {
            throw new InputValidationException(errorMessage);
        }
    }
}