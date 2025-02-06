using Language.Experimental.Constants;
using System.Runtime.InteropServices;
using TokenizerCore.Interfaces;


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
        var isInvalid = IntrinsicType == IntrinsicType.StdCall_Function_Ptr
            || IntrinsicType == IntrinsicType.StdCall_Function_Ptr_Internal
            || IntrinsicType == IntrinsicType.StdCall_Function_Ptr_External
            || IntrinsicType == IntrinsicType.Cdecl_Function_Ptr
            || IntrinsicType == IntrinsicType.Cdecl_Function_Ptr_Internal
            || IntrinsicType == IntrinsicType.Cdecl_Function_Ptr_External
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

}