using ArcaneLibs;
using ArcaneLibs.Extensions.Streams;
using LibMatrix.LegacyEvents.EventTypes;

// string binary = args.Length > 1 ? args[0] : Console.ReadLine()!;

// var asm = Assembly.LoadFrom(binary);
File.Delete("EventSerializerContexts.g.cs");
var stream = File.OpenWrite("EventSerializerContexts.g.cs");
var eventContentTypes = new ClassCollector<EventContent>().ResolveFromAllAccessibleAssemblies();

stream.WriteString("using System.Text.Json.Serialization;\n");

stream.WriteString(string.Join('\n', eventContentTypes.DistinctBy(x => x.Namespace)
    .Select(x => $"using {x.Namespace};")));
stream.WriteString("\n\nnamespace LibMatrix.Generated;\n\n[JsonSourceGenerationOptions(WriteIndented = true)]\n");

// stream.WriteString(string.Join('\n', eventContentTypes//.DistinctBy(x => x.Namespace)
//     .Select(x => $$"""
//                   [JsonSourceGenerationOptions(WriteIndented = true)]
//                   [JsonSerializable(typeof({{x.Name}}))]
//                   internal partial class {{x.Name}}SerializerContext : JsonSerializerContext { }
//
//                   """)));

stream.WriteString(string.Join('\n', eventContentTypes //.DistinctBy(x => x.Namespace)
    .Select(x => $"[JsonSerializable(typeof({x.Name}))]")));

stream.WriteString("\ninternal partial class EventTypeSerializerContext : JsonSerializerContext { }");

await stream.FlushAsync();
stream.Close();