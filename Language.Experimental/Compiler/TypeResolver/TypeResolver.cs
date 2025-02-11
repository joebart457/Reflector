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
    private Dictionary<string, GenericTypeDefinition> _genericTypeDefinitions = new();
    private Dictionary<TypeSymbol, StructTypeInfo> _resolvedTypes = new();

    private Dictionary<string, GenericFunctionDefinition> _genericFunctionDefinitions = new();
    private Dictionary<string, TypedFunctionDefinition> _resolvedFunctionDefinitions = new();

    private TypedFunctionDefinition? _currentFunctionTarget;
    private TypedFunctionDefinition CurrentFunctionTarget => _currentFunctionTarget ?? throw new ArgumentNullException(nameof(CurrentFunctionTarget));
    private Dictionary<string, TypeInfo> _localVariableTypeMap = new();
    private Dictionary<string, TypedFunctionDefinition> _functionDefinitions = new();
    private Dictionary<string, TypedImportedFunctionDefinition> _importedFunctionDefinitions = new();
    private Dictionary<string, TypedImportLibraryDefinition> _importLibraries = new();
    private List<TypedFunctionDefinition> _lambdaFunctions = new();
    public TypeResolverResult Resolve(ParsingResult parsingResult)
    {      
        GatherSignatures(parsingResult);
        var result = new TypeResolverResult();
        foreach(var statement in parsingResult.ImportLibraryDefinitions)
        {
            result.ImportLibraries.Add((TypedImportLibraryDefinition)statement.Resolve(this));
        }
        foreach (var statement in parsingResult.ImportedFunctionDefinitions)
        {
            result.ImportedFunctions.Add((TypedImportedFunctionDefinition)statement.Resolve(this));
        }
        foreach (var statement in parsingResult.FunctionDefinitions)
        {
            result.Functions.Add((TypedFunctionDefinition)statement.Resolve(this));
        }
        result.Functions.AddRange(_lambdaFunctions);
        result.Functions.AddRange(_resolvedFunctionDefinitions.Values);
        return result;
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
            statement.GatherSignature(this);
        }
        foreach (var statement in parsingResult.GenericTypeDefinitions)
        {
            statement.GatherSignature(this);
        }
        foreach (var statement in parsingResult.GenericFunctionDefinitions)
        {
            statement.GatherSignature(this);
        }
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

    internal void GatherSignature(TypeDefinition typeDefinition)
    {
        var typeSymbol = new TypeSymbol(typeDefinition.TypeName, new());
        if (_resolvedTypes.ContainsKey(typeSymbol))
            throw new ParsingException(typeDefinition.TypeName, $"redefinition of named type {typeDefinition.TypeName.Lexeme}");
        _resolvedTypes[typeSymbol] = new StructTypeInfo(typeDefinition.TypeName, new());
    }

    internal void GatherSignature(GenericTypeDefinition genericTypeDefinition)
    {
        if (_genericTypeDefinitions.ContainsKey(genericTypeDefinition.TypeName.Lexeme))
            throw new ParsingException(genericTypeDefinition.TypeName, $"redefinition of named generic type {genericTypeDefinition.TypeName.Lexeme}");
        _genericTypeDefinitions[genericTypeDefinition.TypeName.Lexeme] = genericTypeDefinition;
    }

    internal void GatherSignature(GenericFunctionDefinition genericFunctionDefinition)
    {
        if (_genericFunctionDefinitions.ContainsKey(genericFunctionDefinition.FunctionName.Lexeme))
            throw new ParsingException(genericFunctionDefinition.FunctionName, $"redefinition of named generic function {genericFunctionDefinition.FunctionName.Lexeme}");
        _genericFunctionDefinitions[genericFunctionDefinition.FunctionName.Lexeme] = genericFunctionDefinition;
    }

    internal void GatherSignature(FunctionDefinition functionDefinition)
    {
        if (functionDefinition.Parameters.DistinctBy(x => x.Name.Lexeme).Count() != functionDefinition.Parameters.Count)
            throw new ParsingException(functionDefinition.FunctionName, $"redefinition of parameter name");
        var resolvedParameters = functionDefinition.Parameters.Select(x => new TypedParameter(x.Name, Resolve(x.TypeSymbol))).ToList();

        var invalidParameter = resolvedParameters.Find(x => !x.TypeInfo.IsStackAllocatable);
        if (invalidParameter != null)
            throw new ParsingException(functionDefinition.FunctionName, $"invalid parameter type {invalidParameter.TypeInfo}. Type is not stack allocatable");

        var functionBody = new List<TypedExpression>();

        if (_functionDefinitions.ContainsKey(functionDefinition.FunctionName.Lexeme))
            throw new ParsingException(functionDefinition.FunctionName, $"redefinition of function {functionDefinition.FunctionName.Lexeme}");
        if (_importedFunctionDefinitions.ContainsKey(functionDefinition.FunctionName.Lexeme))
            throw new ParsingException(functionDefinition.FunctionName, $"redefinition of symbol {functionDefinition.FunctionName.Lexeme}");

        _functionDefinitions[functionDefinition.FunctionName.Lexeme] = new TypedFunctionDefinition(functionDefinition.FunctionName, Resolve(functionDefinition.ReturnType), resolvedParameters, functionBody);   
    }

    internal void GatherSignature(ImportedFunctionDefinition importedFunctionDefinition)
    {
        if (importedFunctionDefinition.Parameters.DistinctBy(x => x.Name.Lexeme).Count() != importedFunctionDefinition.Parameters.Count)
            throw new ParsingException(importedFunctionDefinition.FunctionName, $"redefinition of parameter name");
        var resolvedParameters = importedFunctionDefinition.Parameters.Select(x => new TypedParameter(x.Name, Resolve(x.TypeSymbol))).ToList();
        var returnType = Resolve(importedFunctionDefinition.ReturnType);
        if (!returnType.IsStackAllocatable && !returnType.Is(IntrinsicType.Void))
            throw new ParsingException(importedFunctionDefinition.FunctionName, $"invalid size of return. Type is {returnType}");
        var invalidParameter = resolvedParameters.Find(x => !x.TypeInfo.IsStackAllocatable);
        if (invalidParameter != null)
            throw new ParsingException(importedFunctionDefinition.FunctionName, $"invalid parameter type {invalidParameter.TypeInfo}. Type is not stack allocatable");

        if (_functionDefinitions.ContainsKey(importedFunctionDefinition.FunctionName.Lexeme))
            throw new ParsingException(importedFunctionDefinition.FunctionName, $"redefinition of function {importedFunctionDefinition.FunctionName.Lexeme}");
        if (_importedFunctionDefinitions.ContainsKey(importedFunctionDefinition.FunctionName.Lexeme))
            throw new ParsingException(importedFunctionDefinition.FunctionName, $"redefinition of imported symbol {importedFunctionDefinition.FunctionName.Lexeme}");

        if (!_importLibraries.ContainsKey(importedFunctionDefinition.LibraryAlias.Lexeme))
            throw new ParsingException(importedFunctionDefinition.LibraryAlias, $"unable to import function from undefined library '{importedFunctionDefinition.LibraryAlias.Lexeme}'");

        _importedFunctionDefinitions[importedFunctionDefinition.FunctionName.Lexeme] = new TypedImportedFunctionDefinition(importedFunctionDefinition.FunctionName, returnType, resolvedParameters, importedFunctionDefinition.CallingConvention, importedFunctionDefinition.LibraryAlias, importedFunctionDefinition.FunctionSymbol);
    }

    internal void GatherSignature(ImportLibraryDefinition importLibraryDefinition)
    {
        if (_importLibraries.ContainsKey(importLibraryDefinition.LibraryAlias.Lexeme))
            throw new ParsingException(importLibraryDefinition.LibraryAlias, $"import library with alias {importLibraryDefinition.LibraryAlias.Lexeme} is already defined");
        _importLibraries[importLibraryDefinition.LibraryAlias.Lexeme] = new TypedImportLibraryDefinition(importLibraryDefinition.LibraryAlias, importLibraryDefinition.LibraryPath);
    }

    internal void AddToResolvedGenericFunctions(TypedFunctionDefinition typedFunctionDefinition)
    {
        if (_resolvedFunctionDefinitions.ContainsKey(typedFunctionDefinition.FunctionName.Lexeme))
            throw new ParsingException(typedFunctionDefinition.FunctionName, $"redefinition of instantiated generic function {typedFunctionDefinition.FunctionName.Lexeme}");
        _resolvedFunctionDefinitions[typedFunctionDefinition.FunctionName.Lexeme] = typedFunctionDefinition;
    }

    internal TypeInfo ResolveTypeDefinition(TypeDefinition typeDefinition)
    {
        var typeSymbol = new TypeSymbol(typeDefinition.TypeName, new());
        if (!_resolvedTypes.TryGetValue(typeSymbol, out var foundType))
            throw new ParsingException(typeDefinition.TypeName, $"unable to find type signature {typeSymbol}");
        foreach (var field in typeDefinition.Fields)
        {
            foundType.Fields.Add(new(Resolve(field.TypeSymbol), field.Name));
        }
        foundType.ValidateFields();
        return foundType;
    }

    internal TypeInfo Resolve(TypeSymbol typeSymbol)
    {
        if (typeSymbol.IsGenericTypeSymbol)
            throw new ParsingException(typeSymbol.TypeName, $"unable to resolve generic type parameter {typeSymbol} to a concrete type");
        if (typeSymbol.TypeName.Type == TokenTypes.IntrinsicType)
        {
            if (Enum.TryParse<IntrinsicType>(typeSymbol.TypeName.Lexeme, true, out var intrinsicType))
                return ResolveIntrinsicType(intrinsicType, typeSymbol.TypeName, typeSymbol.TypeArguments);
            else throw new ParsingException(typeSymbol.TypeName, $"invalid intrinsic type {typeSymbol}");
        }      
        if (_resolvedTypes.TryGetValue(typeSymbol, out var typeInfo))
            return typeInfo;
        if (typeSymbol.TypeArguments.Any() && _genericTypeDefinitions.TryGetValue(typeSymbol.TypeName.Lexeme, out var genericTypeDefinition))
        {
            if (genericTypeDefinition.GenericTypeParameters.Count != typeSymbol.TypeArguments.Count)
                throw new ParsingException(typeSymbol.TypeName, $"expect {genericTypeDefinition.GenericTypeParameters.Count} type arguments but got {typeSymbol.TypeArguments.Count}");
            var concreteTypeDefinition = genericTypeDefinition.ToConcreteTypeDefinition(typeSymbol.TypeArguments);
            GatherSignature(concreteTypeDefinition);
            return ResolveTypeDefinition(concreteTypeDefinition);
        }
        throw new ParsingException(typeSymbol.TypeName, $"unable to resolve type symbol {typeSymbol}");
    }

    internal TypeInfo ResolveIntrinsicType(IntrinsicType intrinsicType, IToken typeName, List<TypeSymbol> typeArguments)
    {
        if (intrinsicType == IntrinsicType.Ptr)
        {
            if (typeArguments.Count != 1) throw new ParsingException(typeName, "expect exactly one type argument");
            return TypeInfo.Pointer(Resolve(typeArguments[0]));
        }
        else if (intrinsicType == IntrinsicType.Func
            || intrinsicType == IntrinsicType.CFunc)
        {
            if (!typeArguments.Any()) throw new ParsingException(typeName, "expect at least one type argument");
            return new FunctionPtrTypeInfo(intrinsicType, typeArguments.Select(x => Resolve(x)).ToList());
        }
        if (intrinsicType == IntrinsicType.Void)
        {
            if (typeArguments.Any()) throw new ParsingException(typeName, $"type {typeName.Lexeme} does not support any type arguments");
            return TypeInfo.Void;
        }
        if (intrinsicType == IntrinsicType.Int)
        {
            if (typeArguments.Any()) throw new ParsingException(typeName, $"type {typeName.Lexeme} does not support any type arguments");
            return TypeInfo.Integer;
        }
        if (intrinsicType == IntrinsicType.Float)
        {
            if (typeArguments.Any()) throw new ParsingException(typeName, $"type {typeName.Lexeme} does not support any type arguments");
            return TypeInfo.Float;
        }
        if (intrinsicType == IntrinsicType.String)
        {
            if (typeArguments.Any()) throw new ParsingException(typeName, $"type {typeName.Lexeme} does not support any type arguments");
            return TypeInfo.String;
        }
        throw new ParsingException(typeName, $"unsupported intrinsic type {intrinsicType}");
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

    internal TypedExpression Resolve(CallExpression callExpression)
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
                throw new ParsingException(callExpression.Token, $"parity mismatch in call {directCallTarget.FunctionName.Lexeme}: expected {directCallTarget.Parameters.Count} arguments but got {args.Count}");
            for (int i = 0; i < directCallTarget.Parameters.Count; i++)
            {
                if (!directCallTarget.Parameters[i].TypeInfo.Equals(args[i].TypeInfo))
                    throw new ParsingException(callExpression.Token, $"call {directCallTarget.FunctionName.Lexeme}: expected argument to be of type {directCallTarget.Parameters[i].TypeInfo} but got {args[i].TypeInfo}");
            }
            return new TypedDirectCallExpression(directCallTarget.ReturnType, callExpression, directCallTarget, args);
        }

        TypedExpression callTarget = callExpression.CallTarget.Resolve(this);

        if (!callTarget.TypeInfo.IsFunctionPtr) throw new ParsingException(callExpression.Token, $"expect call target to be of type fn[...,t] but got {callTarget.TypeInfo}");
        
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
        var retrievedType = Resolve(compilerIntrinsic_GetExpression.RetrievedType);
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
                return new TypedFunctionPointerExpression(functionWithMatchingName.GetFunctionPointerType(), identifierExpression, functionWithMatchingName.FunctionName, false); // identifiers only reference the function address so they can be used as lambdas
            if (_importedFunctionDefinitions.TryGetValue(identifierExpression.Token.Lexeme, out var importedFunctionWithMatchingName))
                return new TypedFunctionPointerExpression(importedFunctionWithMatchingName.GetFunctionPointerType(), identifierExpression, importedFunctionWithMatchingName.FunctionName, true);
        }
        if (foundType == null)
            throw new ParsingException(identifierExpression.Token, $"unresolved symbol {identifierExpression.Token.Lexeme}");
        return new TypedIdentifierExpression(foundType, identifierExpression, identifierExpression.Token);
    }

    internal ITypedFunctionInfo? ResolveCallTarget(IdentifierExpression identifierExpression)
    {
        // Identifiers that are direct call targets will be handled differently IE
        // (printf msg) 

        if (_functionDefinitions.TryGetValue(identifierExpression.Token.Lexeme, out var functionWithMatchingName))
            return functionWithMatchingName;
        if (_importedFunctionDefinitions.TryGetValue(identifierExpression.Token.Lexeme, out var importedFunctionWithMatchingName))
            return importedFunctionWithMatchingName;
        return null;
    }

    internal ITypedFunctionInfo ResolveCallTarget(GenericFunctionReferenceExpression genericFunctionReferenceExpression, List<TypedExpression> arguments)
    {
        if (genericFunctionReferenceExpression.TypeArguments.Count == 0)
        {
            // we will try to infer the generic type arguments
            if (!_genericFunctionDefinitions.TryGetValue(genericFunctionReferenceExpression.Identifier.Lexeme, out var genericFunction))
                throw new ParsingException(genericFunctionReferenceExpression.Identifier, $"unresolved symbol to generic function definition {genericFunctionReferenceExpression.Identifier.Lexeme}");
            if (arguments.Count != genericFunction.Parameters.Count)
                throw new ParsingException(genericFunctionReferenceExpression.Token, $"expected {genericFunction.Parameters.Count} arguments in call {genericFunction.FunctionName} but got {arguments.Count}");

            // attempt to extract generic type parameters from the argument types
            var genericTypeParameters = genericFunction.GenericTypeParameters;
            var argumentTypes = arguments.Select(x => x.TypeInfo).ToList();
            var genericParametersTypeMap = new Dictionary<TypeSymbol, TypeInfo>();
            for (int i = 0; i < genericFunction.Parameters.Count; i++)
            {
                if (!argumentTypes[i].TryExtractGenericArgumentTypes(genericParametersTypeMap, genericFunction.Parameters[i].TypeSymbol))
                    throw new ParsingException(genericFunctionReferenceExpression.Token, $"unable to resolve generic type arguments for call {genericFunction.FunctionName}");
            }
            var resolvedGenericTypeArguments = new List<TypeSymbol>();
            var missingParameters = new List<TypeSymbol>();
            foreach (var genericTypeParameter in genericFunction.GenericTypeParameters)
            {
                if (!genericParametersTypeMap.TryGetValue(genericTypeParameter, out var resolvedTypeArgument))
                    missingParameters.Add(genericTypeParameter);
                else resolvedGenericTypeArguments.Add(resolvedTypeArgument.ToTypeSymbol());
            }
            if (missingParameters.Any())
                throw new ParsingException(genericFunctionReferenceExpression.Token, $"unable to resolve all parameters for call {genericFunction.FunctionName}. Missing parameters {string.Join(", ", missingParameters.Select(x => x.ToString()))}");
            return ResolveCallTarget(new GenericFunctionReferenceExpression(genericFunctionReferenceExpression.Token, resolvedGenericTypeArguments));
        } 
        else return ResolveCallTarget(genericFunctionReferenceExpression);
    }

    internal ITypedFunctionInfo ResolveCallTarget(GenericFunctionReferenceExpression genericFunctionReferenceExpression)
    {
        var symbol = $"{genericFunctionReferenceExpression.Identifier.Lexeme}!{string.Join('_', genericFunctionReferenceExpression.TypeArguments.Select(x => x.GetFlattenedName()))}";
        if (_resolvedFunctionDefinitions.TryGetValue(symbol, out var resolvedFunctionDefinition)) return resolvedFunctionDefinition;
        if (!_genericFunctionDefinitions.TryGetValue(genericFunctionReferenceExpression.Identifier.Lexeme, out var genericFunctionDefinition))
            throw new ParsingException(genericFunctionReferenceExpression.Identifier, $"unresolved symbol to generic function definition {genericFunctionReferenceExpression.Identifier.Lexeme}");
        var functionDefinition = genericFunctionDefinition.ToFunctionDefinition(genericFunctionReferenceExpression.TypeArguments);
        GatherSignature(functionDefinition);
        var previousFunctionTarget = CurrentFunctionTarget;
        var typedFunctionDefinition = (TypedFunctionDefinition)Resolve(functionDefinition);
        AddToResolvedGenericFunctions(typedFunctionDefinition);
        _currentFunctionTarget = previousFunctionTarget;
        return typedFunctionDefinition;
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
        var typeInfo = Resolve(localVariableExpression.TypeSymbol);
        if (!typeInfo.IsStackAllocatable)
            throw new ParsingException(localVariableExpression.Token, $"unable to create local variable of type {typeInfo}");
        if (CurrentFunctionTarget.Parameters.Any(x => x.Name.Lexeme == localVariableExpression.Identifier.Lexeme))
            throw new ParsingException(localVariableExpression.Identifier, $"symbol {localVariableExpression.Identifier.Lexeme} is already defined as a parameter");
        if (_localVariableTypeMap.ContainsKey(localVariableExpression.Identifier.Lexeme))
            throw new ParsingException(localVariableExpression.Identifier, $"symbol {localVariableExpression.Identifier.Lexeme} is already defined");
        _localVariableTypeMap.Add(localVariableExpression.Identifier.Lexeme, typeInfo);
        var initializer = localVariableExpression.Initializer?.Resolve(this);
        if (initializer != null && !initializer.TypeInfo.Equals(typeInfo))
            throw new ParsingException(localVariableExpression.Identifier, $"expect initializer value of type {typeInfo} but got {initializer.TypeInfo}");
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
        var typeInfo = Resolve(castExpression.TypeSymbol);
        if (typeInfo.IsStackAllocatable || (typeInfo.SizeInMemory() == 4 && resolvedExpression.TypeInfo.SizeInMemory() == 4))
        {
            resolvedExpression.TypeInfo = typeInfo;
            return resolvedExpression;
        }
        throw new ParsingException(castExpression.Token, $"unable to cast type {resolvedExpression.TypeInfo} to type {typeInfo}");
    }

    internal TypedExpression Resolve(GenericFunctionReferenceExpression genericFunctionReferenceExpression)
    {
        var symbol = $"{genericFunctionReferenceExpression.Identifier.Lexeme}!{string.Join('_', genericFunctionReferenceExpression.TypeArguments.Select(x => x.GetFlattenedName()))}";
        if (_resolvedFunctionDefinitions.TryGetValue(symbol, out var resolvedFunctionDefinition)) return new TypedIdentifierExpression(resolvedFunctionDefinition.GetFunctionPointerType(), genericFunctionReferenceExpression, resolvedFunctionDefinition.FunctionName);
        if (!_genericFunctionDefinitions.TryGetValue(genericFunctionReferenceExpression.Identifier.Lexeme, out var genericFunctionDefinition))
            throw new ParsingException(genericFunctionReferenceExpression.Identifier, $"unresolved symbol to generic function definition {genericFunctionReferenceExpression.Identifier.Lexeme}");
        var functionDefinition = genericFunctionDefinition.ToFunctionDefinition(genericFunctionReferenceExpression.TypeArguments);
        GatherSignature(functionDefinition);
        var previousFunctionTarget = CurrentFunctionTarget;
        var typedFunctionDefinition = (TypedFunctionDefinition)Resolve(functionDefinition);
        AddToResolvedGenericFunctions(typedFunctionDefinition);
        _currentFunctionTarget = previousFunctionTarget;
        return Resolve(new IdentifierExpression(typedFunctionDefinition.FunctionName));
    }

    internal TypedExpression Resolve(LambdaExpression lambdaExpression)
    {
        // For lambdas we will simply pull out the function definition and return a reference to the function as an (unique, generated) identifier
        var anonymousToken = GetAnonymousFunctionLabel(lambdaExpression.FunctionDefinition.Token);
        lambdaExpression.FunctionDefinition.FunctionName = anonymousToken;
        GatherSignature(lambdaExpression.FunctionDefinition);
        var flattenedLambda = (TypedFunctionDefinition)Resolve(lambdaExpression.FunctionDefinition);
        _lambdaFunctions.Add(flattenedLambda);
        return Resolve(new IdentifierExpression(anonymousToken));
    }

    private IToken GetAnonymousFunctionLabel(IToken token) => new Token(BuiltinTokenTypes.Word, $"_anonymous__!{AnonymousFunctionIndex++}", token.Start, token.End);

    private int AnonymousFunctionIndex = 0;
}