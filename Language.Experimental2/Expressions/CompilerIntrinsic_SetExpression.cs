using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;


public class CompilerIntrinsic_SetExpression : ExpressionBase
{
    public IdentifierExpression ContextIdentifier { get; set; }
    public int AssignmentOffset { get; set; }
    public ExpressionBase ValueToAssign { get; set; }
    public CompilerIntrinsic_SetExpression(IToken token, IdentifierExpression contextIdentifier, int assignmentOffset, ExpressionBase valueToAssign) : base(token)
    {
        ContextIdentifier = contextIdentifier;
        AssignmentOffset = assignmentOffset;
        ValueToAssign = valueToAssign;
    }
}