using Language.Experimental.Compiler;
using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Constants;
using Language.Experimental.Expressions;
using Language.Experimental.Models;
using Language.Experimental.TypedStatements;
using System.Runtime.InteropServices;

namespace Language.Experimental.TypedExpressions;

public class TypedDirectCallExpression : TypedExpression
{
    public ITypedFunctionInfo CallTarget { get; private set; }
    public List<TypedExpression> Arguments { get; private set; }
    public TypedDirectCallExpression(TypeInfo typeInfo, ExpressionBase originalExpression, ITypedFunctionInfo callTarget, List<TypedExpression> arguments) : base(typeInfo, originalExpression)
    {
        CallTarget = callTarget;
        Arguments = arguments;
    }

    public override void Compile(X86CompilationContext cc)
    {

        bool returnsFloat = CallTarget.ReturnType.Is(IntrinsicType.Float);
        for (int i = Arguments.Count - 1; i >= 0; i--)
        {
            Arguments[i].Compile(cc);
        }
        if (CallTarget.IsImported)
        {
            cc.AddInstruction(X86Instructions.Call(CallTarget.FunctionName.Lexeme, true));
        }else
        {
            cc.AddInstruction(X86Instructions.Call(CallTarget.FunctionName.Lexeme, false));
        }

        if (CallTarget.CallingConvention == CallingConvention.Cdecl) cc.AddInstruction(X86Instructions.Add(X86Register.esp, Arguments.Count * 4));
        else if (CallTarget.CallingConvention != CallingConvention.StdCall) throw new InvalidOperationException($"Unsupported calling convention {CallTarget.CallingConvention}");
        if (returnsFloat)
        {
            cc.AddInstruction(X86Instructions.Sub(X86Register.esp, 4));
            cc.AddInstruction(X86Instructions.Fstp(Offset.Create(X86Register.esp, 0)));
        }
        else if (!CallTarget.ReturnType.Is(IntrinsicType.Void)) cc.AddInstruction(X86Instructions.Push(X86Register.eax));
    }
}