using System.ComponentModel;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AgentOrange.Core.Skills;

sealed partial class AgentSkills
{
    [Description("Führt C# Code in einer isolierten Sandbox aus (CPU-only, stdout erlaubt).")]
    public Task<string> ExecuteSafeCSharp(string code)
    {
        // Idee für deinen Wrapper-Code:
        var lines = code.Split('\n');
        var usings = string.Join("\n", lines.Where(l => l.Trim().StartsWith("using ")));
        var body = string.Join("\n", lines.Where(l => !l.Trim().StartsWith("using ")));

        var finalCode =
            $$"""
             {{usings}}
             public static class Program {
                 public static void Main() {
                     {{body}}
                 }
             }
             """;

        try
        {
            // 1. Roslyn Compilation
            var syntaxTree = CSharpSyntaxTree.ParseText(finalCode);
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>()
                .ToList();

            var compilation = CSharpCompilation.Create(
                assemblyName: $"Guest_{Guid.NewGuid():N}",
                syntaxTrees: [syntaxTree],
                references: references,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication)
            );

            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);

            if (!result.Success)
            {
                var errors = string.Join(Environment.NewLine, result.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.ToString()));
                return Task.FromResult($"[Compiler Error]{Environment.NewLine}{errors}");
            }

            ms.Seek(0, SeekOrigin.Begin);

            // 2. Neue isolierte AssemblyLoadContext
            var alc = new AssemblyLoadContext("Sandbox", isCollectible: true);
            var assembly = alc.LoadFromStream(ms);

            // 3. stdout Capture
            var sw = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(sw);

            // 4. EntryPoint aufrufen
            var entry = assembly.EntryPoint;
            if (entry == null) return Task.FromResult("[Runtime Error]: Main method nicht gefunden");

            // Main ohne args
            var parameters = entry.GetParameters().Length == 0 ? null : new object[] { Array.Empty<string>() };
            entry.Invoke(null, parameters);

            Console.Out.Flush();
            Console.SetOut(originalOut);

            // 5. AssemblyLoadContext entladen
            alc.Unload();

            return Task.FromResult(sw.ToString());
        }
        catch (Exception ex)
        {
            return Task.FromResult($"[Fatal Error]: {ex}");
        }
    }
}