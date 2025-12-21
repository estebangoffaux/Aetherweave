using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Zwedze.Aetherweave.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CodeDuplicationAnalyzer : PropertyDuplicationAnalyzer<string>
{
    protected override string DiagnosticId => "CodeDuplication";
    protected override string Title => "Codes are duplicated";
    protected override string MessageFormat => "Code is duplicated";
    protected override string Description => "Code is duplicated.";
    protected override string Category => "Code Validation";
    protected override string PropertyName => "Code";

    protected override string GroupByExpression(ArgumentSyntax argument)
    {
        return (argument.Expression as CastExpressionSyntax)?.Expression.GetFirstToken().ValueText ?? string.Empty;
    }
}
