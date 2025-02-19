using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Extensions;
using Language.Experimental.TypedExpressions;
using Language.Experimental.TypedStatements;
using System.Runtime.InteropServices;
using System.Text;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Compiler.Models;


public class X86Function
{
    public TypedFunctionDefinition OriginalDeclaration { get; set; }
    public List<TypedLocalVariableExpression> LocalVariables { get; set; }
    public List<X86Instruction> Instructions { get; set; } = new();
    public CallingConvention CallingConvention => OriginalDeclaration.CallingConvention;
    public List<TypedParameter> Parameters => OriginalDeclaration.Parameters;
    public bool IsExported => OriginalDeclaration.IsExported;
    public IToken ExportedSymbol => OriginalDeclaration.ExportedSymbol;
    public X86Function(TypedFunctionDefinition originalDeclaration)
    {
        OriginalDeclaration = originalDeclaration;
        LocalVariables = originalDeclaration.ExtractLocalVariableExpressions().ToList();
    }

    public void AddInstruction(X86Instruction instruction)
    {
        Instructions.Add(instruction);
    }

    public string Emit(int indentLevel)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{OriginalDeclaration.GetDecoratedFunctionIdentifier()}:".Indent(indentLevel + 1));
        foreach (var instruction in Instructions)
        {
            sb.AppendLine(instruction.Emit().Indent(indentLevel + 2));
        }
        return sb.ToString();
    }
}