

using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

public class InlineAssemblyExpression : ExpressionBase
{
    public X86Instruction AssemblyInstruction { get; set; }
    public InlineAssemblyExpression(IToken token, X86Instruction assemblyInstruction) : base(token)
    {
        AssemblyInstruction = assemblyInstruction;
    }

    public override TypedExpression Resolve(TypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }
}
