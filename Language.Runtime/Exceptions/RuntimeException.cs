using TokenizerCore.Interfaces;

namespace Language.Runtime.Exceptions;


public class RuntimeException : System.Exception
{
    public IToken? Token { get; private set; }

    public RuntimeException(IToken? token, string message) :
        base(message)
    {
        Token = token;
    }

    public RuntimeException(IToken? token, string message, Exception innerException) :
        base(message, innerException)
    {
        Token = token;
    }

    public string What()
    {
        return $"{Token}: {Message}";
    }
}