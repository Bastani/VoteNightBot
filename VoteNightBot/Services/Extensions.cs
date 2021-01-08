using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Polenter.Serialization;

namespace VoteNightBot.Services
{
    public static class Extensions
    {
        public static string SerializeObject<T>(this T toSerialize)
        {
            if (toSerialize == null)
                return string.Empty;
            using var stream = new MemoryStream();
            var serializer = new SharpSerializer();
            serializer.Serialize(toSerialize, stream);
            stream.Position = 0;
            var output = new StreamReader(stream).ReadToEnd();
            return output;
        }

        public static T DeserializeObject<T>(this string toDeserialize) where T : class
        {
            if (string.IsNullOrWhiteSpace(toDeserialize))
            {
                return default;
            }

            var bytes = Encoding.UTF8.GetBytes(toDeserialize);
            using var stream = new MemoryStream(bytes);
            var serializer = new SharpSerializer();
            return serializer.Deserialize(stream) as T;
        }

        public static string GetEnvironmentVariable(string variable)
        {
            var token = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(token))
            {
                token = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User);
            }
            if (string.IsNullOrWhiteSpace(token))
            {
                token = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.Machine);
            }
            return token;
        }
    }
}
