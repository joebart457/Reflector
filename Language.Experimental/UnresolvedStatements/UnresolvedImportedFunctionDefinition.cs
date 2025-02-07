using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.Models;
using Language.Experimental.Parser;
using Language.Experimental.Statements;
using Language.Experimental.TypedStatements;
using System.Runtime.InteropServices;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedStatements;

public class UnresolvedImportedFunctionDefinition : UnresolvedStatementBase
{
    public IToken FunctionName { get; set; }
    public TypeSymbol ReturnType { get; set; }
    public List<UnresolvedParameter> Parameters { get; set; }
    public CallingConvention CallingConvention { get; set; }
    public IToken LibraryAlias { get; set; }
    public IToken FunctionSymbol { get; set; }
    public UnresolvedImportedFunctionDefinition(IToken functionName, TypeSymbol returnType, List<UnresolvedParameter> parameters, CallingConvention callingConvention, IToken libraryAlias, IToken functionSymbol) : base(functionName)
    {
        FunctionName = functionName;
        ReturnType = returnType;
        Parameters = parameters;
        CallingConvention = callingConvention;
        LibraryAlias = libraryAlias;
        FunctionSymbol = functionSymbol;
    }


    public string GetDecoratedFunctionIdentifier()
    {
        if (CallingConvention == CallingConvention.Cdecl) return $"_{FunctionName.Lexeme}";
        if (CallingConvention == CallingConvention.StdCall) return $"_{FunctionName.Lexeme}@{Parameters.Count * 4}";
        throw new NotImplementedException();
    }

    public override StatementBase Resolve(TypeSymbolResolver typeSymbolResolver)
    {
        return typeSymbolResolver.Resolve(this);
    }
}