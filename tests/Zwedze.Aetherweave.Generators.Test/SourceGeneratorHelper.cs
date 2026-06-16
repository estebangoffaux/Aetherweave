using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Zwedze.Aetherweave.Generators.Test;

public static class SourceGeneratorHelper<T> where T : IIncrementalGenerator, new()
{
    public static string? GetGeneratedText(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Cast<MetadataReference>();

        var compilation = CSharpCompilation.Create("SourceGeneratorTests",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Source Generator to test
        var generator = new T();

        CSharpGeneratorDriver
            .Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation,
                out var outputCompilation,
                out _);

        return outputCompilation.SyntaxTrees.Skip(1).LastOrDefault()?.ToString();
    }
}
