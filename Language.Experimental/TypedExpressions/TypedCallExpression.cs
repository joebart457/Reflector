using Language.Experimental.Compiler;
using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Constants;
using Language.Experimental.Expressions;
using Language.Experimental.Models;
using System.Runtime.InteropServices;

namespace Language.Experimental.TypedExpressions;

public class TypedCallExpression : TypedExpression
{
    public TypedExpression CallTarget { get; private set; }
    public List<TypedExpression> Arguments { get; private set; }
    public TypedCallExpression(TypeInfo typeInfo, ExpressionBase originalExpression, TypedExpression callTarget, List<TypedExpression> arguments) : base(typeInfo, originalExpression)
    {
        CallTarget = callTarget;
        Arguments = arguments;
    }

    public override void Compile(X86CompilationContext cc)
    {

        if (!CallTarget.TypeInfo.IsFunctionPtr) throw new InvalidOperationException($"expect call to type fnptr<t> but got {CallTarget.TypeInfo}");
        bool returnsFloat = CallTarget.TypeInfo.FunctionReturnType.Is(IntrinsicType.Float);
        var callingConvention = CallTarget.TypeInfo.CallingConvention;
        for (int i = Arguments.Count - 1; i >= 0; i--)
        {
            Arguments[i].Compile(cc);
        }

        if (CallTarget is TypedIdentifierExpression idExpr)
        {
            var offset = cc.GetIdentifierOffset(idExpr.Token);
            cc.AddInstruction(X86Instructions.Call(offset));
        } else
        {
            CallTarget.Compile(cc);
            cc.AddInstruction(X86Instructions.Pop(X86Register.eax));
            cc.AddInstruction(X86Instructions.Call(X86Register.eax.ToString(), false));
        }

        if (callingConvention == CallingConvention.Cdecl) cc.AddInstruction(X86Instructions.Add(X86Register.esp, Arguments.Count * 4));
        else if (callingConvention != CallingConvention.StdCall) throw new InvalidOperationException($"Unsupported calling convention {callingConvention}");
        if (returnsFloat)
        {
            cc.AddInstruction(X86Instructions.Sub(X86Register.esp, 4));
            cc.AddInstruction(X86Instructions.Fstp(Offset.Create(X86Register.esp, 0)));
        }
        else if (!CallTarget.TypeInfo.FunctionReturnType.Is(IntrinsicType.Void)) cc.AddInstruction(X86Instructions.Push(X86Register.eax));
    }
}