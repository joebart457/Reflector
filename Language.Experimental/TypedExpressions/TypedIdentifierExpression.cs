
using Language.Experimental.Compiler;
using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Expressions;
using Language.Experimental.Models;
using TokenizerCore.Interfaces;

namespace Language.Experimental.TypedExpressions;
public class TypedIdentifierExpression : TypedExpression
{
    public IToken Token { get; set; }
    public TypedIdentifierExpression(TypeInfo typeInfo, ExpressionBase originalExpression, IToken identifier) : base(typeInfo, originalExpression)
    {
        Token = identifier;
    }

    public override void Compile(X86CompilationContext cc)
    {
        var offset = cc.GetIdentifierOffset(Token, out bool isfunctionParameter);
        cc.AddInstruction(X86Instructions.Push(offset));
    }

    public RegisterOffset GetMemoryOffset(X86CompilationContext cc) => cc.GetIdentifierOffset(Token);
}
