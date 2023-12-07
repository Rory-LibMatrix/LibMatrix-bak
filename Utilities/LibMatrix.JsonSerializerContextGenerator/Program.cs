using System.Reflection;
using ArcaneLibs;
using ArcaneLibs.Extensions.Streams;
using LibMatrix;
using LibMatrix.Interfaces;

// string binary = args.Length > 1 ? args[0] : Console.ReadLine()!;

// var asm = Assembly.LoadFrom(binary);
File.Delete("EventSerializerContexts.g.cs");
var stream = File.OpenWrite("EventSerializerContexts.g.cs");
var eventContentTypes = new ClassCollector<EventContent>().ResolveFromAllAccessibleAssemblies();
stream.WriteString(string.Join('\n', eventContentTypes//.DistinctBy(x => x.Namespace)
    .Select(x => $$"""
                  [System.Text.Json.Serialization.JsonSourceGenerationOptions(WriteIndented = true)]
                  [System.Text.Json.Serialization.JsonSerializable(typeof({{x.FullName}}))]
                  internal partial class {{x.Name}}SerializerContext : System.Text.Json.Serialization.JsonSerializerContext { }

                  """)));

await stream.FlushAsync();
stream.Close();