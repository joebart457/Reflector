using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;


public class SetExpression : ExpressionBase
{
    public ExpressionBase AssignmentTarget { get; set; }
    public ExpressionBase ValueToAssign { get; set; }
    public SetExpression(IToken token, ExpressionBase assignmentTarget, ExpressionBase valueToAssign) : base(token)
    {
        AssignmentTarget = assignmentTarget;
        ValueToAssign = valueToAssign;
    }

    public override TypedExpression Resolve(TypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }
}