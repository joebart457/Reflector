using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.Parser;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;
public class IdentifierExpression : ExpressionBase
{
    public IdentifierExpression(IToken token) : base(token)
    {
    }

    public override TypedExpression Resolve(TypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }

    public override ExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new IdentifierExpression(Token);
    }
}
