using Language.Experimental.Expressions;
using Language.Experimental.Parser;
using Language.Experimental.UnresolvedStatements;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedExpressions;

public class UnresolvedLambdaExpression : UnresolvedExpressionBase
{
    public UnresolvedFunctionDefinition FunctionDefinition { get; private set; }
    public UnresolvedLambdaExpression(IToken token, UnresolvedFunctionDefinition functionDefinition) : base(token)
    {
        FunctionDefinition = functionDefinition;
    }

    public override ExpressionBase Resolve(TypeSymbolResolver typeSymbolResolver)
    {
        return typeSymbolResolver.Resolve(this);
    }

    public override UnresolvedExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new UnresolvedLambdaExpression(Token, FunctionDefinition.ReplaceGenericTypeSymbols(genericToConcreteTypeMap));
    }
}