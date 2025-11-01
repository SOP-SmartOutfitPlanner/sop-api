using Qdrant.Client.Grpc;

namespace SOPServer.Service.Utils
{
    public static class QdrantUtils
    {
        public static Value ConvertToQdrantValue(object value)
        {
            return value switch
            {
                string s => new Value { StringValue = s },
                int i => new Value { IntegerValue = i },
                long l => new Value { IntegerValue = l },
                double d => new Value { DoubleValue = d },
                float f => new Value { DoubleValue = f },
                bool b => new Value { BoolValue = b },
                _ => new Value { StringValue = value?.ToString() ?? "" }
            };
        }

        public static object ConvertFromQdrantValue(Value value)
        {
            return value.KindCase switch
            {
                Value.KindOneofCase.StringValue => value.StringValue,
                Value.KindOneofCase.IntegerValue => value.IntegerValue,
                Value.KindOneofCase.DoubleValue => value.DoubleValue,
                Value.KindOneofCase.BoolValue => value.BoolValue,
                Value.KindOneofCase.ListValue => value.ListValue,
                Value.KindOneofCase.StructValue => value.StructValue,
                _ => null
            };
        }
    }
}
