using Language.Experimental.Constants;
using Language.Experimental.Parser;
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

    private void ValidateIntrinsicType()
    {
        var isValid = IntrinsicType == IntrinsicType.Func
            || IntrinsicType == IntrinsicType.CFunc;
        if (!isValid) throw new InvalidOperationException($"cannot initialize FunctionPtrTypeInfo with type {IntrinsicType}");
    }
    public List<TypeInfo> GetAllTypeArguments()
    {
        var parameterTypes = ParameterTypes.Select(x => x).ToList();
        parameterTypes.Add(ReturnType);
        return parameterTypes;
    }
    public override TypeInfo FunctionReturnType => ReturnType;
    public override List<TypeInfo> FunctionParameterTypes => ParameterTypes;
    public override bool IsFunctionPtr => true;
    public override CallingConvention CallingConvention => IntrinsicType == IntrinsicType.Func ? CallingConvention.StdCall : CallingConvention.Cdecl;
                                                  
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
        return $"{IntrinsicType.ToString().ToLower()}[{string.Join(",", allTypeArguments.Select(x => x.ToString()))}]";
    }

    public override bool TryExtractGenericArgumentTypes(Dictionary<TypeSymbol, TypeInfo> genericParameterToArgumentTypeMap, TypeSymbol parameterType)
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
        var allTypeArguments = GetAllTypeArguments();
        if (allTypeArguments.Count != parameterType.TypeArguments.Count) return false;
        for (int i = 0; i < allTypeArguments.Count; i++)
        {
            if (!allTypeArguments[i].TryExtractGenericArgumentTypes(genericParameterToArgumentTypeMap, parameterType.TypeArguments[i])) return false;
        }
        return true;
    }

    public override TypeSymbol ToTypeSymbol()
    {
        return new TypeSymbol(CreateToken(IntrinsicType.ToString().ToLower()), GetAllTypeArguments().Select(x => x.ToTypeSymbol()).ToList());
    }
}