using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Akwaba.Desktop;

public class BoolVersVisibilite : IValueConverter
{
    public bool Inverser { get; set; }
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        var b = value is true;
        if (Inverser) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
        (value is Visibility.Visible) ^ Inverser;
}
