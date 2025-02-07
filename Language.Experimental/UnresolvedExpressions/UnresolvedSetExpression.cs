using Language.Experimental.Expressions;
using Language.Experimental.Parser;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedExpressions;


public class UnresolvedSetExpression : UnresolvedExpressionBase
{
    public UnresolvedExpressionBase AssignmentTarget { get; set; }
    public UnresolvedExpressionBase ValueToAssign { get; set; }
    public UnresolvedSetExpression(IToken token, UnresolvedExpressionBase assignmentTarget, UnresolvedExpressionBase valueToAssign) : base(token)
    {
        AssignmentTarget = assignmentTarget;
        ValueToAssign = valueToAssign;
    }

    public override ExpressionBase Resolve(TypeSymbolResolver typeSymbolResolver)
    {
        return typeSymbolResolver.Resolve(this);
    }

    public override UnresolvedExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new UnresolvedSetExpression(Token, AssignmentTarget.ReplaceGenericTypeSymbols(genericToConcreteTypeMap), ValueToAssign.ReplaceGenericTypeSymbols(genericToConcreteTypeMap));
    }
}