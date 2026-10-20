using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SkladWPF
{
    public class StockColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Проверка на пустоту (null, DBNull или DependencyProperty.UnsetValue)
            if (values == null || values.Length < 2 || values[0] == null || values[1] == null)
                return Brushes.Transparent;

            if (values[0] == System.Windows.DependencyProperty.UnsetValue ||
                values[1] == System.Windows.DependencyProperty.UnsetValue)
                return Brushes.Transparent;

            try
            {
                // Безопасное преобразование в числа
                int prihod = System.Convert.ToInt32(values[0]);
                int ostatok = System.Convert.ToInt32(values[1]);

                if (prihod == 0) return Brushes.LightCoral; // Нет прихода - красный

                double percent = (double)ostatok / prihod * 100;

                if (percent > 50) return Brushes.LightGreen;
                if (percent >= 20) return Brushes.Khaki;
                return Brushes.LightCoral;
            }
            catch
            {
                // Если что-то пошло не так при конвертации, возвращаем прозрачный фон
                return Brushes.Transparent;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}