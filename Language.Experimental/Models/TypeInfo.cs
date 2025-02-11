using Language.Experimental.Constants;
using Language.Experimental.Parser;
using ParserLite.Exceptions;
using System.Runtime.InteropServices;
using TokenizerCore.Interfaces;
using TokenizerCore.Model;
using TokenizerCore.Models.Constants;


namespace Language.Experimental.Models;


public class TypeInfo
{
    public IntrinsicType IntrinsicType { get; set; }
    public TypeInfo? GenericTypeArgument { get; set; }
    public TypeInfo(IntrinsicType intrinsicType, TypeInfo? genericTypeArgument)
    {
        IntrinsicType = intrinsicType;
        GenericTypeArgument = genericTypeArgument;
    }

    private void ValidateIntrinsicType()
    {
        var isInvalid = IntrinsicType == IntrinsicType.Func
            || IntrinsicType == IntrinsicType.CFunc
            || IntrinsicType == IntrinsicType.Struct;
        if (isInvalid) throw new InvalidOperationException($"cannot initialize TypeInfo with type {IntrinsicType}");
        if (IntrinsicType == IntrinsicType.Ptr && GenericTypeArgument == null) throw new InvalidOperationException($"type {IntrinsicType} requires one type argument");
    }

    public virtual int SizeInMemory() => 4;
    public virtual int StackSize() => 4;
    public virtual bool IsStackAllocatable => IntrinsicType != IntrinsicType.Struct && IntrinsicType != IntrinsicType.Void;
    public static TypeInfo Integer => new TypeInfo(IntrinsicType.Int, null);
    public static TypeInfo Float => new TypeInfo(IntrinsicType.Float, null);
    public static TypeInfo String => new TypeInfo(IntrinsicType.String, null);
    public static TypeInfo Boolean => new TypeInfo(IntrinsicType.Boolean, null);
    public static TypeInfo Pointer(TypeInfo pointedToType) => new TypeInfo(IntrinsicType.Ptr, pointedToType);
    public static TypeInfo Void => new TypeInfo(IntrinsicType.Void, null);

    public bool Is(IntrinsicType type) => IntrinsicType == type;
    public bool IsValidNormalPtr => IntrinsicType == IntrinsicType.Ptr && GenericTypeArgument != null;
    public virtual bool IsStructType => false;
    public virtual bool IsFunctionPtr => false;
    public virtual bool IsInternalFnPtr => false;
    public virtual bool IsExternalFnPtr => false;
    public virtual TypeInfo FunctionReturnType => throw new InvalidOperationException("type is not a function pointer");
    public virtual List<TypeInfo> FunctionParameterTypes => throw new InvalidOperationException("type is not a function pointer and does not contain parameters");
    public virtual CallingConvention CallingConvention => throw new InvalidOperationException($"unable to determine calling convention of {IntrinsicType}");
    public virtual int GetFieldOffset(IToken fieldName) => throw new InvalidOperationException($"type is not a struct type and does not contain any members");
    public virtual TypeInfo GetFieldType(IToken fieldName) => throw new InvalidOperationException($"type is not a struct type and does not contain any members");
    public override int GetHashCode()
    {
        return IntrinsicType.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is TypeInfo typeInfo)
        {
            if (GenericTypeArgument == null && typeInfo.GenericTypeArgument != null) return false;
            if (GenericTypeArgument != null && typeInfo.GenericTypeArgument == null) return false;
            if (GenericTypeArgument == null && typeInfo.GenericTypeArgument == null) return IntrinsicType == typeInfo.IntrinsicType;
            return IntrinsicType == typeInfo.IntrinsicType
                    && GenericTypeArgument!.Equals(typeInfo.GenericTypeArgument);

        }
        return false;
    }

    public override string ToString()
    {
        if (GenericTypeArgument != null) return $"{IntrinsicType}[{GenericTypeArgument}]";
        return $"{IntrinsicType}";
    }

    public virtual bool TryExtractGenericArgumentTypes(Dictionary<TypeSymbol, TypeInfo> genericParameterToArgumentTypeMap, TypeSymbol parameterType)
    {
        // Note parameter type is the actual function parameter type, not the type parameter type
        if (parameterType.IsGenericTypeSymbol)
        {
            if (genericParameterToArgumentTypeMap.TryGetValue(parameterType, out var resolvedTypeArgument))
            {
                if (!resolvedTypeArgument.Equals(this)) return false;
                return true;
            }
            genericParameterToArgumentTypeMap[parameterType] = this;
            return true;
        }
        if (parameterType.TypeName.Lexeme != IntrinsicType.ToString().ToLower()) return false;
        if (GenericTypeArgument != null)
        {
            if (parameterType.TypeArguments.Count == 1) return GenericTypeArgument.TryExtractGenericArgumentTypes(genericParameterToArgumentTypeMap, parameterType.TypeArguments[0]);
            return false;
        }
        return parameterType.TypeArguments.Count == 0;
    }

    public virtual TypeSymbol ToTypeSymbol()
    {
        return new TypeSymbol(CreateToken(IntrinsicType.ToString().ToLower()), GenericTypeArgument == null ? [] : [GenericTypeArgument.ToTypeSymbol()]);
    }
    protected static IToken CreateToken(string lexeme) => new Token(TokenTypes.IntrinsicType, lexeme, Location.Zero, Location.Zero);
}