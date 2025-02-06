using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.Statements;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

public class LambdaExpression : ExpressionBase
{
    public FunctionDefinition FunctionDefinition { get; private set; }
    public LambdaExpression(IToken token, FunctionDefinition functionDefinition) : base(token)
    {
        FunctionDefinition = functionDefinition;
    }

    public override TypedExpression Resolve(TypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }
}