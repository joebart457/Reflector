using Language.Experimental.Compiler;
using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Constants;
using Language.Experimental.Expressions;
using Language.Experimental.Models;
using Language.Experimental.TypedExpressions;
using System.Runtime.InteropServices;
using TokenizerCore.Interfaces;

namespace Language.Experimental.TypedStatements;


public class TypedFunctionDefinition: TypedStatement, ITypedFunctionInfo
{
    public IToken FunctionName { get; set; }
    public TypeInfo ReturnType { get; set; }
    public List<TypedParameter> Parameters { get; set; }
    public List<TypedExpression> BodyStatements { get; set; }
    public CallingConvention CallingConvention { get; set; }
    public bool IsExported { get; set; }
    public IToken ExportedSymbol { get; set; }
    public bool IsImported => false;
    public IToken FunctionSymbol => ExportedSymbol;

    public TypedFunctionDefinition(IToken functionName, TypeInfo returnType, List<TypedParameter> parameters, List<TypedExpression> bodyStatements)
    {
        FunctionName = functionName;
        ReturnType = returnType;
        Parameters = parameters;
        BodyStatements = bodyStatements;
        CallingConvention = CallingConvention.StdCall;
        IsExported = false;
        ExportedSymbol = functionName;
    }


    public TypedFunctionDefinition(IToken functionName, TypeInfo returnType, List<TypedParameter> parameters, List<TypedExpression> bodyStatements, CallingConvention callingConvention, bool isExported, IToken exportedSymbol)
    {
        FunctionName = functionName;
        ReturnType = returnType;
        Parameters = parameters;
        BodyStatements = bodyStatements;
        CallingConvention = callingConvention;
        IsExported = isExported;
        ExportedSymbol = exportedSymbol;
    }


    public IEnumerable<TypedLocalVariableExpression> ExtractLocalVariableExpressions()
    {
        return BodyStatements.SelectMany(e => ExtractLocalVariableExpressionsHelper(e));
    }

    private List<TypedLocalVariableExpression> ExtractLocalVariableExpressionsHelper(TypedExpression expression)
    {
        var ls = new List<TypedLocalVariableExpression>();
        if (expression is TypedCallExpression ce)
        {
            ls.AddRange(ExtractLocalVariableExpressionsHelper(ce.CallTarget));
            foreach (var arg in ce.Arguments) ls.AddRange(ExtractLocalVariableExpressionsHelper(arg));
        }
        else if (expression is TypedCompilerIntrinsic_GetExpression ci_get) ls.AddRange(ExtractLocalVariableExpressionsHelper(ci_get.ContextPointer));
        else if (expression is TypedCompilerIntrinsic_SetExpression ci_set) ls.AddRange(ExtractLocalVariableExpressionsHelper(ci_set.ContextPointer));
        else if (expression is TypedGetExpression get)
        {
            ls.AddRange(ExtractLocalVariableExpressionsHelper(get.Instance));
        }
        else if (expression is TypedIdentifierExpression id) { }
        else if (expression is TypedInlineAssemblyExpression asm) { }
        else if (expression is TypedLiteralExpression le) { }
        else if (expression is TypedFunctionPointerExpression fpe) { }
        else if (expression is TypedLocalVariableExpression lve) ls.Add(lve);
        else if (expression is TypedReturnExpression tre)
        { 
            if (tre.ReturnValue != null) ls.AddRange(ExtractLocalVariableExpressionsHelper(tre.ReturnValue)); 
        }
        else if (expression is TypedDirectCallExpression tdce) foreach (var arg in tdce.Arguments) ls.AddRange(ExtractLocalVariableExpressionsHelper(arg));
        else throw new InvalidOperationException($"unsupported expression type {expression.GetType().Name}");
        return ls;
    }

    public string GetDecoratedFunctionIdentifier()
    {
        if (CallingConvention == CallingConvention.Cdecl) return $"_{FunctionName.Lexeme}";
        if (CallingConvention == CallingConvention.StdCall) return $"_{FunctionName.Lexeme}@{Parameters.Count * 4}";
        throw new NotImplementedException($"No compiler support for calling convention {CallingConvention}");
    }

    public override void Compile(X86CompilationContext cc)
    {
        cc.EnterFunction(this);
        cc.AddInstruction(X86Instructions.Label(GetDecoratedFunctionIdentifier()));
        cc.AddInstruction(X86Instructions.Push(X86Register.ebp));
        cc.AddInstruction(X86Instructions.Mov(X86Register.ebp, X86Register.esp));
        cc.AddInstruction(X86Instructions.Sub(X86Register.esp, cc.CurrentFunction.LocalVariables.Sum(x => x.TypeInfo.StackSize()) + 4)); // +4 for stack frame

        foreach (var statement in BodyStatements)
        {
            statement.Compile(cc);
            if (!statement.TypeInfo.Is(IntrinsicType.Void)) 
                cc.AddInstruction(X86Instructions.Pop(X86Register.eax)); // Discard unused result of expression statement
        }

        cc.ExitFunction();
    }

    public FunctionPtrTypeInfo GetFunctionPointerType()
    {
        var intrinsicType = IntrinsicType.Func;
        if (CallingConvention == CallingConvention.Cdecl) intrinsicType = IntrinsicType.CFunc;
        var typeArguments = Parameters.Select(x => x.TypeInfo).ToList();
        typeArguments.Add(ReturnType);
        return new FunctionPtrTypeInfo(intrinsicType, typeArguments);
    }
}