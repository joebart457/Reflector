using Language.Experimental.Parser;
using Language.Experimental.Statements;
using Language.Experimental.UnresolvedExpressions;
using System.Runtime.InteropServices;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedStatements;


public class UnresolvedFunctionDefinition : UnresolvedStatementBase
{
    public IToken FunctionName { get; set; }
    public TypeSymbol ReturnType { get; set; }
    public List<UnresolvedParameter> Parameters { get; set; }
    public List<UnresolvedExpressionBase> BodyStatements { get; set; }
    public CallingConvention CallingConvention { get; set; }
    public bool IsExported { get; set; }
    public IToken ExportedSymbol { get; set; }

    public UnresolvedFunctionDefinition(IToken functionName, TypeSymbol returnType, List<UnresolvedParameter> parameters, List<UnresolvedExpressionBase> bodyStatements) : base(functionName)
    {
        FunctionName = functionName;
        ReturnType = returnType;
        Parameters = parameters;
        BodyStatements = bodyStatements;
        CallingConvention = CallingConvention.StdCall;
        IsExported = false;
        ExportedSymbol = functionName;
    }


    public UnresolvedFunctionDefinition(IToken functionName, TypeSymbol returnType, List<UnresolvedParameter> parameters, List<UnresolvedExpressionBase> bodyStatements, CallingConvention callingConvention, bool isExported, IToken exportedSymbol) : base(functionName)
    {
        FunctionName = functionName;
        ReturnType = returnType;
        Parameters = parameters;
        BodyStatements = bodyStatements;
        CallingConvention = callingConvention;
        IsExported = isExported;
        ExportedSymbol = exportedSymbol;
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

    public UnresolvedFunctionDefinition ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        var returnType = ReturnType.ReplaceGenericTypeParameter(genericToConcreteTypeMap);
        var parameters = Parameters.Select(x => new UnresolvedParameter(x.Name, x.TypeSymbol.ReplaceGenericTypeParameter(genericToConcreteTypeMap))).ToList();
        var bodyStatements = BodyStatements.Select(x => x.ReplaceGenericTypeSymbols(genericToConcreteTypeMap)).ToList();
        return new UnresolvedFunctionDefinition(FunctionName, returnType, parameters, bodyStatements, CallingConvention, IsExported, ExportedSymbol);
    }
}