using Language.Experimental.Models;
using Language.Experimental.TypedExpressions;
using Language.Experimental.TypedStatements;
using ParserLite.Exceptions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Api;




public class ExpressionContext
{
    public TypedExpression Expression { get; set; }
    public FunctionContext FunctionContext { get; set; }
    public ExpressionContext(TypedExpression expression, FunctionContext functionContext)
    {
        Expression = expression;
        FunctionContext = functionContext;
    }
    public ILocation Start => Expression.OriginalExpression.StartToken.Start;
    public ILocation End => Expression.OriginalExpression.EndToken.End;

    public bool Contains(int line, int column)
    {
        if (line == Start.Line) return Start.Column <= column;
        if (line == End.Line) return End.Column >= column;
        return Start.Line <= line && End.Line >= line;
    }
}

public class FunctionContext
{
    public TypedFunctionDefinition FunctionDefinition { get; set; }
    public ProgramContext ProgramContext { get; set; }
    public FunctionContext(TypedFunctionDefinition functionDefinition, ProgramContext programContext)
    {
        FunctionDefinition = functionDefinition;
        ProgramContext = programContext;
    }

    public ILocation Start => FunctionDefinition.OriginalStatement.StartToken.Start;
    public ILocation End => FunctionDefinition.OriginalStatement.EndToken.End;

    public bool Contains(int line, int column)
    {
        if (line == Start.Line) return Start.Column <= column;
        if (line == End.Line) return End.Column >= column;
        return Start.Line <= line && End.Line >= line;
    }

    public ExpressionContext? GetExpressionContext(int line, int column)
    {
        if (!Contains(line, column)) return null;
        foreach(var expression in FunctionDefinition.BodyStatements)
        {
            var expressionContext = new ExpressionContext(expression, this);
            if (expressionContext.Contains(line, column)) return expressionContext;
        }
        return null;
    }
}

public class ProgramContext
{
    public List<TypedFunctionDefinition> FunctionDefinitions { get; set; } = new();
    public List<TypedImportedFunctionDefinition> ImportedFunctionDefinitions { get; set; } = new();
    public List<TypedImportLibraryDefinition> ImportLibraryDefinitions { get; set; } = new();
    public List<TypeInfo> AvailableTypes { get; set; } = new();
    public List<(IToken, string)> ValidationErrors { get; set; } = new();
    public FunctionContext? GetFunctionContext(int line, int column)
    {
        foreach(var function in FunctionDefinitions)
        {
            var functionContext = new FunctionContext(function, this);
            if (!functionContext.Contains(line, column)) return functionContext;
        }
        return null;
    }

    public void AddValidationError(ParsingException parsingException)
    {
        ValidationErrors.Add((parsingException.Token, parsingException.Message));
    }

}