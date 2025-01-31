
using Language.Runtime.BuiltinTypes;
using System.Reflection;

namespace Language.Runtime.Extensions;

internal static class TypeExtensions
{
    public static string UndecoratedName(this Type type)
    {
        if (type.GetGenericArguments().Length == 0)
        {
            return type.Name;
        }
        var genericArguments = type.GetGenericArguments();
        var typeDefinition = type.Name;
        var unmangledName = typeDefinition.Substring(0, typeDefinition.IndexOf("`"));
        return unmangledName + "<" + String.Join(",", genericArguments.Select(UndecoratedName)) + ">";
    }

    public static bool IsCompatibleType(this Type type, Type? typeToAssign)
    {
        if (typeToAssign == null)
        {
            if (Nullable.GetUnderlyingType(type) != null) return true;
            if (type.IsByRef || type == typeof(object)) return true;
            return false;
        }
        return type.IsAssignableFrom(typeToAssign);
    }

    public static bool IsCompatibleType(this ParameterInfo parameterInfo, Type? typeToAssign)
    {
        if (typeToAssign == null)
        {
            return new NullabilityInfoContext().Create(parameterInfo).WriteState is NullabilityState.Nullable;
        }
        return parameterInfo.ParameterType.IsAssignableFrom(typeToAssign);
    }
}