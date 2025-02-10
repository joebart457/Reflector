using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.Parser;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

public abstract class ExpressionBase
{
    public IToken Token { get; private set; }

    protected ExpressionBase(IToken token)
    {
        Token = token;
    }

    public abstract TypedExpression Resolve(TypeResolver typeResolver);
    public abstract ExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap);

}