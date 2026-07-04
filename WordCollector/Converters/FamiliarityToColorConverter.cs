using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WordCollector.Converters;

public class FamiliarityToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value is int level
            ? level switch { 0 => "FamNewBrush", 1 => "FamLearningBrush", 2 => "FamMasteredBrush", _ => "TextTertiaryBrush" }
            : "TextTertiaryBrush";
        return Application.Current?.TryFindResource(key) as Brush ?? Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
