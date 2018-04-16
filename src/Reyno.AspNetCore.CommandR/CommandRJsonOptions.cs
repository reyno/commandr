using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;

namespace Reyno.AspNetCore.CommandR {

    public class CommandRJsonOptions {

        public JsonSerializerSettings SerializerSettings { get; set; } = JsonSerializerSettingsProvider.CreateSerializerSettings();
    }
}