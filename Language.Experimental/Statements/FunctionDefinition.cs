using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.Constants;
using Language.Experimental.Expressions;
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



    public IEnumerable<LocalVariableExpression> ExtractLocalVariableExpressions()
    {
        return BodyStatements.SelectMany(e => ExtractLocalVariableExpressionsHelper(e));
    }

    private List<LocalVariableExpression> ExtractLocalVariableExpressionsHelper(ExpressionBase expression)
    {
        var ls = new List<LocalVariableExpression>();
        if (expression is CallExpression ce)
        {
            ls.AddRange(ExtractLocalVariableExpressionsHelper(ce.CallTarget));
            foreach (var arg in ce.Arguments) ls.AddRange(ExtractLocalVariableExpressionsHelper(arg));
        }
        else if (expression is CompilerIntrinsic_GetExpression ci_get) ls.AddRange(ExtractLocalVariableExpressionsHelper(ci_get.ContextPointer));
        else if (expression is CompilerIntrinsic_SetExpression ci_set) ls.AddRange(ExtractLocalVariableExpressionsHelper(ci_set.ContextPointer));
        else if (expression is GetExpression get)
        {
            ls.AddRange(ExtractLocalVariableExpressionsHelper(get.Instance));
        }
        else if (expression is IdentifierExpression id) { }
        else if (expression is InlineAssemblyExpression asm) { }
        else if (expression is LiteralExpression le) { }
        else if (expression is LocalVariableExpression lve) ls.Add(lve);
        else throw new InvalidOperationException($"unsupported expression type {expression.GetType().Name}");
        return ls;
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

    public FunctionDefinition ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        var returnType = ReturnType.ReplaceGenericTypeParameter(genericToConcreteTypeMap);
        var parameters = Parameters.Select(x => new Parameter(x.Name, x.TypeSymbol.ReplaceGenericTypeParameter(genericToConcreteTypeMap))).ToList();
        var bodyStatements = BodyStatements.Select(x => x.ReplaceGenericTypeSymbols(genericToConcreteTypeMap)).ToList();
        return new FunctionDefinition(FunctionName, returnType, parameters, bodyStatements, CallingConvention, IsExported, ExportedSymbol);
    }

}