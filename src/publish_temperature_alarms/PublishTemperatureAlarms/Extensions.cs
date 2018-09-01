using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PublishTemperatureAlarms
{
    public static class Extensions
    {
        public static double ToDouble(this string value)
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        public static int ToInt32(this string value)
        {
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
    }
}
