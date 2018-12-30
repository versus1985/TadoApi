using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoenZomers.Tado.Api.Converters
{
    public class WeatherStateConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(Enums.WeatherStates) || t == typeof(Enums.WeatherStates?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "NIGHT_CLEAR":
                    return Enums.WeatherStates.NightClear;
                case "SUN":
                    return Enums.WeatherStates.Sun;
                case "NIGHT_CLOUDY":
                    return Enums.WeatherStates.NightCloudy;
                case "FOGGY":
                    return Enums.WeatherStates.Foggy;
                case "CLOUDY_PARTLY":
                    return Enums.WeatherStates.CloudyPartly;
                case "CLOUDY_MOSTLY":
                    return Enums.WeatherStates.CloudyMostly;
                default:
                    return Enums.WeatherStates.Other;
            }
            throw new Exception("Cannot unmarshal type State");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (Enums.WeatherStates)untypedValue;
            switch (value)
            {
                case Enums.WeatherStates.NightClear:
                    serializer.Serialize(writer, "NIGHT_CLEAR");
                    return;
                case Enums.WeatherStates.Sun:
                    serializer.Serialize(writer, "SUN");
                    return;
                case Enums.WeatherStates.NightCloudy:
                    serializer.Serialize(writer, "NIGHT_CLOUDY");
                    return;
                case Enums.WeatherStates.Foggy:
                    serializer.Serialize(writer, "FOGGY");
                    return;
                case Enums.WeatherStates.CloudyPartly:
                    serializer.Serialize(writer, "CLOUDY_PARTLY");
                    return;
                case Enums.WeatherStates.CloudyMostly:
                    serializer.Serialize(writer, "CLOUDY_MOSTLY");
                    return;
                default:
                    serializer.Serialize(writer, "");
                    return;
            }
            throw new Exception("Cannot marshal type State");
        }
    }
}
