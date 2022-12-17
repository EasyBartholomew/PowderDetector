using System.Globalization;
using System.Windows.Controls;

namespace PowderDetector.Validation
{
    public class DoubleValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string text)
                return double.TryParse(text, NumberStyles.AllowDecimalPoint, 
                    CultureInfo.CurrentCulture, out _)
                    ? ValidationResult.ValidResult
                    : new ValidationResult(false, "Не удалось распознать значение!");

            return new ValidationResult(false, "Некорректный тип данных!");
        }
    }
}
