
using Language.Experimental.Models;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.TypedExpressions;
public class TypedIdentifierExpression : TypedExpression
{
    public IToken Token { get; set; }
    public TypedIdentifierExpression(TypeInfo typeInfo, ExpressionBase originalExpression, IToken identifier) : base(typeInfo, originalExpression)
    {
        Token = identifier;
    }

}
