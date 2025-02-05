using Language.Experimental.Compiler;
using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Models;
using Language.Experimental.TypedExpressions;

namespace Language.Experimental.Expressions;


public class TypedCompilerIntrinsic_SetExpression : TypedExpression
{
    public TypedExpression ContextPointer { get; set; }
    public int AssignmentOffset { get; set; }
    public TypedExpression ValueToAssign { get; set; }
    public TypedCompilerIntrinsic_SetExpression(TypeInfo typeInfo, ExpressionBase originalExpression, TypedExpression contextPointer, int assignmentOffset, TypedExpression valueToAssign) : base(typeInfo, originalExpression)
    {
        ContextPointer = contextPointer;
        AssignmentOffset = assignmentOffset;
        ValueToAssign = valueToAssign;
    }

    public override void Compile(X86CompilationContext cc)
    {
        ContextPointer.Compile(cc);
        ValueToAssign.Compile(cc);
        cc.AddInstruction(X86Instructions.Pop(X86Register.eax));
        cc.AddInstruction(X86Instructions.Pop(X86Register.esi));
        var contextOffset = Offset.Create(X86Register.esi, AssignmentOffset);
        cc.AddInstruction(X86Instructions.Mov(contextOffset, X86Register.eax));
        cc.AddInstruction(X86Instructions.Push(X86Register.eax));
    }
}