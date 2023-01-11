using Microsoft.UI.Xaml.Data;

namespace WSLStudio.Views.Functions;

public static class BindingFormatHandler{

    public static string ConcatenateString(string? str, string? distroName)
    {
        return string.Concat($"{str}_", distroName);
    }
   
}