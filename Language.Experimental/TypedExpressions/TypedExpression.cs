using Language.Experimental.Compiler;
using Language.Experimental.Expressions;
using Language.Experimental.Models;

namespace Language.Experimental.TypedExpressions;


public abstract class TypedExpression
{
    public TypeInfo TypeInfo { get; set; }
    public ExpressionBase OriginalExpression { get; set; }

    public TypedExpression(TypeInfo typeInfo, ExpressionBase originalExpression)
    {
        TypeInfo = typeInfo;
        OriginalExpression = originalExpression;
    }

    public abstract void Compile(X86CompilationContext cc);
}