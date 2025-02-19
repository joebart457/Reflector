using Language.Experimental.Models;
using Language.Experimental.Statements;
using Language.Experimental.TypedExpressions;
using Language.Experimental.TypedStatements;
using ParserLite.Exceptions;
using System.Diagnostics.CodeAnalysis;
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

    public bool TryGetContainingExpression(int line, int column, out TypedExpression? containingExpression)
    {
        if (Expression.TryGetContainingExpression(line, column, out containingExpression)) return true;
        return false;       
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
            if (expressionContext.TryGetContainingExpression(line, column, out var containingExpression) && containingExpression != null) return new ExpressionContext(containingExpression, this);
        }
        return null;
    }

    public TypedParameter? GetParameter(IToken paramterName) => FunctionDefinition.Parameters.Find(x => x.Name.Lexeme == paramterName.Lexeme);
    public TypedLocalVariableExpression? GetLocalVariableExpression(IToken localVariableName) => FunctionDefinition.ExtractLocalVariableExpressions().FirstOrDefault(x => x.Identifier.Lexeme == localVariableName.Lexeme);

}

public class ProgramContext
{
    private class TokenEqualityComparer : EqualityComparer<IToken>
    {
        public override bool Equals(IToken? x, IToken? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals (y, null)) return false;
            return x.Lexeme == y.Lexeme && x.Type == y.Type;
        }

        public override int GetHashCode([DisallowNull] IToken obj)
        {
            return obj.Lexeme.GetHashCode();
        }
    }
    public List<TypedFunctionDefinition> FunctionDefinitions { get; set; } = new();
    public List<TypedImportedFunctionDefinition> ImportedFunctionDefinitions { get; set; } = new();
    public List<TypedImportLibraryDefinition> ImportLibraryDefinitions { get; set; } = new();
    public Dictionary<IToken, GenericTypeDefinition> GenericTypeDefinitions { get; set; } = new(new TokenEqualityComparer());
    public List<StructTypeInfo> UserDefinedTypes { get; set; } = new();
    public List<(IToken, string)> ValidationErrors { get; set; } = new();
    public List<IToken> Tokens { get; set; } = new();
    public Dictionary<IToken, List<IToken>> References { get; set; } = new(new TokenEqualityComparer());
    public FunctionContext? GetFunctionContext(int line, int column)
    {
        FunctionContext? match = null;
        foreach(var function in FunctionDefinitions)
        {
            var functionContext = new FunctionContext(function, this);
            if (functionContext.Contains(line, column))
            {
                if (match == null) match = functionContext;
                // search for inner most matching function
                else if (match.Contains(functionContext.Start.Line, functionContext.Start.Column)) match = functionContext;
            }
        }
        return match;
    }

    public void AddValidationError(ParsingException parsingException)
    {
        ValidationErrors.Add((parsingException.Token, parsingException.Message));
    }

    public IToken? GetTokenAt(int line, int column)
    {
        foreach(var token in Tokens)
        {
            if (ContainsToken(token, line, column)) return token;
        }
        return null;
    }

    public (int index, IToken? token) GetTokenAndIndexAt(int line, int column)
    {
        int index = 0;
        foreach (var token in Tokens)
        {
            if (ContainsToken(token, line, column)) return (index, token);
            index++;
        }
        return (-1, null);
    }

    public IToken? GetPreviousToken(int index)
    {
        index--;
        if (index <0 || index >= Tokens.Count) return null;
        return Tokens[index];
    }

    public bool ContainsToken(IToken token, int line, int column)
    {
        if (line == token.Start.Line && line == token.End.Line) return token.Start.Column <= column && column <= token.End.Column;
        if (line == token.Start.Line) return token.Start.Column <= column;
        if (line == token.End.Line) return token.End.Column >= column;
        return token.Start.Line <= line && token.End.Line >= line;
    }

}