using Language.Experimental.Expressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Statements;


public class ExpressionStatement : StatementBase
{
    public ExpressionBase Expression { get; set; }
    public ExpressionStatement(IToken token, ExpressionBase expression) : base(token)
    {
        Expression = expression;
    }
}