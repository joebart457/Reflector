using Language.Experimental.Compiler;
using Language.Experimental.Expressions;
using Language.Experimental.Models;
using TokenizerCore.Interfaces;

namespace Language.Experimental.TypedExpressions;
public class TypedLocalVariableExpression : TypedExpression
{
    public IToken Identifier { get; set; }
    public TypedExpression? Initializer { get; set; }
    public TypedLocalVariableExpression(TypeInfo typeInfo, ExpressionBase originalExpression, IToken identifier, TypedExpression? initializer) : base(typeInfo, originalExpression)
    {
        Identifier = identifier;
        Initializer = initializer;
    }

    public override void Compile(X86CompilationContext cc)
    {
        if (Initializer != null)
        {
            Initializer.Compile(cc);
        }
    }
}