namespace Gu.Wpf.ModernUI.TypeConverters
{
    using System;
    using System.Globalization;
    using System.Linq;

    internal class NullableBoolConverter : ITypeConverter<bool?>
    {
        internal static readonly NullableBoolConverter Default = new NullableBoolConverter();
        private static readonly Type[] ValidTypes =
        {
            typeof(bool),
        };

        /// <inheritdoc/>
        public bool IsValid(object value)
        {
            if (value == null)
            {
                return true;
            }

            if (ValidTypes.Contains(value.GetType()))
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool CanConvertTo(object value, CultureInfo culture)
        {
            if (value == null)
            {
                return true;
            }

            if (ValidTypes.Contains(value.GetType()))
            {
                return true;
            }

            var s = value as string;
            if (s != null)
            {
                bool temp;
                return bool.TryParse(s, out temp);
            }

            return false;
        }

        /// <inheritdoc/>
        public bool? ConvertTo(object value, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (ValidTypes.Contains(value.GetType()))
            {
                return (bool)Convert.ChangeType(value, typeof(bool));
            }

            var s = value as string;
            if (s != null)
            {
                return bool.Parse(s);
            }

            throw new ArgumentException("value");
        }

        /// <inheritdoc/>
        object ITypeConverter.ConvertTo(object value, CultureInfo culture)
        {
            return this.ConvertTo(value, culture);
        }
    }
}