using System.Text.RegularExpressions;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Zwedze.Aetherweave.Generators.Test;

public class SmartEnumGeneratorTest
{
    private string? _generatedCode;

    [SetUp]
    public void Setup()
    {
        var testCode = @"
using Zwedze.Aetherweave.SharedKernel;

namespace TcgFac.DataStreamGateway.Domain;

[Zwedze.Aetherweave.SmartEnum]
public sealed partial class SynchronizationSignatureType : SmartEnum<SynchronizationSignatureType>
{
    public static SynchronizationSignatureType Timestamp = new(""timestamp"");
    public static SynchronizationSignatureType Md5 = new(""md5"");
}
";
        _generatedCode = SourceGeneratorHelper<SmartEnumGenerator>.GetGeneratedText(testCode);
    }

    [Test]
    public void Should_GenerateCode_BePresent()
    {
        _generatedCode.Should().NotBeNull();
    }
    [Test]
    public void GenerateCode_Should_ValidClassDeclaration()
    {
        var tree = CSharpSyntaxTree.ParseText(_generatedCode!);
        var @class = tree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();
        @class.Should().NotBeNull();
        @class.Modifiers.Should().Contain(m => m.IsKind(SyntaxKind.PublicKeyword));
        @class.Modifiers.Should().Contain(m => m.IsKind(SyntaxKind.PartialKeyword));
        @class.Identifier.Text.Should().Be("SynchronizationSignatureType");
    }

    [Test]
    public void GenerateCode_Should_ValidPrivateConstructor()
    {
        var tree = CSharpSyntaxTree.ParseText(_generatedCode!);
        var constructor = tree.GetRoot()
            .DescendantNodes()
            .OfType<ConstructorDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == "SynchronizationSignatureType");
    
        constructor.Should().NotBeNull();
        constructor.Modifiers.Should().Contain(m => m.IsKind(SyntaxKind.PrivateKeyword));
    }
    
    [Test]
    public void GenerateCode_Should_ValidCodeProperty()
    {
        var tree = CSharpSyntaxTree.ParseText(_generatedCode!);
        var property = tree.GetRoot()
            .DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == "Code");
    
        property.Should().NotBeNull();
        property.Modifiers.Should().Contain(m => m.IsKind(SyntaxKind.PublicKeyword));
        // Check that it has a getter
        property.AccessorList.Should().NotBeNull();
        property.AccessorList!.Accessors
            .Should().Contain(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
    
        // Check that it has no setter
        property.AccessorList.Accessors
            .Should().NotContain(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) || 
                                      a.IsKind(SyntaxKind.InitAccessorDeclaration));
    }
    
    [Test]
    public void GenerateCode_Should_TwoStaticFromCodeMethods()
    {
        var tree = CSharpSyntaxTree.ParseText(_generatedCode!);
        var fromCodeMethods = tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Identifier.Text == "FromCode")
            .ToList();
    
        // Should have exactly 2 FromCode methods
        fromCodeMethods.Should().HaveCount(2);
    
        // Both should be public static
        foreach (var method in fromCodeMethods)
        {
            method.Modifiers.Should().Contain(m => m.IsKind(SyntaxKind.PublicKeyword));
            method.Modifiers.Should().Contain(m => m.IsKind(SyntaxKind.StaticKeyword));
        }
    
        // One should take Code<SynchronizationSignatureType> parameter
        var codeParameterMethod = fromCodeMethods
            .FirstOrDefault(m => m.ParameterList.Parameters.Count == 1 && 
                                 (m.ParameterList.Parameters[0].Type?.ToString().Contains("Code<SynchronizationSignatureType>") ?? false));
        codeParameterMethod.Should().NotBeNull("there should be a FromCode method accepting Code<SynchronizationSignatureType>");
    
        // One should take string parameter
        var stringParameterMethod = fromCodeMethods
            .FirstOrDefault(m => m.ParameterList.Parameters.Count == 1 && 
                                 m.ParameterList.Parameters[0].Type?.ToString() == "string");
        stringParameterMethod.Should().NotBeNull("there should be a FromCode method accepting string");
    }
    
    [Test]
    public void GenerateCode_Should_ValidAllValuesProperty()
    {
        var tree = CSharpSyntaxTree.ParseText(_generatedCode!);
        var property = tree.GetRoot()
            .DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(p => p.Identifier.Text == "AllValues");
    
        property.Should().NotBeNull();
    
        // Should be public static
        property.Modifiers.Should().Contain(m => m.IsKind(SyntaxKind.PublicKeyword));
        property.Modifiers.Should().Contain(m => m.IsKind(SyntaxKind.StaticKeyword));
    
        // Should return an array type
        property.Type.Should().BeOfType<ArrayTypeSyntax>();
    
        // Should use expression body (=>) syntax
        property.ExpressionBody.Should().NotBeNull("AllValues should use expression body syntax (=>)");
    
        // The expression should be an array creation
        property.ExpressionBody!.Expression.Should().BeOfType<ArrayCreationExpressionSyntax>();
    
        var arrayCreation = (ArrayCreationExpressionSyntax)property.ExpressionBody.Expression;
        arrayCreation.Initializer.Should().NotBeNull("AllValues array should have an initializer");
    
        // Should have 2 elements (Timestamp and Md5)
        arrayCreation.Initializer!.Expressions.Should().HaveCount(2);
    }
}
