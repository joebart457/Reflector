
using Language.Experimental.Constants;
using Language.Experimental.Expressions;
using Language.Experimental.Models;
using Language.Experimental.Statements;
using Language.Experimental.UnresolvedExpressions;
using Language.Experimental.UnresolvedStatements;
using ParserLite.Exceptions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Parser;

public class TypeSymbolResolver
{
    private Dictionary<string, GenericTypeDefinition> _genericTypeDefinitions = new();
    private Dictionary<TypeSymbol, StructTypeInfo> _resolvedTypes = new();

    private Dictionary<string, GenericFunctionDefinition> _genericFunctionDefinitions = new();
    private Dictionary<string, FunctionDefinition> _resolvedFunctionDefinitions = new();
    public ParsingResult Resolve(UnresolvedParsingResult unresolvedParsingResult)
    {
        _genericFunctionDefinitions = new();
        _resolvedTypes = new();
        _genericFunctionDefinitions = new();
        _resolvedFunctionDefinitions = new();
        var parsingResult = new ParsingResult();
        foreach(var genericTypeDefinition in unresolvedParsingResult.GenericTypeDefinitions)
        {
            GatherSignature(genericTypeDefinition);
        }
        foreach (var genericFunctionDefinition in unresolvedParsingResult.GenericFunctionDefinitions)
        {
            GatherSignature(genericFunctionDefinition);
        }
        foreach (var typeDefinition in unresolvedParsingResult.TypeDefinitions)
        {
            GatherSignature(typeDefinition);
        }
        
        parsingResult.ImportLibraryDefinitions = unresolvedParsingResult.ImportLibraryDefinitions.Select(x => ResolveImportLibraryDefinition(x)).ToList();
        parsingResult.ImportedFunctionDefinitions = unresolvedParsingResult.ImportedFunctionDefinitions.Select(x => ResolveImportedFunctionDefinition(x)).ToList();
        parsingResult.FunctionDefinitions = unresolvedParsingResult.FunctionDefinitions.Select(x => ResolveFunctionDefinition(x)).ToList();
        parsingResult.FunctionDefinitions.AddRange(_resolvedFunctionDefinitions.Values);
        return parsingResult;
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

    internal void AddToResolvedGenericFunctions(UnresolvedFunctionDefinition unresolvedFunctionDefinition)
    {
        if (_resolvedFunctionDefinitions.ContainsKey(unresolvedFunctionDefinition.FunctionName.Lexeme))
            throw new ParsingException(unresolvedFunctionDefinition.FunctionName, $"redefinition of instantiated generic function {unresolvedFunctionDefinition.FunctionName.Lexeme}");
        var functionDefinition = ResolveFunctionDefinition(unresolvedFunctionDefinition);
        _resolvedFunctionDefinitions[unresolvedFunctionDefinition.FunctionName.Lexeme] = functionDefinition;
    }

    internal FunctionDefinition ResolveFunctionDefinition(UnresolvedFunctionDefinition unresolvedFunctionDefinition)
    {
        var returnType = Resolve(unresolvedFunctionDefinition.ReturnType);
        var parameters = unresolvedFunctionDefinition.Parameters.Select(x => new Parameter(x.Name, Resolve(x.TypeSymbol))).ToList();
        var bodyStatements = unresolvedFunctionDefinition.BodyStatements.Select(x => x.Resolve(this)).ToList();
        return new FunctionDefinition(unresolvedFunctionDefinition.FunctionName, returnType, parameters, bodyStatements, unresolvedFunctionDefinition.CallingConvention, unresolvedFunctionDefinition.IsExported, unresolvedFunctionDefinition.ExportedSymbol);
    }

    internal ImportedFunctionDefinition ResolveImportedFunctionDefinition(UnresolvedImportedFunctionDefinition unresolvedImportedFunctionDefinition)
    {
        var returnType = Resolve(unresolvedImportedFunctionDefinition.ReturnType);
        var parameters = unresolvedImportedFunctionDefinition.Parameters.Select(x => new Parameter(x.Name, Resolve(x.TypeSymbol))).ToList();
        return new ImportedFunctionDefinition(unresolvedImportedFunctionDefinition.FunctionName, returnType, parameters, unresolvedImportedFunctionDefinition.CallingConvention, unresolvedImportedFunctionDefinition.LibraryAlias, unresolvedImportedFunctionDefinition.FunctionSymbol);
    }

    internal ImportLibraryDefinition ResolveImportLibraryDefinition(UnresolvedImportLibraryDefinition unresolvedImportLibraryDefinition)
    {
        return new ImportLibraryDefinition(unresolvedImportLibraryDefinition.LibraryAlias, unresolvedImportLibraryDefinition.LibraryPath);
    }

    internal TypeInfo ResolveTypeDefinition(TypeDefinition typeDefinition)
    {
        var typeSymbol = new TypeSymbol(typeDefinition.TypeName, new());
        if (!_resolvedTypes.TryGetValue(typeSymbol, out var foundType))
            throw new ParsingException(typeDefinition.TypeName, $"unable to find type signature {typeSymbol}");
        foreach(var field in typeDefinition.Fields)
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
        if (Enum.TryParse<IntrinsicType>(typeSymbol.TypeName.Lexeme, false, out var intrinsicType))
            return ResolveIntrinsicType(intrinsicType, typeSymbol.TypeName, typeSymbol.TypeArguments);
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
        else if (intrinsicType == IntrinsicType.StdCall_Function_Ptr
            || intrinsicType == IntrinsicType.StdCall_Function_Ptr_Internal
            || intrinsicType == IntrinsicType.StdCall_Function_Ptr_External
            || intrinsicType == IntrinsicType.Cdecl_Function_Ptr
            || intrinsicType == IntrinsicType.Cdecl_Function_Ptr_Internal
            || intrinsicType == IntrinsicType.Cdecl_Function_Ptr_External)
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

    private bool RequiresTypeArgument(IntrinsicType type)
    {
        return type == IntrinsicType.Ptr
            || type == IntrinsicType.StdCall_Function_Ptr
            || type == IntrinsicType.StdCall_Function_Ptr_Internal
            || type == IntrinsicType.StdCall_Function_Ptr_External
            || type == IntrinsicType.Cdecl_Function_Ptr
            || type == IntrinsicType.Cdecl_Function_Ptr_Internal
            || type == IntrinsicType.Cdecl_Function_Ptr_External;
    }
    private bool SupportsMultipleTypeArguments(IntrinsicType type)
    {
        return type == IntrinsicType.StdCall_Function_Ptr
            || type == IntrinsicType.StdCall_Function_Ptr_Internal
            || type == IntrinsicType.StdCall_Function_Ptr_External
            || type == IntrinsicType.Cdecl_Function_Ptr
            || type == IntrinsicType.Cdecl_Function_Ptr_Internal
            || type == IntrinsicType.Cdecl_Function_Ptr_External;
    }

    internal ExpressionBase Resolve(UnresolvedCallExpression unresolvedCallExpression)
    {
        return new CallExpression(unresolvedCallExpression.Token, unresolvedCallExpression.CallTarget.Resolve(this), unresolvedCallExpression.Arguments.Select(x => x.Resolve(this)).ToList());
    }

    internal ExpressionBase Resolve(UnresolvedCastExpression unresolvedCastExpression)
    {
        return new CastExpression(unresolvedCastExpression.Token, Resolve(unresolvedCastExpression.TypeSymbol), unresolvedCastExpression.Expression.Resolve(this));
    }

    internal ExpressionBase Resolve(UnresolvedCompilerIntrinsic_GetExpression unresolvedCompilerIntrinsic_GetExpression)
    {
        return new CompilerIntrinsic_GetExpression(unresolvedCompilerIntrinsic_GetExpression.Token, 
            Resolve(unresolvedCompilerIntrinsic_GetExpression.RetrievedType), unresolvedCompilerIntrinsic_GetExpression.ContextPointer.Resolve(this), unresolvedCompilerIntrinsic_GetExpression.MemberOffset);
    }

    internal ExpressionBase Resolve(UnresolvedCompilerIntrinsic_SetExpression unresolvedCompilerIntrinsic_SetExpression)
    {
        return new CompilerIntrinsic_SetExpression(unresolvedCompilerIntrinsic_SetExpression.Token, 
            unresolvedCompilerIntrinsic_SetExpression.ContextPointer.Resolve(this), unresolvedCompilerIntrinsic_SetExpression.AssignmentOffset, unresolvedCompilerIntrinsic_SetExpression.ValueToAssign.Resolve(this));
    }

    internal ExpressionBase Resolve(UnresolvedGetExpression unresolvedGetExpression)
    {
        return new GetExpression(unresolvedGetExpression.Token, unresolvedGetExpression.Instance.Resolve(this), unresolvedGetExpression.TargetField, unresolvedGetExpression.ShortCircuitOnNull);
    }

    internal ExpressionBase Resolve(UnresolvedIdentifierExpression unresolvedIdentifierExpression)
    {
        return new IdentifierExpression(unresolvedIdentifierExpression.Token);
    }

    internal ExpressionBase Resolve(UnresolvedLambdaExpression unresolvedLambdaExpression)
    {
        return new LambdaExpression(unresolvedLambdaExpression.Token, ResolveFunctionDefinition(unresolvedLambdaExpression.FunctionDefinition));
    }

    internal ExpressionBase Resolve(UnresolvedLiteralExpression unresolvedLiteralExpression)
    {
        return new LiteralExpression(unresolvedLiteralExpression.Token, unresolvedLiteralExpression.Value);
    }

    internal ExpressionBase Resolve(UnresolvedLocalVariableExpression unresolvedLocalVariableExpression)
    {
        return new LocalVariableExpression(unresolvedLocalVariableExpression.Token, Resolve(unresolvedLocalVariableExpression.TypeSymbol), unresolvedLocalVariableExpression.Identifier, unresolvedLocalVariableExpression.Initializer?.Resolve(this));
    }

    internal ExpressionBase Resolve(UnresolvedReturnExpression unresolvedReturnExpression)
    {
        return new ReturnExpression(unresolvedReturnExpression.Token, unresolvedReturnExpression.ReturnValue?.Resolve(this));
    }

    internal ExpressionBase Resolve(UnresolvedSetExpression unresolvedSetExpression)
    {
        return new SetExpression(unresolvedSetExpression.Token, unresolvedSetExpression.AssignmentTarget.Resolve(this), unresolvedSetExpression.ValueToAssign.Resolve(this));
    }

    internal ExpressionBase Resolve(UnresolvedInlineAssemblyExpression unresolvedInlineAssemblyExpression)
    {
        return new InlineAssemblyExpression(unresolvedInlineAssemblyExpression.Token, unresolvedInlineAssemblyExpression.AssemblyInstruction);
    }

    internal StatementBase Resolve(UnresolvedFunctionDefinition unresolvedFunctionDefinition)
    {
        return ResolveFunctionDefinition(unresolvedFunctionDefinition);
    }

    internal StatementBase Resolve(UnresolvedImportedFunctionDefinition unresolvedImportedFunctionDefinition)
    {
       return ResolveImportedFunctionDefinition(unresolvedImportedFunctionDefinition);
    }

    internal StatementBase Resolve(UnresolvedImportLibraryDefinition unresolvedImportLibraryDefinition)
    {
        return ResolveImportLibraryDefinition(unresolvedImportLibraryDefinition);
    }

    internal ExpressionBase Resolve(GenericFunctionReferenceExpression genericFunctionReferenceExpression)
    {
        var symbol = $"{genericFunctionReferenceExpression.Identifier.Lexeme}!{string.Join('_', genericFunctionReferenceExpression.TypeArguments.Select(x => x.GetFlattenedName()))}";
        if (_resolvedFunctionDefinitions.TryGetValue(symbol, out var resolvedFunctionDefinition)) return new IdentifierExpression(resolvedFunctionDefinition.FunctionName);
        if (!_genericFunctionDefinitions.TryGetValue(genericFunctionReferenceExpression.Identifier.Lexeme, out var genericFunctionDefinition))
            throw new ParsingException(genericFunctionReferenceExpression.Identifier, $"unresolved symbol to generic function definition {genericFunctionReferenceExpression.Identifier.Lexeme}");
        var functionDefinition = genericFunctionDefinition.ToFunctionDefinition(genericFunctionReferenceExpression.TypeArguments);
        AddToResolvedGenericFunctions(functionDefinition);
        return new IdentifierExpression(functionDefinition.FunctionName);
    }
}