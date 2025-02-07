using Language.Experimental.Compiler;
using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Expressions;
using Language.Experimental.Models;
using TokenizerCore.Interfaces;

namespace Language.Experimental.TypedExpressions;

public class TypedFunctionPointerExpression : TypedExpression
{
    public IToken FunctionSymbol { get; private set; }
    public bool IsImportedFunction { get; private set; }
    public TypedFunctionPointerExpression(TypeInfo typeInfo, ExpressionBase originalExpression, IToken functionSymbol, bool isImportedFunction) : base(typeInfo, originalExpression)
    {
        FunctionSymbol = functionSymbol;
        IsImportedFunction = isImportedFunction;
    }

    public override void Compile(X86CompilationContext cc)
    {
        if (IsImportedFunction)
            cc.AddInstruction(X86Instructions.Push(Offset.CreateSymbolOffset(FunctionSymbol.Lexeme, 0)));
        else cc.AddInstruction(X86Instructions.Push(FunctionSymbol.Lexeme));
    }
}