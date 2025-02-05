using Language.Experimental.Compiler;
using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Expressions;
using Language.Experimental.Models;

namespace Language.Experimental.TypedExpressions;

internal class TypedCompilerIntrinsic_GetExpression : TypedExpression
{
    public TypedExpression ContextPointer { get; set; }
    public int MemberOffset { get; set; }
    public TypedCompilerIntrinsic_GetExpression(TypeInfo typeInfo, ExpressionBase originalExpression, TypedExpression contextPointer, int memberOffset) : base(typeInfo, originalExpression)
    {
        ContextPointer = contextPointer;
        MemberOffset = memberOffset;
    }

    public override void Compile(X86CompilationContext cc)
    {
        ContextPointer.Compile(cc);
        cc.AddInstruction(X86Instructions.Pop(X86Register.esi));
        var contextOffset = Offset.Create(X86Register.esi, MemberOffset);
        cc.AddInstruction(X86Instructions.Push(contextOffset));
    }
}