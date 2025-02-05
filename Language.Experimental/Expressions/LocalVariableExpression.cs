using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.Models;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

public class LocalVariableExpression : ExpressionBase
{
    public TypeInfo TypeInfo { get; set; }
    public IToken Identifier { get; set; }
    public ExpressionBase? Initializer { get; set; }
    public LocalVariableExpression(IToken token, TypeInfo typeInfo, IToken identifier, ExpressionBase? initializer) : base(token)
    {
        TypeInfo = typeInfo;
        Identifier = identifier;
        Initializer = initializer;
    }

    public override TypedExpression Resolve(TypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }
}