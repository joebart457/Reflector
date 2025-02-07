using Language.Experimental.Constants;
using Language.Experimental.Expressions;
using Language.Experimental.Models;
using Language.Experimental.Parser;
using Language.Experimental.Statements;
using Language.Experimental.TypedExpressions;
using Language.Experimental.TypedStatements;
using ParserLite.Exceptions;
using TokenizerCore.Interfaces;
using TokenizerCore.Model;
using TokenizerCore.Models.Constants;

namespace Language.Experimental.Compiler.TypeResolver;

public class TypeResolver
{
    private TypedFunctionDefinition? _currentFunctionTarget;
    private TypedFunctionDefinition CurrentFunctionTarget => _currentFunctionTarget ?? throw new ArgumentNullException(nameof(CurrentFunctionTarget));
    private Dictionary<string, TypeInfo> _localVariableTypeMap = new();
    private Dictionary<string, TypedFunctionDefinition> _functionDefinitions = new();
    private Dictionary<string, TypedImportedFunctionDefinition> _importedFunctionDefinitions = new();
    private Dictionary<string, TypedImportLibraryDefinition> _importLibraries = new();
    private List<TypedFunctionDefinition> _lambdaFunctions = new();
    public IEnumerable<TypedStatement> Resolve(ParsingResult parsingResult)
    {
        GatherSignatures(parsingResult);
        foreach(var statement in parsingResult.ImportLibraryDefinitions)
        {
            yield return statement.Resolve(this);
        }
        foreach (var statement in parsingResult.ImportedFunctionDefinitions)
        {
            yield return statement.Resolve(this);
        }
        foreach (var statement in parsingResult.FunctionDefinitions)
        {
            yield return statement.Resolve(this);
        }
        yield break;
    }

    public void GatherSignatures(ParsingResult parsingResult)
    {
        _localVariableTypeMap = new();
        _functionDefinitions = new();
        _importedFunctionDefinitions = new();
        _importLibraries = new();
        _currentFunctionTarget = null;
        foreach (var statement in parsingResult.ImportLibraryDefinitions)
        {
            statement.GatherSignature(this);
        }
        foreach (var statement in parsingResult.ImportedFunctionDefinitions)
        {
            statement.GatherSignature(this);
        }
        foreach (var statement in parsingResult.FunctionDefinitions)
        {
            statement.GatherSignature(this);
        }
    }

    internal void GatherSignature(FunctionDefinition functionDefinition)
    {
        if (functionDefinition.Parameters.DistinctBy(x => x.Name.Lexeme).Count() != functionDefinition.Parameters.Count)
            throw new ParsingException(functionDefinition.FunctionName, $"redefinition of parameter name");

        var invalidParameter = functionDefinition.Parameters.Find(x => !x.TypeInfo.IsStackAllocatable);
        if (invalidParameter != null)
            throw new ParsingException(functionDefinition.FunctionName, $"invalid parameter type {invalidParameter.TypeInfo}. Type is not stack allocatable");

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

        var invalidParameter = importedFunctionDefinition.Parameters.Find(x => !x.TypeInfo.IsStackAllocatable);
        if (invalidParameter != null)
            throw new ParsingException(importedFunctionDefinition.FunctionName, $"invalid parameter type {invalidParameter.TypeInfo}. Type is not stack allocatable");

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
        TypedExpression callTarget; 
       
        if (callExpression.CallTarget is IdentifierExpression identifierExpression) 
            callTarget = ResolveCallTarget(identifierExpression); 
        else callTarget = callExpression.CallTarget.Resolve(this);

        if (!callTarget.TypeInfo.IsFunctionPtr) throw new ParsingException(callExpression.Token, $"expect call target to be of type fn<...,t> but got {callTarget.TypeInfo}");
        var args = callExpression.Arguments.Select(x => x.Resolve(this)).ToList();
        if (args.Count != callTarget.TypeInfo.FunctionParameterTypes.Count)
            throw new ParsingException(callExpression.Token, $"parity mismatch in call {callTarget.TypeInfo}: expected {callTarget.TypeInfo.FunctionParameterTypes.Count} arguments but got {args.Count}");
        for (int i = 0; i < callTarget.TypeInfo.FunctionParameterTypes.Count; i++)
        {
            if (!callTarget.TypeInfo.FunctionParameterTypes[i].Equals(args[i].TypeInfo))
                throw new ParsingException(callExpression.Token, $"call {callTarget.TypeInfo}:expected argument to be of type {callTarget.TypeInfo.FunctionParameterTypes[i]} but got {args[i].TypeInfo}");
        }
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
        var instance = getExpression.Instance.Resolve(this);
        if (instance.TypeInfo.IsValidNormalPtr && instance.TypeInfo.GenericTypeArgument!.IsStructType)
        {
            var fieldType = instance.TypeInfo.GetFieldType(getExpression.TargetField);
            return new TypedGetExpression(fieldType, getExpression, instance, getExpression.TargetField, getExpression.ShortCircuitOnNull);
        }
        throw new ParsingException(getExpression.Token, $"expect valid pointer type on left hand side of member accessor");
    }

    internal TypedExpression Resolve(IdentifierExpression identifierExpression)
    {
        var foundType = CurrentFunctionTarget.Parameters.Find(x => x.Name.Lexeme == identifierExpression.Token.Lexeme)?.TypeInfo;
        if (foundType == null && !_localVariableTypeMap.TryGetValue(identifierExpression.Token.Lexeme, out foundType))
        {
            if (_functionDefinitions.TryGetValue(identifierExpression.Token.Lexeme, out var functionWithMatchingName))
                return new TypedFunctionPointerExpression(functionWithMatchingName.GetFunctionPointerType().AsReference(), identifierExpression, functionWithMatchingName.FunctionName, false); // identifiers only reference the function address so they can be used as lambdas
            if (_importedFunctionDefinitions.TryGetValue(identifierExpression.Token.Lexeme, out var importedFunctionWithMatchingName))
                return new TypedFunctionPointerExpression(importedFunctionWithMatchingName.GetFunctionPointerType().AsReference(), identifierExpression, importedFunctionWithMatchingName.FunctionName, true);
        }
        if (foundType == null)
            throw new ParsingException(identifierExpression.Token, $"unresolved symbol {identifierExpression.Token.Lexeme}");
        return new TypedIdentifierExpression(foundType, identifierExpression, identifierExpression.Token);
    }

    internal TypedExpression ResolveCallTarget(IdentifierExpression identifierExpression)
    {
        // Identifiers that are direct call targets will be handled differently IE
        // (printf msg) 
        // printf will be resolved to type Cdecl_FunctionPointer_External instead of Cdecl_FunctionPointer
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

    internal TypedExpression ResolveSetTarget(IdentifierExpression identifierExpression)
    {
        var foundType = CurrentFunctionTarget.Parameters.Find(x => x.Name.Lexeme == identifierExpression.Token.Lexeme)?.TypeInfo;
        if (foundType == null) _localVariableTypeMap.TryGetValue(identifierExpression.Token.Lexeme, out foundType);
        if (foundType == null)
            throw new ParsingException(identifierExpression.Token, $"unresolved symbol {identifierExpression.Token.Lexeme}");
        return new TypedIdentifierExpression(foundType, identifierExpression, identifierExpression.Token);
    }

    internal TypedExpression Resolve(InlineAssemblyExpression inlineAssemblyExpression)
    {
        return new TypedInlineAssemblyExpression(TypeInfo.Void, inlineAssemblyExpression, inlineAssemblyExpression.AssemblyInstruction);
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
        if (!localVariableExpression.TypeInfo.IsStackAllocatable)
            throw new ParsingException(localVariableExpression.Token, $"unable to create local variable of type {localVariableExpression.TypeInfo}");
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

    internal TypedExpression Resolve(ReturnExpression returnExpression)
    {
        var returnValue = returnExpression.ReturnValue?.Resolve(this);
        if (CurrentFunctionTarget.ReturnType.Is(IntrinsicType.Void))
        {
            if (returnValue != null) throw new ParsingException(returnExpression.Token, $"unable to return value for function with return type of {IntrinsicType.Void}");
            else return new TypedReturnExpression(TypeInfo.Void, returnExpression, null);
        }
        if (returnValue == null)
            throw new ParsingException(returnExpression.Token, $"expected return type to match function return type of {CurrentFunctionTarget.ReturnType} but got None");
        if (!returnValue.TypeInfo.Equals(CurrentFunctionTarget.ReturnType))
            throw new ParsingException(returnExpression.Token, $"expected return type to match function return type of {CurrentFunctionTarget.ReturnType} but got {returnValue.TypeInfo}");
        return new TypedReturnExpression(TypeInfo.Void, returnExpression, returnValue);
    }

    internal TypedExpression Resolve(SetExpression setExpression)
    {
        TypedExpression setTarget;
        if (setExpression.AssignmentTarget is IdentifierExpression identifierExpression)
        {
            setTarget = ResolveSetTarget(identifierExpression);
        }
        else if (setExpression.AssignmentTarget is GetExpression getExpression)
        {
            setTarget = Resolve(getExpression);
        }
        else throw new ParsingException(setExpression.Token, $"expect assignment target to be identifier or member accessor");
        var valueToAssign = setExpression.ValueToAssign.Resolve(this);
        if (!valueToAssign.TypeInfo.IsStackAllocatable)
            throw new ParsingException(setExpression.ValueToAssign.Token, $"invalid value transfer (type {valueToAssign.TypeInfo})");
        if (!setTarget.TypeInfo.Equals(valueToAssign.TypeInfo))
            throw new ParsingException(setExpression.Token, $"type mismatch: expected assignment value to be of type {setTarget.TypeInfo} but got {valueToAssign.TypeInfo}");
        return new TypedSetExpression(setTarget.TypeInfo, setExpression, setTarget, valueToAssign);
    }
    internal TypedExpression Resolve(CastExpression castExpression)
    {
        // We will trust the programmer on casts and only disallow casting to struct types bigger than 4 bytes
        var resolvedExpression = castExpression.Expression.Resolve(this);
        if (castExpression.TypeInfo.IsStackAllocatable || castExpression.TypeInfo.SizeInMemory() == 4)
        {
            resolvedExpression.TypeInfo = castExpression.TypeInfo;
            return resolvedExpression;
        }
        throw new ParsingException(castExpression.Token, $"unable to cast type {resolvedExpression.TypeInfo} to type {castExpression.TypeInfo}");
    }

    internal TypedExpression Resolve(LambdaExpression lambdaExpression)
    {
        // For lambdas we will simply pull out the function definition and return a reference to the function as an (unique, generated) identifier
        var anonymousToken = GetAnonymousFunctionLabel(lambdaExpression.FunctionDefinition.Token.Location.Line, lambdaExpression.FunctionDefinition.Token.Location.Column);
        lambdaExpression.FunctionDefinition.FunctionName = anonymousToken;
        GatherSignature(lambdaExpression.FunctionDefinition);
        var flattenedLambda = (TypedFunctionDefinition)Resolve(lambdaExpression.FunctionDefinition);
        _lambdaFunctions.Add(flattenedLambda);
        return Resolve(new IdentifierExpression(anonymousToken));
    }

    private IToken GetAnonymousFunctionLabel(int row, int column) => new Token(BuiltinTokenTypes.Word, $"_anonymous__!{AnonymousFunctionIndex++}", row, column);

    private int AnonymousFunctionIndex = 0;
}