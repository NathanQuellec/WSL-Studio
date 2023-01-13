using Microsoft.UI.Xaml.Data;

namespace WSLStudio.Views.Functions;

public static class BindingFormatHandler{

    public static string Concatenate(string? str1, string? str2)
    {
        return string.Concat(str1, str2);
    }

    public static string Concatenate(string? str1, string? str2, string? str3)
    {
        return string.Concat(str1, str2, str3);
    }

}