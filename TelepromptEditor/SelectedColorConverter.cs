using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Teleprompter
{
    class SelectedColorConverter : IValueConverter
    {
        string previous;
        Brush cache;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && (bool)value)
            {
                if (parameter != null)
                {
                    var name = parameter.ToString();
                    if (name == previous)
                    {
                        return cache;
                    }
                    Color c = (Color)ColorConverter.ConvertFromString(name);
                    previous = name;
                    cache = new SolidColorBrush(c);
                    return cache;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
