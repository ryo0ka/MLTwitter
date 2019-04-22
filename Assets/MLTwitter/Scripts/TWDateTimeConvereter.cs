using System;
using System.Globalization;
using Newtonsoft.Json;

namespace MLTwitter
{
	internal class TWDateTimeConvereter : JsonConverter
	{
		const string Template = "ddd MMM dd HH:mm:ss +ffff yyyy";
		static readonly CultureInfo CultureInfo = new CultureInfo("en-US");

		public override bool CanRead => true;
		public override bool CanWrite => false;

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(DateTime);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			string raw = serializer.Deserialize<string>(reader);
			return DateTime.ParseExact(raw, Template, CultureInfo);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}
}