using Microsoft.UI.Xaml.Data;

namespace WSLStudio.Views.Functions;

public static class BindingFormatHandler{

    public static string ConcatenateBinding(string str, string distroName)
    {
        if (str == null)
            return null;

        if (distroName == null)
            return null;

        return string.Concat($"{str}_", distroName);
    }
   
}