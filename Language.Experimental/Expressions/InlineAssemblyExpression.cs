

using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

public class InlineAssemblyExpression : ExpressionBase
{
    public string Assembly { get; set; }
    public InlineAssemblyExpression(IToken token, string assembly) : base(token)
    {
        Assembly = assembly;
    }

    public override TypedExpression Resolve(TypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }
}
