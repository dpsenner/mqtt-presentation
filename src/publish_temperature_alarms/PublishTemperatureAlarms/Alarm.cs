using System;
using System.Collections.Generic;
using System.Text;

namespace PublishTemperatureAlarms
{
    public class Alarm : IEquatable<Alarm>
    {
        public string ApplicationId { get; set; }

        public string Component { get; set; }

        public double Temperature { get; set; }

        public bool Equals(Alarm other)
        {
            if (other == null)
            {
                return false;
            }

            if (other.ApplicationId != ApplicationId)
            {
                return false;
            }

            if (other.Component != Component)
            {
                return false;
            }

            return true;
        }
    }
}
