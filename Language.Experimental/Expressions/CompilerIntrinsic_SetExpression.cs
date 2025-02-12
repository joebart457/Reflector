using Language.Experimental.Interfaces;
using Language.Experimental.Parser;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;


public class CompilerIntrinsic_SetExpression : ExpressionBase
{
    public ExpressionBase ContextPointer { get; set; }
    public int AssignmentOffset { get; set; }
    public ExpressionBase ValueToAssign { get; set; }
    public CompilerIntrinsic_SetExpression(IToken token, ExpressionBase contextPointer, int assignmentOffset, ExpressionBase valueToAssign) : base(token)
    {
        ContextPointer = contextPointer;
        AssignmentOffset = assignmentOffset;
        ValueToAssign = valueToAssign;
    }

    public override TypedExpression Resolve(ITypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }

    public override ExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new CompilerIntrinsic_SetExpression(Token, ContextPointer.ReplaceGenericTypeSymbols(genericToConcreteTypeMap), AssignmentOffset, ValueToAssign.ReplaceGenericTypeSymbols(genericToConcreteTypeMap));
    }
}