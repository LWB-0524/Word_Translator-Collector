using System.Globalization;
using System.Windows.Data;

namespace WordCollector.Converters;

public class FamiliarityToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is int level
            ? level switch { 0 => "陌生", 1 => "学习中", 2 => "已掌握", _ => "未知" }
            : "未知";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is string text
            ? text switch { "陌生" => 0, "学习中" => 1, "已掌握" => 2, _ => 0 }
            : 0;
}
