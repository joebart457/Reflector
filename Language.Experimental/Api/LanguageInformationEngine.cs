using Language.Experimental.Constants;
using Language.Experimental.Expressions;
using Language.Experimental.Models;
using Language.Experimental.Parser;
using Language.Experimental.Statements;
using Language.Experimental.TypedExpressions;
using Language.Experimental.TypedStatements;
using Language.Experimental.TypeResolver;
using Microsoft.VisualBasic.FileIO;
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


    public override TypedExpression Resolve(CallExpression callExpression)
    {
        var args = callExpression.Arguments.Select(x => x.Resolve(this)).ToList();
        ITypedFunctionInfo? directCallTarget = null;
        if (callExpression.CallTarget is IdentifierExpression identifierExpression)
            directCallTarget = ResolveCallTarget(identifierExpression);
        else if (callExpression.CallTarget is GenericFunctionReferenceExpression genericFunctionReferenceExpression)
            directCallTarget = ResolveCallTarget(genericFunctionReferenceExpression, args);
        if (directCallTarget != null)
        {
            if (args.Count != directCallTarget.Parameters.Count)
                _programContext.AddValidationError(new ParsingException(callExpression.Token, $"parity mismatch in call {directCallTarget.FunctionName.Lexeme}: expected {directCallTarget.Parameters.Count} arguments but got {args.Count}"));
            else
            {
                for (int i = 0; i < directCallTarget.Parameters.Count; i++)
                {
                    if (!directCallTarget.Parameters[i].TypeInfo.Equals(args[i].TypeInfo))
                        _programContext.AddValidationError(new ParsingException(args[i].OriginalExpression.Token, $"call {directCallTarget.FunctionName.Lexeme}: expected argument to be of type {directCallTarget.Parameters[i].TypeInfo} but got {args[i].TypeInfo}"));
                }
            }
            return new TypedDirectCallExpression(directCallTarget.ReturnType, callExpression, directCallTarget, args);
        }

        TypedExpression callTarget = callExpression.CallTarget.Resolve(this);

        if (!callTarget.TypeInfo.IsFunctionPtr) _programContext.AddValidationError(new ParsingException(callExpression.Token, $"expect call target to be of type fn[...,t] but got {callTarget.TypeInfo}"));
        else
        {
            if (args.Count != callTarget.TypeInfo.FunctionParameterTypes.Count)
                _programContext.AddValidationError(new ParsingException(callExpression.Token, $"parity mismatch in call {callTarget.TypeInfo}: expected {callTarget.TypeInfo.FunctionParameterTypes.Count} arguments but got {args.Count}"));
            else
            {
                for (int i = 0; i < callTarget.TypeInfo.FunctionParameterTypes.Count; i++)
                {
                    if (!callTarget.TypeInfo.FunctionParameterTypes[i].Equals(args[i].TypeInfo))
                        _programContext.AddValidationError(new ParsingException(callExpression.Token, $"call {callTarget.TypeInfo}:expected argument to be of type {callTarget.TypeInfo.FunctionParameterTypes[i]} but got {args[i].TypeInfo}"));
                }
            }
            return new TypedCallExpression(callTarget.TypeInfo.FunctionReturnType, callExpression, callTarget, args);
        }

        return new TypedCallExpression(TypeInfo.Void, callExpression, callTarget, args);

    }

    public override TypedExpression Resolve(CompilerIntrinsic_GetExpression compilerIntrinsic_GetExpression)
    {
        var retrievedType = Resolve(compilerIntrinsic_GetExpression.RetrievedType);
        var contextPointer = compilerIntrinsic_GetExpression.ContextPointer.Resolve(this);
        if (!contextPointer.TypeInfo.IsValidNormalPtr) throw new ParsingException(compilerIntrinsic_GetExpression.Token, $"retrieval context expects pointer type but got {contextPointer.TypeInfo}");
        return new TypedCompilerIntrinsic_GetExpression(retrievedType, compilerIntrinsic_GetExpression, contextPointer, compilerIntrinsic_GetExpression.MemberOffset);
    }

    public override TypedExpression Resolve(CompilerIntrinsic_SetExpression compilerIntrinsic_SetExpression)
    {
        var valueToAssign = compilerIntrinsic_SetExpression.ValueToAssign.Resolve(this);
        var contextPointer = compilerIntrinsic_SetExpression.ContextPointer.Resolve(this);
        if (!contextPointer.TypeInfo.IsValidNormalPtr) throw new ParsingException(compilerIntrinsic_SetExpression.Token, $"memory context expects pointer type but got {contextPointer.TypeInfo}");
        return new TypedCompilerIntrinsic_SetExpression(TypeInfo.Void, compilerIntrinsic_SetExpression, contextPointer, compilerIntrinsic_SetExpression.AssignmentOffset, valueToAssign);
    }

    public override TypedExpression Resolve(GetExpression getExpression)
    {
        var instance = getExpression.Instance.Resolve(this);
        ReclassifyToken(getExpression.TargetField, ReclassifiedTokenTypes.TypeField);
        if (instance.TypeInfo.IsValidNormalPtr && instance.TypeInfo.GenericTypeArgument!.IsStructType)
        {
            try
            {
                var fieldType = instance.TypeInfo.GenericTypeArgument.GetFieldType(getExpression.TargetField);
                return new TypedGetExpression(fieldType, getExpression, instance, getExpression.TargetField, getExpression.ShortCircuitOnNull);
            }
            catch (ParsingException pe)
            {
                _programContext.AddValidationError(pe);
                return new TypedGetExpression(TypeInfo.Void, getExpression, instance, getExpression.TargetField, getExpression.ShortCircuitOnNull);
            }
        }
        _programContext.AddValidationError(new ParsingException(getExpression.Token, $"expect valid pointer type on left hand side of member accessor"));
        return new TypedGetExpression(TypeInfo.Void, getExpression, instance, ReclassifyToken(getExpression.TargetField, ReclassifiedTokenTypes.TypeField), getExpression.ShortCircuitOnNull);
    }

    public override TypedExpression Resolve(IdentifierExpression identifierExpression)
    {
        var foundType = CurrentFunctionTarget.Parameters.Find(x => x.Name.Lexeme == identifierExpression.Token.Lexeme)?.TypeInfo;
        if (foundType != null)
        {
            ReclassifyToken(identifierExpression.Token, ReclassifiedTokenTypes.Parameter);
        }
        else if (_localVariableTypeMap.TryGetValue(identifierExpression.Token.Lexeme, out foundType))
        {
            ReclassifyToken(identifierExpression.Token, ReclassifiedTokenTypes.Variable);
        }
        else
        {
            if (_functionDefinitions.TryGetValue(identifierExpression.Token.Lexeme, out var functionWithMatchingName))
            {
                ReclassifyToken(identifierExpression.Token, ReclassifiedTokenTypes.Function);
                return new TypedFunctionPointerExpression(functionWithMatchingName.GetFunctionPointerType(), identifierExpression, functionWithMatchingName.FunctionName, false); // identifiers only reference the function address so they can be used as lambdas
            }
            if (_importedFunctionDefinitions.TryGetValue(identifierExpression.Token.Lexeme, out var importedFunctionWithMatchingName))
            {
                ReclassifyToken(identifierExpression.Token, ReclassifiedTokenTypes.ImportedFunction);
                return new TypedFunctionPointerExpression(importedFunctionWithMatchingName.GetFunctionPointerType(), identifierExpression, importedFunctionWithMatchingName.FunctionName, true);
            }
        }
        if (foundType == null)
        {
            _programContext.AddValidationError(new ParsingException(identifierExpression.Token, $"unresolved symbol {identifierExpression.Token.Lexeme}"));
            foundType = TypeInfo.Void;
        }

        return new TypedIdentifierExpression(foundType, identifierExpression, identifierExpression.Token);
    }

    public override TypedExpression Resolve(InlineAssemblyExpression inlineAssemblyExpression)
    {
        return new TypedInlineAssemblyExpression(TypeInfo.Void, inlineAssemblyExpression, inlineAssemblyExpression.AssemblyInstruction);
    }

    public override TypedExpression Resolve(LiteralExpression literalExpression)
    {
        if (literalExpression.Value == null)
            return new TypedLiteralExpression(TypeInfo.Pointer(TypeInfo.Void), literalExpression, null);
        if (literalExpression.Value.GetType() == typeof(string))
            return new TypedLiteralExpression(TypeInfo.String, literalExpression, literalExpression.Value);
        if (literalExpression.Value.GetType() == typeof(int))
            return new TypedLiteralExpression(TypeInfo.Integer, literalExpression, literalExpression.Value);
        if (literalExpression.Value.GetType() == typeof(float))
            return new TypedLiteralExpression(TypeInfo.Float, literalExpression, literalExpression.Value);
        _programContext.AddValidationError(new ParsingException(literalExpression.Token, $"unsupported literal type {literalExpression.Value.GetType().Name}"));
        return new TypedLiteralExpression(TypeInfo.Void , literalExpression, literalExpression.Value);
    }

    public override TypedExpression Resolve(LocalVariableExpression localVariableExpression)
    {
        var typeInfo = Resolve(localVariableExpression.TypeSymbol);
        if (!typeInfo.IsStackAllocatable)
            _programContext.AddValidationError(new ParsingException(localVariableExpression.Token, $"unable to create local variable of type {typeInfo}"));
        if (CurrentFunctionTarget.Parameters.Any(x => x.Name.Lexeme == localVariableExpression.Identifier.Lexeme))
            _programContext.AddValidationError(new ParsingException(localVariableExpression.Identifier, $"symbol {localVariableExpression.Identifier.Lexeme} is already defined as a parameter"));
        if (_localVariableTypeMap.ContainsKey(localVariableExpression.Identifier.Lexeme))
            _programContext.AddValidationError(new ParsingException(localVariableExpression.Identifier, $"symbol {localVariableExpression.Identifier.Lexeme} is already defined"));
        _localVariableTypeMap.Add(localVariableExpression.Identifier.Lexeme, typeInfo);
        var initializer = localVariableExpression.Initializer?.Resolve(this);
        if (initializer != null && !initializer.TypeInfo.Equals(typeInfo))
            _programContext.AddValidationError(new ParsingException(localVariableExpression.Identifier, $"expect initializer value of type {typeInfo} but got {initializer.TypeInfo}"));
        ReclassifyToken(localVariableExpression.Identifier, ReclassifiedTokenTypes.Variable);
        return new TypedLocalVariableExpression(TypeInfo.Void, localVariableExpression, localVariableExpression.Identifier, typeInfo, initializer);
    }

    public override TypedExpression Resolve(ReturnExpression returnExpression)
    {
        var returnValue = returnExpression.ReturnValue?.Resolve(this);
        if (CurrentFunctionTarget.ReturnType.Is(IntrinsicType.Void))
        {
            if (returnValue != null) _programContext.AddValidationError(new ParsingException(returnExpression.Token, $"unable to return value for function with return type of {IntrinsicType.Void}"));
            return new TypedReturnExpression(TypeInfo.Void, returnExpression, null);
        }
        if (returnValue == null)
            _programContext.AddValidationError(new ParsingException(returnExpression.Token, $"expected return type to match function return type of {CurrentFunctionTarget.ReturnType} but got None"));
        else if (!returnValue.TypeInfo.Equals(CurrentFunctionTarget.ReturnType))
            _programContext.AddValidationError(new ParsingException(returnExpression.Token, $"expected return type to match function return type of {CurrentFunctionTarget.ReturnType} but got {returnValue.TypeInfo}"));
        return new TypedReturnExpression(TypeInfo.Void, returnExpression, returnValue);
    }

    public override TypedExpression Resolve(SetExpression setExpression)
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
        else
        {
            _programContext.AddValidationError(new ParsingException(setExpression.Token, $"expect assignment target to be identifier or member accessor"));
            setTarget = setExpression.AssignmentTarget.Resolve(this);
        }
        var valueToAssign = setExpression.ValueToAssign.Resolve(this);
        if (!valueToAssign.TypeInfo.IsStackAllocatable)
            _programContext.AddValidationError(new ParsingException(setExpression.ValueToAssign.Token, $"invalid value transfer (type {valueToAssign.TypeInfo})"));
        if (!setTarget.TypeInfo.Equals(valueToAssign.TypeInfo))
            _programContext.AddValidationError(new ParsingException(setExpression.Token, $"type mismatch: expected assignment value to be of type {setTarget.TypeInfo} but got {valueToAssign.TypeInfo}"));
        return new TypedSetExpression(setTarget.TypeInfo, setExpression, setTarget, valueToAssign);
    }
    public override TypedExpression Resolve(CastExpression castExpression)
    {
        // We will trust the programmer on casts and only disallow casting to struct types bigger than 4 bytes
        var resolvedExpression = castExpression.Expression.Resolve(this);
        var typeInfo = Resolve(castExpression.TypeSymbol);
        if (typeInfo.IsStackAllocatable || typeInfo.SizeInMemory() == 4 && resolvedExpression.TypeInfo.SizeInMemory() == 4)
        {
            resolvedExpression.TypeInfo = typeInfo;
            return resolvedExpression;
        }
        _programContext.AddValidationError(new ParsingException(castExpression.Token, $"unable to cast type {resolvedExpression.TypeInfo} to type {typeInfo}"));
        resolvedExpression.TypeInfo = typeInfo;
        return resolvedExpression; 
    }



}