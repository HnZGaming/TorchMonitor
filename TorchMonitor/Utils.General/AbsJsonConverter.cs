using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Utils.General
{
	internal abstract class AbsJsonConverter<T> : JsonConverter
	{
		protected abstract T Parse(string str);
		protected abstract string ReverseParse(T obj);

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(T);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var token = JToken.Load(reader);
			return Parse(token.ToString());
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value is T type)
			{
				var str = ReverseParse(type);
				var t = JToken.FromObject(str);
				t.WriteTo(writer);
			}
		}
	}
}