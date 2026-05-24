using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AppealsFinal
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;
            if (string.IsNullOrEmpty(status)) return Brushes.White;
            if (status.Contains("Новое")) return (SolidColorBrush)new BrushConverter().ConvertFrom("#FFF9C4");
            if (status.Contains("В работе")) return (SolidColorBrush)new BrushConverter().ConvertFrom("#BBDEFB");
            if (status.Contains("Выполнено")) return (SolidColorBrush)new BrushConverter().ConvertFrom("#C8E6C9");
            if (status.Contains("Отклонено")) return (SolidColorBrush)new BrushConverter().ConvertFrom("#FFCDD2");
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}