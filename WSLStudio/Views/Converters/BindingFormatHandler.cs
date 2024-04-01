using Microsoft.UI.Xaml.Data;

namespace WSLStudio.Views.Converters;

public class BindingFormatHandler : IValueConverter {

    public object Convert(object value, Type targetType, object parameter, string language) 
    {
        string str1 = (string)value;
        string str2 = (string)parameter;
        return string.Concat(str1, str2);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return (string)value;
    }
}