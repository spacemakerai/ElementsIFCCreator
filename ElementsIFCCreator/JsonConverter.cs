using Newtonsoft.Json;
using static FormaAPI.CommonClient;

namespace ElementsIFCCreator
{
    public class JsonConverter : IJsonConverter
    {
        public T Deserialize<T>(string str)
        {
            return JsonConvert.DeserializeObject<T>(str);
        }

        public void DeserializeFromObject<T>(object source, ref T dest) where T : new()
        {
            dest = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source));
        }

        public string Serialize<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}
