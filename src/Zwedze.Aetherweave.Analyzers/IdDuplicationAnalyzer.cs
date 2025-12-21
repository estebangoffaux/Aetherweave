using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Zwedze.Aetherweave.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class IdDuplicationAnalyzer : PropertyDuplicationAnalyzer<long>
{
    protected override string DiagnosticId => "IdDuplication";
    protected override string Title => "Ids are duplicated";
    protected override string MessageFormat => "Id is duplicated";
    protected override string Description => "Id is duplicated.";
    protected override string Category => "Id Validation";
    protected override string PropertyName => "Id";

    protected override long GroupByExpression(ArgumentSyntax argument)
    {
        var t = (argument.Expression as CastExpressionSyntax)?.Expression.GetFirstToken().ValueText ?? string.Empty;
        return long.Parse(t);
    }
}
