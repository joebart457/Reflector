using Language.Experimental.Models;
using Language.Experimental.Parser;
using Language.Experimental.Statements;
using Language.Experimental.TypedExpressions;
using Language.Experimental.TypedStatements;
using ParserLite.Exceptions;

namespace Language.Experimental.Api;
public class LanguageInformationEngine : TypeResolver.TypeResolver
{
    private ProgramContext _programContext = new();

    public ProgramContext Resolve(string filePath)
    {
        var parser = new ProgramParser();
        _programContext = new ProgramContext();
        var result = parser.ParseFile(filePath, out var errors);
        _programContext.Tokens = parser.GetTokens().ToList();
        _programContext.ValidationErrors.AddRange(errors.Select(x => (x.Token, x.Message)));
        ResolveWithTryCatch(result);
        return _programContext;
    }

    public ProgramContext ResolveText(string text)
    {
        var parser = new ProgramParser();
        _programContext = new ProgramContext();
        var result = parser.ParseText(text, out var errors);
        _programContext.Tokens = parser.GetTokens().ToList();
        _programContext.ValidationErrors.AddRange(errors.Select(x => (x.Token, x.Message)));
        ResolveWithTryCatch(result);
        return _programContext;
    }

    private void ResolveWithTryCatch(ParsingResult parsingResult)
    {
        GatherSignatures(parsingResult);

        foreach (var typeDefinition in parsingResult.TypeDefinitions)
        {
            if (RunWithTryCatch(() => ResolveTypeDefinition(typeDefinition), out var resolved) && resolved != null)
                _programContext.UserDefinedTypes.Add(resolved);
        }
        foreach (var statement in parsingResult.ImportLibraryDefinitions)
        {
            if (RunWithTryCatch(() => (TypedImportLibraryDefinition)statement.Resolve(this), out var resolved))
                _programContext.ImportLibraryDefinitions.Add(resolved!);
        }
        foreach (var statement in parsingResult.ImportedFunctionDefinitions)
        {
            if (RunWithTryCatch(() => (TypedImportedFunctionDefinition)statement.Resolve(this), out var resolved))
                _programContext.ImportedFunctionDefinitions.Add(resolved!);
        }
        foreach (var statement in parsingResult.FunctionDefinitions)
        {
            if (RunWithTryCatch(() => (TypedFunctionDefinition)statement.Resolve(this), out var resolved))
                _programContext.FunctionDefinitions.Add(resolved!);
        }
        _programContext.FunctionDefinitions.AddRange(_lambdaFunctions);
        _programContext.FunctionDefinitions.Reverse();
    }


    private void GatherSignatures(ParsingResult parsingResult)
    {
        _localVariableTypeMap = new();
        _functionDefinitions = new();
        _importedFunctionDefinitions = new();
        _importLibraries = new();
        _currentFunctionTarget = null;
        foreach (var statement in parsingResult.TypeDefinitions)
        {
            RunWithTryCatch(() => statement.GatherSignature(this));
        }
        foreach (var statement in parsingResult.GenericTypeDefinitions)
        {
            RunWithTryCatch(() => statement.GatherSignature(this));
        }
        foreach (var statement in parsingResult.GenericFunctionDefinitions)
        {
            RunWithTryCatch(() => statement.GatherSignature(this));
        }
        foreach (var statement in parsingResult.ImportLibraryDefinitions)
        {
            RunWithTryCatch(() => statement.GatherSignature(this));
        }
        foreach (var statement in parsingResult.ImportedFunctionDefinitions)
        {
            RunWithTryCatch(() => statement.GatherSignature(this));
        }
        foreach (var statement in parsingResult.FunctionDefinitions)
        {
            RunWithTryCatch(() => statement.GatherSignature(this));
        }
    }

    private void RunWithTryCatch(Action action)
    {
        try
        {
            action();
        }catch(ParsingException pe)
        {
            _programContext.AddValidationError(pe);
        }
    }

    private bool RunWithTryCatch<Ty>(Func<Ty> func, out Ty? tyVal) where Ty: class
    {
        tyVal = default;
        try
        {
            tyVal = func();
            return true;
        }
        catch (ParsingException pe)
        {
            _programContext.AddValidationError(pe);
            return false;
        }
    }

   

    public override TypedStatement Resolve(FunctionDefinition functionDefinition)
    {
        if (!_functionDefinitions.TryGetValue(functionDefinition.FunctionName.Lexeme, out _currentFunctionTarget))
            throw new ParsingException(functionDefinition.FunctionName, $"unresolved function reference {functionDefinition.FunctionName.Lexeme}");

        _localVariableTypeMap = new();
        foreach (var expression in functionDefinition.BodyStatements)
        {
            try
            {
                CurrentFunctionTarget.BodyStatements.Add(expression.Resolve(this));
            }catch (ParsingException pe)
            {
                _programContext.AddValidationError(pe);
            }
        }
        var returnIndex = CurrentFunctionTarget.BodyStatements.FindIndex(x => x is TypedReturnExpression);
        if (returnIndex == -1) _programContext.AddValidationError(new ParsingException(functionDefinition.FunctionName, $"function {functionDefinition.FunctionName.Lexeme} does not return"));
        if (returnIndex != CurrentFunctionTarget.BodyStatements.Count - 1)
        {
            var unreachableStatement = CurrentFunctionTarget.BodyStatements[returnIndex + 1];
            _programContext.AddValidationError(new ParsingException(unreachableStatement.OriginalExpression.Token, $"unreachable code detected"));
        }
        return CurrentFunctionTarget;
    }

}