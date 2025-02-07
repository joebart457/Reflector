using Language.Experimental.Expressions;
using Language.Experimental.Parser;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedExpressions;


public class UnresolvedCompilerIntrinsic_SetExpression : UnresolvedExpressionBase
{
    public UnresolvedExpressionBase ContextPointer { get; set; }
    public int AssignmentOffset { get; set; }
    public UnresolvedExpressionBase ValueToAssign { get; set; }
    public UnresolvedCompilerIntrinsic_SetExpression(IToken token, UnresolvedExpressionBase contextPointer, int assignmentOffset, UnresolvedExpressionBase valueToAssign) : base(token)
    {
        ContextPointer = contextPointer;
        AssignmentOffset = assignmentOffset;
        ValueToAssign = valueToAssign;
    }

    public override ExpressionBase Resolve(TypeSymbolResolver typeSymbolResolver)
    {
        return typeSymbolResolver.Resolve(this);
    }

    public override UnresolvedExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new UnresolvedCompilerIntrinsic_SetExpression(Token, ContextPointer.ReplaceGenericTypeSymbols(genericToConcreteTypeMap), AssignmentOffset, ValueToAssign.ReplaceGenericTypeSymbols(genericToConcreteTypeMap));
    }
}