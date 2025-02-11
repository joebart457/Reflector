namespace Language.Experimental.Constants;

public enum IntrinsicType
{
    Void,
    Int,
    Float,
    String,
    Boolean,
    Ptr,
    Func,          // Indirect function ptr IE (var fn<int> x) (set x myFunction)
    CFunc,          // Indirect function ptr IE (var fn<int> x) (set x myFunction)
    Struct,
}