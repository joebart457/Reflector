using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.Expressions;
using Language.Experimental.Parser;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedExpressions;

public abstract class UnresolvedExpressionBase
{
    public IToken Token { get; private set; }

    protected UnresolvedExpressionBase(IToken token)
    {
        Token = token;
    }

    public abstract ExpressionBase Resolve(TypeSymbolResolver typeSymbolResolver);
    public abstract UnresolvedExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap);
}