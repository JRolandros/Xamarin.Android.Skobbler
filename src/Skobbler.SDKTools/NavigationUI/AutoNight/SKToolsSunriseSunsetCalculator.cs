using Android.Util;
using Java.Util;
using System;
using TimeZone = Java.Util.TimeZone;

namespace Skobbler.Ngx.SDKTools.NavigationUI.AutoNight
{
    internal class SKToolsSunriseSunsetCalculator
    {

        private const string TAG = "SunriseSunsetCalculator";

        /// <summary>
        /// constants for Sun's zenith values for sunrise/sunset
        /// </summary>
        public const double OFFICIAL = 90.5;

        public const double CIVIL = 96.0;

        public const double NAUTICAL = 102.0;

        public const double ASTRONOMICAL = 108.0;

        public const int NR_OF_MILLISECONDS_IN_A_HOUR = 3600000;

        public static void calculateSunriseSunsetHours(double latitude, double longitude, double zenith)
        {
            calculateTime(latitude, longitude, zenith, true);
            calculateTime(latitude, longitude, zenith, false);
        }

        private static void calculateTime(double latitude, double longitude, double zenith, bool calculateSunrise)
        {

            double approximateTime;
            double meanAnomaly;
            double localHour;
            double universalTime;

            DateTime calendar = new DateTime();
            int currentYear = calendar.Year;
            int currentMonth = calendar.Month + 1;
            int currentDay = calendar.Day;

            // first calculate the day of the year
            double N1 = Math.Floor(275.0 * currentMonth / 9.0);
            double N2 = Math.Floor((currentMonth + 9.0) / 12.0);
            double N3 = (1 + Math.Floor((currentYear - 4.0 * Math.Floor(currentYear / 4.0) + 2.0) / 3.0));
            double dayOfYear = N1 - (N2 * N3) + currentDay - 30.0;

            // convert the longitude to hour value and calculate an approximate time
            double longHour = longitude / 15.0;
            if (calculateSunrise)
            {
                approximateTime = dayOfYear + ((6.0 - longHour) / 24.0);
            }
            else
            {
                approximateTime = dayOfYear + ((18.0 - longHour) / 24.0);
            }

            // calculate the Sun's mean anomaly

            meanAnomaly = (0.9856 * approximateTime) - 3.289;

            // calculate the Sun's true longitude

            double sunLongitude = meanAnomaly + (1.916 * Math.Sin(meanAnomaly * Math.PI / 180.0)) + (0.020 * Math.Sin(2 * meanAnomaly * Math.PI / 180.0)) + 282.634;

            sunLongitude = getNormalizedValue(sunLongitude, 360);

            // calculate the Sun's right ascension

            double sunRightAscension = Math.Atan(0.91764 * Math.Tan(sunLongitude * Math.PI / 180)) * 180.0 / Math.PI;
            sunRightAscension = getNormalizedValue(sunRightAscension, 360);
            // right ascension value needs to be in the same quadrant as L

            double longitudeQuadrant = (Math.Floor(sunLongitude / 90.0)) * 90.0;
            double rightAscensionQuadrant = (Math.Floor(sunRightAscension / 90.0)) * 90.0;
            sunRightAscension = sunRightAscension + (longitudeQuadrant - rightAscensionQuadrant);

            // right ascension value needs to be converted into hours

            sunRightAscension = sunRightAscension / 15.0;

            // calculate the Sun's declination

            double sunSinDeclination = 0.39782 * Math.Sin(sunLongitude * Math.PI / 180.0);
            double sunCosDeclination = Math.Cos(Math.Asin(sunSinDeclination));

            // calculate the Sun's local hour angle

            double cosLocalHour = (Math.Cos(zenith * Math.PI / 180.0) - (sunSinDeclination * Math.Sin(latitude * Math.PI / 180.0))) / (sunCosDeclination * Math.Cos(latitude * Math.PI / 180.0));

            if (cosLocalHour > 1)
            {
                return;

            }
            if (cosLocalHour < -1)
            {
                return;
            }

            // finish calculating localHour and convert into hours

            if (calculateSunrise)
            {
                localHour = 360.0 - Math.Acos(cosLocalHour) * 180.0 / Math.PI;
            }
            else
            {
                localHour = Math.Acos(cosLocalHour) * 180.0 / Math.PI;
            }
            localHour = localHour / 15.0;

            // calculate local mean time of rising/setting

            double localMeanTime = localHour + sunRightAscension - (0.06571 * approximateTime) - 6.622;

            // adjust back to UTC

            universalTime = localMeanTime - longHour;
            universalTime = getNormalizedValue(universalTime, 24);

            // convert UT value to local time zone of latitude/longitude

            int localOffset = CurrentTimezoneOffset;

            double localTime = getNormalizedValue(universalTime + localOffset, 24);


            int hour = (int)Math.Floor(localTime);
            int hourSeconds = (int)(3600 * (localTime - hour));
            int minute = hourSeconds / 60;

            if (calculateSunrise)
            {
                SKToolsDateUtils.AUTO_NIGHT_SUNRISE_HOUR = hour;
                SKToolsDateUtils.AUTO_NIGHT_SUNRISE_MINUTE = minute;
                Log.Debug(TAG, "Sunrise : " + SKToolsDateUtils.AUTO_NIGHT_SUNRISE_HOUR + ":" + SKToolsDateUtils.AUTO_NIGHT_SUNRISE_MINUTE);
            }
            else
            {
                SKToolsDateUtils.AUTO_NIGHT_SUNSET_HOUR = hour;
                SKToolsDateUtils.AUTO_NIGHT_SUNSET_MINUTE = minute;
                Log.Debug(TAG, "Sunset : " + SKToolsDateUtils.AUTO_NIGHT_SUNSET_HOUR + ":" + SKToolsDateUtils.AUTO_NIGHT_SUNSET_MINUTE);
            }
        }

        /// <summary>
        /// normalizes a value within a given range </summary>
        /// <param name="value"> </param>
        /// <param name="maxRange"> </param>
        /// <returns> normalize value </returns>
        private static double getNormalizedValue(double value, double maxRange)
        {
            while (value > maxRange)
            {
                value -= maxRange;
            }
            while (value < 0)
            {
                value += maxRange;
            }
            return value;
        }

        /// <summary>
        /// calculates current timezone offset </summary>
        /// <returns> the current timezone offset </returns>
        private static int CurrentTimezoneOffset
        {
            get
            {
                TimeZone timezone = TimeZone.Default;
                Calendar calendar = GregorianCalendar.GetInstance(timezone);
                int offsetInMillis = timezone.GetOffset(calendar.TimeInMillis);
                int offset = offsetInMillis / NR_OF_MILLISECONDS_IN_A_HOUR;
                return offset;
            }
        }
    }
}