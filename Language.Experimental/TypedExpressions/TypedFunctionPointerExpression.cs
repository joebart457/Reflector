using Language.Experimental.Compiler;
using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Expressions;
using Language.Experimental.Models;
using Language.Experimental.TypedStatements;

namespace Language.Experimental.TypedExpressions;

public class TypedFunctionPointerExpression : TypedExpression
{
    public ITypedFunctionInfo FunctionInfo { get; private set; }
    public TypedFunctionPointerExpression(TypeInfo typeInfo, ExpressionBase originalExpression, ITypedFunctionInfo functionInfo) : base(typeInfo, originalExpression)
    {
        FunctionInfo = functionInfo;

    }

    public override void Compile(X86CompilationContext cc)
    {
        if (FunctionInfo.IsImported)
            cc.AddInstruction(X86Instructions.Push(Offset.CreateSymbolOffset(FunctionInfo.GetDecoratedFunctionIdentifier(), 0)));
        else cc.AddInstruction(X86Instructions.Push(FunctionInfo.GetDecoratedFunctionIdentifier()));
    }
}