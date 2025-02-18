using Language.Experimental.Interfaces;
using Language.Experimental.Parser;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;


public class LiteralExpression : ExpressionBase
{
    public object? Value { get; private set; }
    public LiteralExpression(IToken token, object? value): base(token)
    {
        Value = value;
    }

    public override TypedExpression Resolve(ITypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }

    public override ExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new LiteralExpression(Token, Value).CopyStartAndEndTokens(this);
    }
}