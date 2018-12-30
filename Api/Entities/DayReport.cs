
using System;
using System.Collections.Generic;

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace KoenZomers.Tado.Api.Entities
{

    public partial class DayReport : IDisposable
    {
        [JsonProperty("zoneType")]
        public string ZoneType { get; set; }

        [JsonProperty("interval")]
        public Interval Interval { get; set; }

        [JsonProperty("hoursInDay")]
        public long HoursInDay { get; set; }

        [JsonProperty("measuredData")]
        public MeasuredData MeasuredData { get; set; }

        [JsonProperty("stripes")]
        public Stripes Stripes { get; set; }

        [JsonProperty("settings")]
        public Settings Settings { get; set; }

        [JsonProperty("callForHeat")]
        public CallForHeat CallForHeat { get; set; }

        [JsonProperty("weather")]
        public Weather Weather { get; set; }

        public void Dispose()
        {
        }
    }

    public partial class CallForHeat
    {
        [JsonProperty("timeSeriesType")]
        public string TimeSeriesType { get; set; }

        [JsonProperty("valueType")]
        public string ValueType { get; set; }

        [JsonProperty("dataIntervals")]
        public CallForHeatDataInterval[] DataIntervals { get; set; }
    }

    public partial class CallForHeatDataInterval
    {
        [JsonProperty("from")]
        public DateTimeOffset From { get; set; }

        [JsonProperty("to")]
        public DateTimeOffset To { get; set; }

        [JsonProperty("value")]
        [JsonConverter(typeof(Converters.CallForHeatDataIntervalTypeConverter))]
        public Enums.CallForHeatDataIntervalTypes CallForHeatDataIntervalType { get; set; }
    }

    public partial class Interval
    {
        [JsonProperty("from")]
        public DateTimeOffset From { get; set; }

        [JsonProperty("to")]
        public DateTimeOffset To { get; set; }
    }

    public partial class MeasuredData
    {
        [JsonProperty("measuringDeviceConnected")]
        public MeasuringDeviceConnected MeasuringDeviceConnected { get; set; }

        [JsonProperty("insideTemperature")]
        public InsideTemperature InsideTemperature { get; set; }

        [JsonProperty("humidity")]
        public Humidity Humidity { get; set; }
    }

    public partial class Humidity
    {
        [JsonProperty("timeSeriesType")]
        public string TimeSeriesType { get; set; }

        [JsonProperty("valueType")]
        public string ValueType { get; set; }

        [JsonProperty("percentageUnit")]
        public string PercentageUnit { get; set; }

        [JsonProperty("min")]
        public double Min { get; set; }

        [JsonProperty("max")]
        public double Max { get; set; }

        [JsonProperty("dataPoints")]
        public HumidityDataPoint[] DataPoints { get; set; }
    }

    public partial class HumidityDataPoint
    {
        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }
    }

    public partial class InsideTemperature
    {
        [JsonProperty("timeSeriesType")]
        public string TimeSeriesType { get; set; }

        [JsonProperty("valueType")]
        public string ValueType { get; set; }

        [JsonProperty("min")]
        public Max Min { get; set; }

        [JsonProperty("max")]
        public Max Max { get; set; }

        [JsonProperty("dataPoints")]
        public InsideTemperatureDataPoint[] DataPoints { get; set; }
    }

    public partial class InsideTemperatureDataPoint
    {
        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("value")]
        public Max Value { get; set; }
    }

    public partial class Max
    {
        [JsonProperty("celsius")]
        public double Celsius { get; set; }

        [JsonProperty("fahrenheit")]
        public double Fahrenheit { get; set; }
    }

    public partial class MeasuringDeviceConnected
    {
        [JsonProperty("timeSeriesType")]
        public string TimeSeriesType { get; set; }

        [JsonProperty("valueType")]
        public string ValueType { get; set; }

        [JsonProperty("dataIntervals")]
        public MeasuringDeviceConnectedDataInterval[] DataIntervals { get; set; }
    }

    public partial class MeasuringDeviceConnectedDataInterval
    {
        [JsonProperty("from")]
        public DateTimeOffset From { get; set; }

        [JsonProperty("to")]
        public DateTimeOffset To { get; set; }

        [JsonProperty("value")]
        public bool Value { get; set; }
    }

    public partial class Settings
    {
        [JsonProperty("timeSeriesType")]
        public string TimeSeriesType { get; set; }

        [JsonProperty("valueType")]
        public string ValueType { get; set; }

        [JsonProperty("dataIntervals")]
        public SettingsDataInterval[] DataIntervals { get; set; }
    }

    public partial class SettingsDataInterval
    {
        [JsonProperty("from")]
        public DateTimeOffset From { get; set; }

        [JsonProperty("to")]
        public DateTimeOffset To { get; set; }

        [JsonProperty("value")]
        public SettingClass Value { get; set; }
    }

    public partial class SettingClass
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("power")]
        public string Power { get; set; }

        [JsonProperty("temperature")]
        public Max Temperature { get; set; }
    }

    public partial class Stripes
    {
        [JsonProperty("timeSeriesType")]
        public string TimeSeriesType { get; set; }

        [JsonProperty("valueType")]
        public string ValueType { get; set; }

        [JsonProperty("dataIntervals")]
        public StripesDataInterval[] DataIntervals { get; set; }
    }

    public partial class StripesDataInterval
    {
        [JsonProperty("from")]
        public DateTimeOffset From { get; set; }

        [JsonProperty("to")]
        public DateTimeOffset To { get; set; }

        [JsonProperty("value")]
        public PurpleValue Value { get; set; }
    }

    public partial class PurpleValue
    {
        [JsonProperty("stripeType")]
        public string StripeType { get; set; }

        [JsonProperty("setting")]
        public SettingClass Setting { get; set; }
    }

    public partial class Weather
    {
        [JsonProperty("condition")]
        public Condition Condition { get; set; }

        [JsonProperty("sunny")]
        public MeasuringDeviceConnected Sunny { get; set; }

        [JsonProperty("slots")]
        public Slots Slots { get; set; }
    }

    public partial class Condition
    {
        [JsonProperty("timeSeriesType")]
        public string TimeSeriesType { get; set; }

        [JsonProperty("valueType")]
        public string ValueType { get; set; }

        [JsonProperty("dataIntervals")]
        public ConditionDataInterval[] DataIntervals { get; set; }
    }

    public partial class ConditionDataInterval
    {
        [JsonProperty("from")]
        public DateTimeOffset From { get; set; }

        [JsonProperty("to")]
        public DateTimeOffset To { get; set; }

        [JsonProperty("value")]
        public Slot Value { get; set; }
    }

    public partial class Slot
    {
        [JsonProperty("state")]
        [JsonConverter(typeof(Converters.WeatherStateConverter))]
        public Enums.WeatherStates State { get; set; }

        [JsonProperty("temperature")]
        public Max Temperature { get; set; }
    }

    public partial class Slots
    {
        [JsonProperty("timeSeriesType")]
        public string TimeSeriesType { get; set; }

        [JsonProperty("valueType")]
        public string ValueType { get; set; }

        [JsonProperty("slots")]
        public Dictionary<string, Slot> SlotsSlots { get; set; }
    }


    //internal static class Converter
    //{
    //    public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    //    {
    //        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
    //        DateParseHandling = DateParseHandling.None,
    //        Converters =
    //        {
    //            StateConverter.Singleton,
    //            new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
    //        },
    //    };
    //}

    
}
