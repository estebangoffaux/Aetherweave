using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Zwedze.Aetherweave.Analyzers;

internal abstract class PropertyDuplicationAnalyzer<TProperty> : DiagnosticAnalyzer
{
    protected abstract string DiagnosticId { get; }
    protected abstract string Title { get; }
    protected abstract string MessageFormat { get; }
    protected abstract string Description { get; }
    protected abstract string Category { get; }
    protected abstract string PropertyName { get; }

    private DiagnosticDescriptor Rule => new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        true,
        Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected abstract TProperty GroupByExpression(ArgumentSyntax argument);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.RecordDeclaration);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        IEnumerable<FieldDeclarationSyntax> fieldDeclarations;
        switch (context.Node)
        {
            case ClassDeclarationSyntax classDeclaration:
                fieldDeclarations = classDeclaration.Members.OfType<FieldDeclarationSyntax>();
                break;

            case RecordDeclarationSyntax recordDeclaration:
                fieldDeclarations = recordDeclaration.Members.OfType<FieldDeclarationSyntax>();
                break;

            default:
                return;
        }

        var argumentSyntaxes = fieldDeclarations
            .Select(GetArgument)
            .Where(record => record != null)
            .OfType<ArgumentSyntax>()
            .ToList();

        var arguments = argumentSyntaxes
            .GroupBy(GroupByExpression)
            .Where(x => x.Count() > 1);

        foreach (var argument in arguments)
        {
            foreach (var invalidArgument in argument.Skip(1))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, invalidArgument.GetLocation()));
            }
        }
    }

    private ArgumentSyntax? GetArgument(FieldDeclarationSyntax fieldDeclaration)
    {
        var variables = fieldDeclaration.Declaration.Variables;

        var initializer = variables.Single().Initializer;
        if (initializer is null)
        {
            return null;
        }
        var arguments = (initializer.Value as BaseObjectCreationExpressionSyntax)?.ArgumentList?.Arguments ?? [];

        foreach (var argument in arguments)
        {
            if (argument.Expression is not CastExpressionSyntax expression)
            {
                continue;
            }
            if (expression.Type is not GenericNameSyntax expressionType)
            {
                continue;
            }
            var expressionIdentifier = expressionType.Identifier.Value;
            if (expressionIdentifier is null)
            {
                continue;
            }

            if (expressionIdentifier.ToString() != PropertyName)
            {
                continue;
            }

            return argument;
        }
        return null;
    }
}
