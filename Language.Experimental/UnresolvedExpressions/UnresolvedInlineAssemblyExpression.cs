using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Expressions;
using Language.Experimental.Parser;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedExpressions;

public class UnresolvedInlineAssemblyExpression : UnresolvedExpressionBase
{
    public X86Instruction AssemblyInstruction { get; set; }
    public UnresolvedInlineAssemblyExpression(IToken token, X86Instruction assemblyInstruction) : base(token)
    {
        AssemblyInstruction = assemblyInstruction;
    }

    public override ExpressionBase Resolve(TypeSymbolResolver typeSymbolResolver)
    {
        return typeSymbolResolver.Resolve(this);
    }

    public override UnresolvedExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new UnresolvedInlineAssemblyExpression(Token, AssemblyInstruction);
    }
}
