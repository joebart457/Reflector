using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.Parser;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

public class CastExpression : ExpressionBase
{
    public TypeSymbol TypeSymbol { get; private set; }
    public ExpressionBase Expression { get; private set; }
    public CastExpression(IToken token, TypeSymbol typeSymbol, ExpressionBase expression) : base(token)
    {
        TypeSymbol = typeSymbol;
        Expression = expression;
    }

    public override TypedExpression Resolve(TypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }

    public override ExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new CastExpression(Token, TypeSymbol.ReplaceGenericTypeParameter(genericToConcreteTypeMap), Expression.ReplaceGenericTypeSymbols(genericToConcreteTypeMap));
    }
}