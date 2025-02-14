using Language.Experimental.Compiler;
using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Expressions;
using Language.Experimental.Models;
using TokenizerCore.Interfaces;

namespace Language.Experimental.TypedExpressions;
public class TypedLocalVariableExpression : TypedExpression
{
    public IToken Identifier { get; set; }
    public TypeInfo VariableType { get; set; }
    public TypedExpression? Initializer { get; set; }
    public TypedLocalVariableExpression(TypeInfo typeInfo, ExpressionBase originalExpression, IToken identifier, TypeInfo variableType, TypedExpression? initializer) : base(typeInfo, originalExpression)
    {
        Identifier = identifier;
        VariableType = variableType;
        Initializer = initializer;
    }

    public override void Compile(X86CompilationContext cc)
    {
        if (Initializer != null)
        {
            Initializer.Compile(cc);
            // Since all local variables are 4 bytes logic can be simple
            var offset = cc.GetIdentifierOffset(Identifier);
            cc.AddInstruction(X86Instructions.Pop(X86Register.eax));
            cc.AddInstruction(X86Instructions.Mov(offset, X86Register.eax));
        }
        
    }
}