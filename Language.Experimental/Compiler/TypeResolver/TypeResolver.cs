using Language.Experimental.Expressions;
using Language.Experimental.Models;
using Language.Experimental.Statements;
using Language.Experimental.TypedExpressions;
using Language.Experimental.TypedStatements;
using ParserLite.Exceptions;

namespace Language.Experimental.Compiler.TypeResolver;

public class TypeResolver
{
    private TypedFunctionDefinition? _currentFunctionTarget;
    private TypedFunctionDefinition CurrentFunctionTarget => _currentFunctionTarget ?? throw new ArgumentNullException(nameof(CurrentFunctionTarget));
    private Dictionary<string, TypeInfo> _localVariableTypeMap = new();
    private Dictionary<string, TypedFunctionDefinition> _functionDefinitions = new();
    private Dictionary<string, TypedImportedFunctionDefinition> _importedFunctionDefinitions = new();
    private Dictionary<string, TypedImportLibraryDefinition> _importLibraries = new();
    public IEnumerable<TypedStatement> Resolve(List<StatementBase> statements)
    {
        GatherSignatures(statements);
        foreach(var statement in statements)
        {
            yield return statement.Resolve(this);
        }
        yield break;
    }

    public void GatherSignatures(List<StatementBase> statements)
    {
        _localVariableTypeMap = new();
        _functionDefinitions = new();
        _importedFunctionDefinitions = new();
        _importLibraries = new();
        _currentFunctionTarget = null;
        foreach (var statement in statements)
        {
            statement.GatherSignature(this);
        }
    }

    internal void GatherSignature(FunctionDefinition functionDefinition)
    {
        if (functionDefinition.Parameters.DistinctBy(x => x.Name.Lexeme).Count() != functionDefinition.Parameters.Count)
            throw new ParsingException(functionDefinition.FunctionName, $"redefinition of parameter name");

        var functionBody = new List<TypedExpression>();

        if (_functionDefinitions.ContainsKey(functionDefinition.FunctionName.Lexeme))
            throw new ParsingException(functionDefinition.FunctionName, $"redefinition of function {functionDefinition.FunctionName.Lexeme}");
        if (_importedFunctionDefinitions.ContainsKey(functionDefinition.FunctionName.Lexeme))
            throw new ParsingException(functionDefinition.FunctionName, $"redefinition of symbol {functionDefinition.FunctionName.Lexeme}");

        _functionDefinitions[functionDefinition.FunctionName.Lexeme] = new TypedFunctionDefinition(functionDefinition.FunctionName, functionDefinition.ReturnType, functionDefinition.Parameters, functionBody);
        
    }

    internal TypedStatement Resolve(FunctionDefinition functionDefinition)
    {
        if (!_functionDefinitions.TryGetValue(functionDefinition.FunctionName.Lexeme, out _currentFunctionTarget))
            throw new ParsingException(functionDefinition.FunctionName, $"unresolved function reference {functionDefinition.FunctionName.Lexeme}");

        _localVariableTypeMap = new();
        foreach(var expression in functionDefinition.BodyStatements)
        {
            CurrentFunctionTarget.BodyStatements.Add(expression.Resolve(this));
        }
        return CurrentFunctionTarget;
    }

    internal TypedStatement Resolve(ImportedFunctionDefinition importedFunctionDefinition)
    {
        if (!_importedFunctionDefinitions.TryGetValue(importedFunctionDefinition.FunctionName.Lexeme, out var typedImportedFunctionDefinition))
            throw new ParsingException(importedFunctionDefinition.FunctionName, $"unresolved imported function reference {importedFunctionDefinition.FunctionName.Lexeme}");

        return typedImportedFunctionDefinition;
    }

    internal TypedStatement Resolve(ImportLibraryDefinition importLibraryDefinition)
    {
        if (!_importLibraries.TryGetValue(importLibraryDefinition.LibraryAlias.Lexeme, out var typedImportLibraryDefinition))
            throw new ParsingException(importLibraryDefinition.LibraryAlias, $"unresolved library reference {importLibraryDefinition.LibraryAlias.Lexeme}");

        return typedImportLibraryDefinition;
    }

    internal void GatherSignature(ImportedFunctionDefinition importedFunctionDefinition)
    {
        if (importedFunctionDefinition.Parameters.DistinctBy(x => x.Name.Lexeme).Count() != importedFunctionDefinition.Parameters.Count)
            throw new ParsingException(importedFunctionDefinition.FunctionName, $"redefinition of parameter name");

        if (_functionDefinitions.ContainsKey(importedFunctionDefinition.FunctionName.Lexeme))
            throw new ParsingException(importedFunctionDefinition.FunctionName, $"redefinition of function {importedFunctionDefinition.FunctionName.Lexeme}");
        if (_importedFunctionDefinitions.ContainsKey(importedFunctionDefinition.FunctionName.Lexeme))
            throw new ParsingException(importedFunctionDefinition.FunctionName, $"redefinition of imported symbol {importedFunctionDefinition.FunctionName.Lexeme}");

        if (!_importLibraries.ContainsKey(importedFunctionDefinition.LibraryAlias.Lexeme))
            throw new ParsingException(importedFunctionDefinition.LibraryAlias, $"unable to import function from undefined library '{importedFunctionDefinition.LibraryAlias.Lexeme}'");

        _importedFunctionDefinitions[importedFunctionDefinition.FunctionName.Lexeme] = new TypedImportedFunctionDefinition(importedFunctionDefinition.FunctionName, importedFunctionDefinition.ReturnType, importedFunctionDefinition.Parameters, importedFunctionDefinition.CallingConvention, importedFunctionDefinition.LibraryAlias, importedFunctionDefinition.FunctionSymbol);
    }

    internal void GatherSignature(ImportLibraryDefinition importLibraryDefinition)
    {
        if (_importLibraries.ContainsKey(importLibraryDefinition.LibraryAlias.Lexeme))
            throw new ParsingException(importLibraryDefinition.LibraryAlias, $"import library with alias {importLibraryDefinition.LibraryAlias.Lexeme} is already defined");
        _importLibraries[importLibraryDefinition.LibraryAlias.Lexeme] = new TypedImportLibraryDefinition(importLibraryDefinition.LibraryAlias, importLibraryDefinition.LibraryPath);
    }
    internal TypedExpression Resolve(CallExpression callExpression)
    {
        var callTarget = callExpression.CallTarget.Resolve(this);
        if (!callTarget.TypeInfo.IsFunctionPtr) throw new ParsingException(callExpression.Token, $"expect call target to be of type fn<...,t> but got {callTarget.TypeInfo}");
        var args = callExpression.Arguments.Select(x => x.Resolve(this)).ToList();
        return new TypedCallExpression(callTarget.TypeInfo.FunctionReturnType, callExpression, callTarget, args);
    }

    internal TypedExpression Resolve(CompilerIntrinsic_GetExpression compilerIntrinsic_GetExpression)
    {
        var retrievedType = compilerIntrinsic_GetExpression.RetrievedType;
        var contextPointer = compilerIntrinsic_GetExpression.ContextPointer.Resolve(this);
        if (!contextPointer.TypeInfo.IsValidNormalPtr) throw new ParsingException(compilerIntrinsic_GetExpression.Token, $"retrieval context expects pointer type but got {contextPointer.TypeInfo}");
        return new TypedCompilerIntrinsic_GetExpression(retrievedType, compilerIntrinsic_GetExpression, contextPointer, compilerIntrinsic_GetExpression.MemberOffset);
    }

    internal TypedExpression Resolve(CompilerIntrinsic_SetExpression compilerIntrinsic_SetExpression)
    {
        var valueToAssign = compilerIntrinsic_SetExpression.ValueToAssign.Resolve(this);
        var contextPointer = compilerIntrinsic_SetExpression.ContextPointer.Resolve(this);
        if (!contextPointer.TypeInfo.IsValidNormalPtr) throw new ParsingException(compilerIntrinsic_SetExpression.Token, $"memory context expects pointer type but got {contextPointer.TypeInfo}");
        return new TypedCompilerIntrinsic_SetExpression(TypeInfo.Void, compilerIntrinsic_SetExpression, contextPointer, compilerIntrinsic_SetExpression.AssignmentOffset, valueToAssign);
    }

    internal TypedExpression Resolve(GetExpression getExpression)
    {
        throw new NotImplementedException();
    }

    internal TypedExpression Resolve(IdentifierExpression identifierExpression)
    {
        var foundType = CurrentFunctionTarget.Parameters.Find(x => x.Name.Lexeme == identifierExpression.Token.Lexeme)?.TypeInfo;
        if (foundType == null && !_localVariableTypeMap.TryGetValue(identifierExpression.Token.Lexeme, out foundType))
        {
            if (_functionDefinitions.TryGetValue(identifierExpression.Token.Lexeme, out var functionWithMatchingName))
                foundType = functionWithMatchingName.GetFunctionPointerType();
            if (_importedFunctionDefinitions.TryGetValue(identifierExpression.Token.Lexeme, out var importedFunctionWithMatchingName))
                foundType = importedFunctionWithMatchingName.GetFunctionPointerType();
        }
        if (foundType == null)
            throw new ParsingException(identifierExpression.Token, $"unresolved symbol {identifierExpression.Token.Lexeme}");
        return new TypedIdentifierExpression(foundType, identifierExpression, identifierExpression.Token);
    }

    internal TypedExpression Resolve(InlineAssemblyExpression inlineAssemblyExpression)
    {
        return new TypedInlineAssemblyExpression(TypeInfo.Void, inlineAssemblyExpression, inlineAssemblyExpression.Assembly);
    }

    internal TypedExpression Resolve(LiteralExpression literalExpression)
    {
        if (literalExpression.Value == null)
            return new TypedLiteralExpression(TypeInfo.Pointer(TypeInfo.Void), literalExpression, null);
        if (literalExpression.Value.GetType() == typeof(string))
            return new TypedLiteralExpression(TypeInfo.String, literalExpression, literalExpression.Value);
        if (literalExpression.Value.GetType() == typeof(int))
            return new TypedLiteralExpression(TypeInfo.Integer, literalExpression, literalExpression.Value);
        if (literalExpression.Value.GetType() == typeof(float))
            return new TypedLiteralExpression(TypeInfo.Float, literalExpression, literalExpression.Value);
        throw new ParsingException(literalExpression.Token, $"unsupported literal type {literalExpression.Value.GetType().Name}");
    }

    internal TypedExpression Resolve(LocalVariableExpression localVariableExpression)
    {
        if (CurrentFunctionTarget.Parameters.Any(x => x.Name.Lexeme == localVariableExpression.Identifier.Lexeme))
            throw new ParsingException(localVariableExpression.Identifier, $"symbol {localVariableExpression.Identifier.Lexeme} is already defined as a parameter");
        if (_localVariableTypeMap.ContainsKey(localVariableExpression.Identifier.Lexeme))
            throw new ParsingException(localVariableExpression.Identifier, $"symbol {localVariableExpression.Identifier.Lexeme} is already defined");
        _localVariableTypeMap.Add(localVariableExpression.Identifier.Lexeme, localVariableExpression.TypeInfo);
        var initializer = localVariableExpression.Initializer?.Resolve(this);
        if (initializer != null && !initializer.TypeInfo.Equals(localVariableExpression.TypeInfo))
            throw new ParsingException(localVariableExpression.Identifier, $"expect initializer value of type {localVariableExpression.TypeInfo} but got {initializer.TypeInfo}");
        return new TypedLocalVariableExpression(TypeInfo.Void, localVariableExpression, localVariableExpression.Identifier, initializer);
    }

    
}