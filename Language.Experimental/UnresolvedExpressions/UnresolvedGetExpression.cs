using Language.Experimental.Expressions;
using Language.Experimental.Parser;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedExpressions;

public class UnresolvedGetExpression : UnresolvedExpressionBase
{
    public UnresolvedExpressionBase Instance { get; private set; }
    public IToken TargetField { get; private set; }
    public bool ShortCircuitOnNull { get; private set; }    
    public UnresolvedGetExpression(IToken token, UnresolvedExpressionBase instance, IToken targetField,  bool shortCircuitOnNull): base(token)
    {
        Instance = instance;
        TargetField = targetField;
        ShortCircuitOnNull = shortCircuitOnNull;
    }

    public override ExpressionBase Resolve(TypeSymbolResolver typeSymbolResolver)
    {
        return typeSymbolResolver.Resolve(this);
    }

    public override UnresolvedExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new UnresolvedGetExpression(Token, Instance.ReplaceGenericTypeSymbols(genericToConcreteTypeMap), TargetField, ShortCircuitOnNull);
    }
}