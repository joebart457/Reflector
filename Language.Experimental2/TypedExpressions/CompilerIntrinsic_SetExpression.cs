using Language.Experimental.Models;
using Language.Experimental.TypedExpressions;

namespace Language.Experimental.Expressions;


public class TypedCompilerIntrinsic_SetExpression : TypedExpression
{
    public TypedIdentifierExpression ContextIdentifier { get; set; }
    public int AssignmentOffset { get; set; }
    public ExpressionBase ValueToAssign { get; set; }
    public TypedCompilerIntrinsic_SetExpression(TypeInfo typeInfo, ExpressionBase originalExpression, TypedIdentifierExpression contextIdentifier, int assignmentOffset, ExpressionBase valueToAssign) : base(typeInfo, originalExpression)
    {
        ContextIdentifier = contextIdentifier;
        AssignmentOffset = assignmentOffset;
        ValueToAssign = valueToAssign;
    }
}