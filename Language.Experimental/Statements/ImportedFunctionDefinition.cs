using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.Models;
using Language.Experimental.Parser;
using Language.Experimental.TypedStatements;
using System.Runtime.InteropServices;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Statements;

public class ImportedFunctionDefinition : StatementBase
{
    public IToken FunctionName { get; set; }
    public TypeSymbol ReturnType { get; set; }
    public List<Parameter> Parameters { get; set; }
    public CallingConvention CallingConvention { get; set; }
    public IToken LibraryAlias { get; set; }
    public IToken FunctionSymbol { get; set; }
    public ImportedFunctionDefinition(IToken functionName, TypeSymbol returnType, List<Parameter> parameters, CallingConvention callingConvention, IToken libraryAlias, IToken functionSymbol) : base(functionName)
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

    public override void GatherSignature(TypeResolver typeResolver)
    {
        typeResolver.GatherSignature(this);
    }

    public override TypedStatement Resolve(TypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }
}