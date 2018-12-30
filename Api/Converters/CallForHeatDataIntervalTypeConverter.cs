using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoenZomers.Tado.Api.Converters
{
    public partial class CallForHeatDataIntervalTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(Enums.CallForHeatDataIntervalTypes) || t == typeof(Enums.CallForHeatDataIntervalTypes?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "NONE":
                    return Enums.CallForHeatDataIntervalTypes.None;
                case "LOW":
                    return Enums.CallForHeatDataIntervalTypes.Low;
                case "MEDIUM":
                    return Enums.CallForHeatDataIntervalTypes.Medium;
                case "HIGH":
                    return Enums.CallForHeatDataIntervalTypes.High;
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
            var value = (Enums.CallForHeatDataIntervalTypes)untypedValue;
            switch (value)
            {
                case Enums.CallForHeatDataIntervalTypes.None:
                    serializer.Serialize(writer, "NONE");
                    return;
                case Enums.CallForHeatDataIntervalTypes.Low:
                    serializer.Serialize(writer, "LOW");
                    return;
                case Enums.CallForHeatDataIntervalTypes.Medium:
                    serializer.Serialize(writer, "MEDIUM");
                    return;
                case Enums.CallForHeatDataIntervalTypes.High:
                    serializer.Serialize(writer, "HIGH");
                    return;
            }
            throw new Exception("Cannot marshal type State");
        }
    }
}
