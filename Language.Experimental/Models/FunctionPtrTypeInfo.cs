using Language.Experimental.Constants;
using System.Runtime.InteropServices;


namespace Language.Experimental.Models;


public class FunctionPtrTypeInfo: TypeInfo
{
    public List<TypeInfo> ParameterTypes { get; private set; }
    public TypeInfo ReturnType { get; private set; }
    public FunctionPtrTypeInfo(IntrinsicType intrinsicType, List<TypeInfo> genericTypeArguments) : base(intrinsicType, null)
    {
        ValidateIntrinsicType();
        if (genericTypeArguments.Count == 0) throw new InvalidOperationException($"expect at least 1 type argument for type {IntrinsicType}");
        if (genericTypeArguments.Count == 1)
        {
            ReturnType = genericTypeArguments[0];
            ParameterTypes = new List<TypeInfo>();
        }else
        {
            ReturnType = genericTypeArguments.Last();
            ParameterTypes = genericTypeArguments.GetRange(0, genericTypeArguments.Count - 1);
        }
    }

    public FunctionPtrTypeInfo(IntrinsicType intrinsicType, TypeInfo returnType, List<TypeInfo> parameterTypes) : base(intrinsicType, null)
    {
        ValidateIntrinsicType();
        ReturnType = returnType;
        ParameterTypes = parameterTypes;
    }

    private void ValidateIntrinsicType()
    {
        var isValid = IntrinsicType == IntrinsicType.StdCall_Function_Ptr
            || IntrinsicType == IntrinsicType.StdCall_Function_Ptr_Internal
            || IntrinsicType == IntrinsicType.StdCall_Function_Ptr_External
            || IntrinsicType == IntrinsicType.Cdecl_Function_Ptr
            || IntrinsicType == IntrinsicType.Cdecl_Function_Ptr_Internal
            || IntrinsicType == IntrinsicType.Cdecl_Function_Ptr_External;
        if (!isValid) throw new InvalidOperationException($"cannot initialize FunctionPtrTypeInfo with type {IntrinsicType}");
    }
    public override TypeInfo FunctionReturnType => ReturnType;
    public override List<TypeInfo> FunctionParameterTypes => ParameterTypes;
    public override bool IsFunctionPtr => true;

    public override bool IsInternalFnPtr => IntrinsicType == IntrinsicType.StdCall_Function_Ptr_Internal
                                    || IntrinsicType == IntrinsicType.StdCall_Function_Ptr_Internal
                                    || IntrinsicType == IntrinsicType.Cdecl_Function_Ptr_Internal;

    public override CallingConvention CallingConvention => _stdcallTypes.Contains(IntrinsicType) ? CallingConvention.StdCall :
                                                   (_cdeclTypes.Contains(IntrinsicType) ? CallingConvention.Cdecl : throw new InvalidOperationException($"unable to determine calling convention of {IntrinsicType}"));

    private static List<IntrinsicType> _stdcallTypes => [IntrinsicType.StdCall_Function_Ptr, IntrinsicType.StdCall_Function_Ptr_External, IntrinsicType.StdCall_Function_Ptr_Internal];
    private static List<IntrinsicType> _cdeclTypes => [IntrinsicType.Cdecl_Function_Ptr, IntrinsicType.Cdecl_Function_Ptr_External, IntrinsicType.Cdecl_Function_Ptr_Internal];

    public FunctionPtrTypeInfo AsReference()
    {
        if (_stdcallTypes.Contains(IntrinsicType)) return new FunctionPtrTypeInfo(IntrinsicType.StdCall_Function_Ptr, ReturnType, ParameterTypes);
        return new FunctionPtrTypeInfo(IntrinsicType.Cdecl_Function_Ptr, ReturnType, ParameterTypes);
    }

    public override int GetHashCode()
    {
        return IntrinsicType.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is FunctionPtrTypeInfo fnPtrTypeInfo)
        {
            if (fnPtrTypeInfo.ParameterTypes.Count != ParameterTypes.Count) return false;
            for (int i = 0; i < ParameterTypes.Count; i++)
            {
                if (!ParameterTypes[i].Equals(fnPtrTypeInfo.ParameterTypes[i])) return false;
            }
            return IntrinsicType == fnPtrTypeInfo.IntrinsicType
                    && ReturnType.Equals(fnPtrTypeInfo.ReturnType);
                
        }
        return false;
    }


    public override string ToString()
    {
        var allTypeArguments = ParameterTypes.Select(x => x).ToList();
        allTypeArguments.Add(ReturnType);
        return $"{IntrinsicType}[{string.Join(",", allTypeArguments.Select(x => x.ToString()))}]";
    }
}