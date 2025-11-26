using Newtonsoft.Json;
using System;
using System.Text;

namespace Flowsave.Serialization
{
    public class JsonSerializer : ISerializer
    {
        public SerializationType Format { get; } = SerializationType.Json;

        private readonly JsonSerializerSettings _settings;

        public JsonSerializer(JsonOptions options)
        {
            _settings = new JsonSerializerSettings
            {
                Formatting = options.PrettyPrint ? Formatting.Indented : Formatting.None,
                NullValueHandling = options.IncludeNulls ? NullValueHandling.Include : NullValueHandling.Ignore,
                TypeNameHandling = options.TypeHinting
                    ? TypeNameHandling.All       // full type hinting
                    : TypeNameHandling.Auto      // minimal
            };
        }

        public Result<byte[]> Serialize<T>(T data)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data, _settings);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                return Result<byte[]>.Success(bytes);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"JSON serialize failed: {ex.Message}");
            }
        }

        public Result<T> Deserialize<T>(byte[] data)
        {
            if (data == null)
                return Result<T>.Failure("Input is null.");

            try
            {
                string json = Encoding.UTF8.GetString(data);
                T obj = JsonConvert.DeserializeObject<T>(json, _settings);
                return Result<T>.Success(obj);
            }
            catch (Exception ex)
            {
                return Result<T>.Failure($"JSON deserialize failed: {ex.Message}");
            }
        }
    }
}
