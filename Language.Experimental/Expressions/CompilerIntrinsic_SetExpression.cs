using Language.Experimental.Compiler.TypeResolver;
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

    public override TypedExpression Resolve(TypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }
}