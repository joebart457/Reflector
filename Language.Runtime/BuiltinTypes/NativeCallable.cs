using Language.Runtime.CustomAttributes;
using Language.Runtime.Exceptions;
using Language.Runtime.Extensions;
using Language.Runtime.Interfaces;
using System.Reflection;
using TokenizerCore.Model;

namespace Language.Runtime.BuiltinTypes;


public class NativeCallable : ICallable
{
    public object? Instance { get; private set; }
    public string FullName { get; private set; }
    public string Name { get; private set; }
    public MethodInfo MethodInfo { get; private set; }
    public List<Parameter> Parameters => MethodInfo.GetParameters().Select(p => new Parameter(p.Name ?? "_unnamed_", p.ParameterType)).ToList();
    public NativeCallable(object? instance, string fullName, string name, MethodInfo methodInfo)
    {
        Instance = instance;
        FullName = fullName;
        Name = name;
        MethodInfo = methodInfo;
    }

    public NativeCallable(string fullName, string name, MethodInfo methodInfo)
    {
        Instance = null;
        FullName = fullName;
        Name = name;
        MethodInfo = methodInfo;
    }

    public object? Invoke(RuntimeContext context, List<object?> args)
    {
        var parameters = MethodInfo.GetParameters();
        var cleanedArguments = new List<object?>();

        if (parameters.Length > 0 && parameters[0].GetCustomAttribute<RuntimeInsertedAttribute>() != null && parameters[0].ParameterType == typeof(RuntimeContext))
        {
            // Inject the runtime context if the native function calls for it
            args.Insert(0, context);
        }

        if (args.Count > parameters.Length && parameters.LastOrDefault()?.IsDefined(typeof(ParamArrayAttribute), false) != true)
            throw new RuntimeException(new Token(FullName, Name, -1, -1), $"parity mismatch in call {FullName}; expected {parameters.Length} argument(s) but got {args.Count}");

        for (int i = 0; i < parameters.Length; i++)
        {

            if (i == parameters.Length - 1)
            {
                var vParam = parameters.LastOrDefault();
                if (vParam?.IsDefined(typeof(ParamArrayAttribute), false) == true)
                {
                    var typeToMatch = vParam.ParameterType.GetElementType() ?? throw new Exception($"internal exception during call {FullName}; unable to determine element type of variadic parameter");
                    var vArgumentType = typeof(List<>).MakeGenericType(typeToMatch);
                    var varidaicArgument = Activator.CreateInstance(vArgumentType);
                    var pushMethod = vArgumentType.GetMethod("Add") ?? throw new Exception($"internal exception during call {FullName}; cannot create variadic push method");
                    var toArrayMethod = vArgumentType.GetMethod("ToArray") ?? throw new Exception($"internal exception during call {FullName}; cannot create variadic array method"); ;
                    if (varidaicArgument == null)
                        throw new RuntimeException(new Token(FullName, Name, -1, -1), $"failed to resolve variadic arguments in call {FullName}");
                    for (; i < args.Count; i++)
                    {
                        if (!typeToMatch.IsCompatibleType(args[i]?.GetType()))
                            throw new RuntimeException(new Token(FullName, Name, -1, -1), $"argument type mismatch in call {FullName}; expected varidaic argument[{i}] to be of type {typeToMatch.UndecoratedName()} but got {args[i]?.GetType().UndecoratedName()}");
                        pushMethod.Invoke(varidaicArgument, new object?[] { args[i] });
                    }
                    cleanedArguments.Add(toArrayMethod.Invoke(varidaicArgument, new object?[] { }));
                    break;
                }
            }
            if (i >= args.Count)
            {
                if (parameters[i].HasDefaultValue)
                {
                    cleanedArguments.Add(parameters[i].DefaultValue);
                    continue;
                }
                throw new RuntimeException(new Token(FullName, Name, -1, -1), $"parity mismatch in call {FullName}; expected {parameters.Length} argument(s) but got {args.Count}");
            }
            if (parameters[i].IsCompatibleType(args[i]?.GetType()))
            {
                cleanedArguments.Add(args[i]);
            }
            else throw new RuntimeException(new Token(FullName, Name, -1, -1), $"argument type mismatch in call {FullName}; expected argument[{i}] to be of type {parameters[i].ParameterType.UndecoratedName()} but got {args[i]?.GetType().UndecoratedName()}");
        }
        try
        {
            return MethodInfo.Invoke(Instance, cleanedArguments.ToArray());
        }
        catch (RuntimeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeException(new Token(FullName, Name, -1, -1), $"internal error in call {FullName}", ex);
        }
    }
}