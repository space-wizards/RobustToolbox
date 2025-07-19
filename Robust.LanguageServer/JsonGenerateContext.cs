using System.Text.Json.Serialization;
using Robust.LanguageServer.Notifications;

namespace Robust.LanguageServer;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ProgressInfo))]
public partial class JsonGenerateContext : JsonSerializerContext;
