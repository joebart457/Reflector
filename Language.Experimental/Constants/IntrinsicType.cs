namespace Language.Experimental.Constants;

public enum IntrinsicType
{
    Void,
    Int,
    Float,
    String,
    Boolean,
    Ptr,
    StdCall_Function_Ptr,          // Indirect function ptr IE (var fn<int> x) (set x myFunction)
    Cdecl_Function_Ptr,          // Indirect function ptr IE (var fn<int> x) (set x myFunction)
    StdCall_Function_Ptr_Internal,  // normal function defined in code
    Cdecl_Function_Ptr_Internal,  // normal function defined in code
    StdCall_Function_Ptr_External, // imported function from dll
    Cdecl_Function_Ptr_External, // imported function from dll
    Struct,
}