using Language.Experimental.Compiler;
using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Expressions;
using Language.Experimental.Models;

namespace Language.Experimental.TypedExpressions;

public class TypedInlineAssemblyExpression : TypedExpression
{
    public string Assembly { get; set; }
    public TypedInlineAssemblyExpression(TypeInfo typeInfo, ExpressionBase originalExpression, string assembly) : base(typeInfo, originalExpression)
    {
        Assembly = assembly;
    }

    public override void Compile(X86CompilationContext cc)
    {
        cc.AddInstruction(X86Instructions.RawAsm(Assembly));
    }
}