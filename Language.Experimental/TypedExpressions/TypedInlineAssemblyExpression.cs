using Language.Experimental.Compiler;
using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Expressions;
using Language.Experimental.Models;

namespace Language.Experimental.TypedExpressions;

public class TypedInlineAssemblyExpression : TypedExpression
{
    public X86Instruction AssemblyInstruction { get; set; }
    public TypedInlineAssemblyExpression(TypeInfo typeInfo, ExpressionBase originalExpression, X86Instruction assemblyInstruction) : base(typeInfo, originalExpression)
    {
        AssemblyInstruction = assemblyInstruction;
    }

    public override void Compile(X86CompilationContext cc)
    {
        cc.AddInstruction(AssemblyInstruction);
    }
}