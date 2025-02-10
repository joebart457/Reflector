using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.Expressions;
using Language.Experimental.Parser;
using Language.Experimental.TypedStatements;
using ParserLite.Exceptions;
using System.Runtime.InteropServices;
using TokenizerCore.Interfaces;
using TokenizerCore.Model;

namespace Language.Experimental.Statements;


public class GenericFunctionDefinition : StatementBase
{
    public IToken FunctionName { get; set; }
    public List<GenericTypeSymbol> GenericTypeParameters { get; set; }
    public TypeSymbol ReturnType { get; set; }
    public List<Parameter> Parameters { get; set; }
    public List<ExpressionBase> BodyStatements { get; set; }
    public CallingConvention CallingConvention { get; set; }
    public bool IsExported { get; set; }

    public GenericFunctionDefinition(IToken functionName, List<GenericTypeSymbol> genericTypeParameters, TypeSymbol returnType, List<Parameter> parameters, List<ExpressionBase> bodyStatements) : base(functionName)
    {
        FunctionName = functionName;
        GenericTypeParameters = genericTypeParameters;
        ReturnType = returnType;
        Parameters = parameters;
        BodyStatements = bodyStatements;
        CallingConvention = CallingConvention.StdCall;
        IsExported = false;
    }

    public string GetDecoratedFunctionIdentifier()
    {
        if (CallingConvention == CallingConvention.Cdecl) return $"_{FunctionName.Lexeme}";
        if (CallingConvention == CallingConvention.StdCall) return $"_{FunctionName.Lexeme}@{Parameters.Count * 4}";
        throw new NotImplementedException();
    }

    public FunctionDefinition ToFunctionDefinition(List<TypeSymbol> typeArguments)
    {
        var unresolvedTypeParameter = typeArguments.Find(x => x.IsGenericTypeSymbol || x.ContainsGenericTypeSymbol);
        if (unresolvedTypeParameter != null)
            throw new ParsingException(FunctionName, $"invalid generic type arguments: unresolved generic type parameter {unresolvedTypeParameter}");
        if (typeArguments.Count != GenericTypeParameters.Count)
            throw new ParsingException(FunctionName, $"expected {GenericTypeParameters.Count} type arguments but got {typeArguments.Count}");

        var genericToConcreteTypeMap = new Dictionary<GenericTypeSymbol, TypeSymbol>();
        for (int i = 0; i < GenericTypeParameters.Count; i++)
        {
            if (genericToConcreteTypeMap.ContainsKey(GenericTypeParameters[i]))
                throw new ParsingException(GenericTypeParameters[i].TypeName, $"redefinition of generic type parameter {GenericTypeParameters[i].TypeName.Lexeme}");
            genericToConcreteTypeMap[GenericTypeParameters[i]] = typeArguments[i];
        }

        var newParameters = Parameters.Select(x => new Parameter(x.Name, x.TypeSymbol)).ToList();
        foreach (var parameter in newParameters)
        {
            parameter.TypeSymbol = parameter.TypeSymbol.ReplaceGenericTypeParameter(genericToConcreteTypeMap);
        }
        unresolvedTypeParameter = newParameters.Select(x => x.TypeSymbol).FirstOrDefault(x => x.IsGenericTypeSymbol || x.ContainsGenericTypeSymbol);
        if (unresolvedTypeParameter != null)
            throw new ParsingException(FunctionName, $"invalid generic type arguments: unresolved generic type parameter {unresolvedTypeParameter}");
        var newBodyStatements = BodyStatements.Select(x => x.ReplaceGenericTypeSymbols(genericToConcreteTypeMap)).ToList();

        var instantiatedFunctionName = $"{FunctionName.Lexeme}!{string.Join('_', typeArguments.Select(x => x.GetFlattenedName()))}";
        var instantiatedFunctionNameToken = new Token(FunctionName.Type, instantiatedFunctionName, FunctionName.Location.Line, FunctionName.Location.Column);

        var returnType = ReturnType.ReplaceGenericTypeParameter(genericToConcreteTypeMap);

        return new FunctionDefinition(instantiatedFunctionNameToken, returnType, newParameters, newBodyStatements, CallingConvention, IsExported, instantiatedFunctionNameToken);

    }

    public override void GatherSignature(TypeResolver typeResolver)
    {
        typeResolver.GatherSignature(this);
    }

    public override TypedStatement Resolve(TypeResolver typeResolver)
    {
        throw new NotImplementedException();
    }
}