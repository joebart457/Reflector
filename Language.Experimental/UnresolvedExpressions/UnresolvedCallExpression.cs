using Language.Experimental.Expressions;
using Language.Experimental.Parser;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedExpressions;

public class UnresolvedCallExpression : UnresolvedExpressionBase
{
    public UnresolvedExpressionBase CallTarget { get; private set; }
    public List<UnresolvedExpressionBase> Arguments { get; private set; }
    public UnresolvedCallExpression(IToken token, UnresolvedExpressionBase callTarget, List<UnresolvedExpressionBase> arguments) : base(token)
    {
        CallTarget = callTarget;
        Arguments = arguments;
    }

    public override ExpressionBase Resolve(TypeSymbolResolver typeSymbolResolver)
    {
        return typeSymbolResolver.Resolve(this);
    }

    public override UnresolvedExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new UnresolvedCallExpression(Token, CallTarget.ReplaceGenericTypeSymbols(genericToConcreteTypeMap), Arguments.Select(x => x.ReplaceGenericTypeSymbols(genericToConcreteTypeMap)).ToList());
    }
}