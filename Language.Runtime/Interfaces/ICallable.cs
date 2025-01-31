using TokenizerCore.Interfaces;
using TokenizerCore.Model;
using TokenizerCore.Models.Constants;

namespace Language.Runtime.Interfaces;

public class Parameter
{
    public IToken Symbol { get; set; }
    public Type TypeInfo { get; set; }

    public Parameter(IToken symbol, Type typeInfo)
    {
        Symbol = symbol;
        TypeInfo = typeInfo;
    }

    public Parameter(string symbol, Type typeInfo)
    {
        Symbol = new Token(BuiltinTokenTypes.Word, symbol, -1, -1);
        TypeInfo = typeInfo;
    }

}

public interface ICallable
{
    public string FullName { get; }
    public List<Parameter> Parameters { get; }
    public object? Invoke(RuntimeContext context, List<object?> args);
}
