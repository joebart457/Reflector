using Language.Experimental.Constants;


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

    public static TypeInfo Integer => new TypeInfo(IntrinsicType.Int, null);
    public static TypeInfo Float => new TypeInfo(IntrinsicType.Float, null);
    public static TypeInfo String => new TypeInfo(IntrinsicType.String, null);
    public static TypeInfo Boolean => new TypeInfo(IntrinsicType.Boolean, null);
    public static TypeInfo Pointer => new TypeInfo(IntrinsicType.Ptr, null);
    public static TypeInfo Function(TypeInfo returnType) => new TypeInfo(IntrinsicType.Function, returnType);
}