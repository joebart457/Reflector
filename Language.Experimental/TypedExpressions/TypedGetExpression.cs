using Language.Experimental.Compiler;
using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Expressions;
using Language.Experimental.Models;
using TokenizerCore.Interfaces;
using TokenizerCore.Model;

namespace Language.Experimental.TypedExpressions;

public class TypedGetExpression : TypedExpression
{
    public TypedExpression Instance { get; private set; }
    public IToken TargetField { get; private set; }
    public bool ShortCircuitOnNull { get; private set; }    
    public TypedGetExpression(TypeInfo typeInfo, ExpressionBase originalExpression, TypedExpression instance, IToken targetField,  bool shortCircuitOnNull): base(typeInfo, originalExpression)
    {
        Instance = instance;
        TargetField = targetField;
        ShortCircuitOnNull = shortCircuitOnNull;
    }

    public override void Compile(X86CompilationContext cc)
    {
        var offset = Instance.TypeInfo.GetFieldOffset(TargetField);
        Instance.Compile(cc);
        cc.AddInstruction(X86Instructions.Pop(X86Register.esi));
        var fieldOffset = Offset.Create(X86Register.esi, offset);
        cc.AddInstruction(X86Instructions.Push(fieldOffset));
    }

    public RegisterOffset CompileAndReturnMemoryOffset(X86CompilationContext cc)
    {
        var offset = Instance.TypeInfo.GetFieldOffset(TargetField);
        Instance.Compile(cc);
        cc.AddInstruction(X86Instructions.Pop(X86Register.esi));
        var fieldOffset = Offset.Create(X86Register.esi, offset);
        return fieldOffset;
    }
}