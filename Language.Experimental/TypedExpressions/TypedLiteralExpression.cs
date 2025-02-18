
using Language.Experimental.Compiler;
using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Expressions;
using Language.Experimental.Models;


namespace Language.Experimental.TypedExpressions;


public class TypedLiteralExpression : TypedExpression
{
    public object? Value { get; private set; }
    public TypedLiteralExpression(TypeInfo typeInfo, ExpressionBase originalExpression, object? value): base(typeInfo, originalExpression)
    {
        Value = value;
    }

    public override void Compile(X86CompilationContext cc)
    {
        if (Value is string str)
        {
            var label = cc.AddStringData(str);
            cc.AddInstruction(X86Instructions.Push(label));
        }
        else if (Value == null)
        {
            cc.AddInstruction(X86Instructions.Push(0));
        }
        else if (Value is int i)
        {
            cc.AddInstruction(X86Instructions.Push(i));
        } 
        else if (Value is float f)
        {
            var label = cc.AddSinglePrecisionFloatingPointData(f);
            cc.AddInstruction(X86Instructions.Push(Offset.CreateSymbolOffset(label, 0)));
        }
        else
        {
            throw new NotImplementedException($"literals are not implemented for type {Value.GetType()}");
        }
    }

}