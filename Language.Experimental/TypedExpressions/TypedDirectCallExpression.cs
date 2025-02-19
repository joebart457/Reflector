using Language.Experimental.Compiler;
using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Constants;
using Language.Experimental.Expressions;
using Language.Experimental.Models;
using Language.Experimental.TypedStatements;
using System.Runtime.InteropServices;
using TokenizerCore.Interfaces;

namespace Language.Experimental.TypedExpressions;

public class TypedDirectCallExpression : TypedExpression
{
    public IToken OriginalToken { get; private set; }
    public ITypedFunctionInfo CallTarget { get; private set; }
    public List<TypedExpression> Arguments { get; private set; }
    public TypedDirectCallExpression(TypeInfo typeInfo, ExpressionBase originalExpression, IToken originalToken, ITypedFunctionInfo callTarget, List<TypedExpression> arguments) : base(typeInfo, originalExpression)
    {
        OriginalToken = originalToken;
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
            cc.AddInstruction(X86Instructions.Call(CallTarget.GetDecoratedFunctionIdentifier(), true));
        }else
        {
            cc.AddInstruction(X86Instructions.Call(CallTarget.GetDecoratedFunctionIdentifier(), false));
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

    public override bool TryGetContainingExpression(int line, int column, out TypedExpression? containingExpression)
    {
        foreach (var argument in Arguments)
        {
            if (argument.TryGetContainingExpression(line, column, out containingExpression)) return true;
        }
        if (Contains(OriginalToken, line, column))
        {
            containingExpression = new TypedIdentifierExpression(CallTarget.GetFunctionPointerType(), new IdentifierExpression(OriginalToken), OriginalToken);
            return true;
        }
        return base.TryGetContainingExpression(line, column, out containingExpression);
    }

    private bool Contains(IToken token, int line, int column)
    {
        if (line == token.Start.Line && line == token.End.Line) return token.Start.Column <= column && column <= token.End.Column;
        if (line == token.Start.Line) return token.Start.Column <= column;
        if (line == token.End.Line) return token.End.Column >= column;
        return token.Start.Line <= line && token.End.Line >= line;
    }
}