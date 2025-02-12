using Language.Experimental.Interfaces;
using Language.Experimental.Parser;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

public class ReturnExpression : ExpressionBase
{
    public ExpressionBase? ReturnValue { get; set; }
    public ReturnExpression(IToken token, ExpressionBase? returnValue) : base(token)
    {
        ReturnValue = returnValue;
    }

    public override TypedExpression Resolve(ITypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }

    public override ExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new ReturnExpression(Token, ReturnValue?.ReplaceGenericTypeSymbols(genericToConcreteTypeMap));
    }
}