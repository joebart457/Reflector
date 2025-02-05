using Language.Experimental.Compiler;
using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Constants;
using Language.Experimental.Expressions;
using Language.Experimental.Models;
using System.Runtime.InteropServices;

namespace Language.Experimental.TypedExpressions;


public class TypedReturnExpression : TypedExpression
{
    public TypedExpression? ReturnValue { get; set; }
    public TypedReturnExpression(TypeInfo typeInfo, ExpressionBase originalExpression, TypedExpression? returnValue) : base(typeInfo, originalExpression)
    {
        ReturnValue = returnValue;
    }

    public override void Compile(X86CompilationContext cc)
    {
        if (ReturnValue != null)
        {
            ReturnValue.Compile(cc);

            if (ReturnValue.TypeInfo.Is(IntrinsicType.Float))
            {
                cc.AddInstruction(X86Instructions.Fld(Offset.Create(X86Register.esp, 0)));
                cc.AddInstruction(X86Instructions.Add(X86Register.esp, 4));
            }
            else
            {
                cc.AddInstruction(X86Instructions.Pop(X86Register.eax));
            }
        }      

        cc.AddInstruction(X86Instructions.Mov(X86Register.esp, X86Register.ebp));
        cc.AddInstruction(X86Instructions.Pop(X86Register.ebp));
        if (cc.CurrentFunction.CallingConvention == CallingConvention.Cdecl) cc.AddInstruction(X86Instructions.Ret());
        else if (cc.CurrentFunction.CallingConvention == CallingConvention.StdCall)
        {
            var parameterCount = cc.CurrentFunction.Parameters.Count;
            if (parameterCount == 0) cc.AddInstruction(X86Instructions.Ret());
            else cc.AddInstruction(X86Instructions.Ret(parameterCount * 4));
        }
        else throw new NotImplementedException($"support for calling convention {cc.CurrentFunction.CallingConvention} has not been implemented");
    }
}