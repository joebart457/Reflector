using Language.Experimental.Constants;
using Language.Experimental.Expressions;
using Language.Experimental.Interfaces;
using Language.Experimental.Models;
using Language.Experimental.Parser;
using Language.Experimental.TypedStatements;
using System.Runtime.InteropServices;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Statements;


public class FunctionDefinition : StatementBase
{
    public IToken FunctionName { get; set; }
    public TypeSymbol ReturnType { get; set; }
    public List<Parameter> Parameters { get; set; }
    public List<ExpressionBase> BodyStatements { get; set; }
    public CallingConvention CallingConvention { get; set; }
    public bool IsExported { get; set; }
    public IToken ExportedSymbol { get; set; }

    public FunctionDefinition(IToken functionName, TypeSymbol returnType, List<Parameter> parameters, List<ExpressionBase> bodyStatements) : base(functionName)
    {
        FunctionName = functionName;
        ReturnType = returnType;
        Parameters = parameters;
        BodyStatements = bodyStatements;
        CallingConvention = CallingConvention.StdCall;
        IsExported = false;
        ExportedSymbol = functionName;
    }


    public FunctionDefinition(IToken functionName, TypeSymbol returnType, List<Parameter> parameters, List<ExpressionBase> bodyStatements, CallingConvention callingConvention, bool isExported, IToken exportedSymbol) : base(functionName)
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

    public override void GatherSignature(ITypeResolver typeResolver)
    {
        typeResolver.GatherSignature(this);
    }

    public override TypedStatement Resolve(ITypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);  
    }

    public FunctionDefinition ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        var returnType = ReturnType.ReplaceGenericTypeParameter(genericToConcreteTypeMap);
        var parameters = Parameters.Select(x => new Parameter(x.Name, x.TypeSymbol.ReplaceGenericTypeParameter(genericToConcreteTypeMap))).ToList();
        var bodyStatements = BodyStatements.Select(x => x.ReplaceGenericTypeSymbols(genericToConcreteTypeMap)).ToList();
        return new FunctionDefinition(FunctionName, returnType, parameters, bodyStatements, CallingConvention, IsExported, ExportedSymbol);
    }

}