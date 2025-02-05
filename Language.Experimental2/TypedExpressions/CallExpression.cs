using TokenizerCore.Interfaces;

namespace Language.Experimental.TypedExpressions;

public class TypedCallExpression : TypedExpression
{
    public TypedExpression CallTarget { get; private set; }
    public List<TypedExpression> Arguments { get; private set; }
    public TypedCallExpression(IToken token, TypedExpression callTarget, List<TypedExpression> arguments) : base(token)
    {
        CallTarget = callTarget;
        Arguments = arguments;
    }
}