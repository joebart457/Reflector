
using Language.Runtime.BuiltinTypes;
using System.Linq.Expressions;
using System.Reflection;

namespace Language.Runtime.Builders;

public class RuntimeContextBuilder
{
    private Environment<object?> _context;

    public RuntimeContextBuilder()
    {
        _context = new Environment<object?>(null);
    }

    public RuntimeContextBuilder(Environment<object?>? baseContext)
    {
        _context = baseContext ?? new Environment<object?>(null);
    }

    public RuntimeContextBuilder(string moduleName)
    {
        _context = new Environment<object?>(moduleName, null);
    }
    public Environment<object?> EmitEnvironment()
    {
        var context = _context;
        _context = new Environment<object?>(null);
        return context;
    }

    public RuntimeContext EmitContext()
    {
        return new RuntimeContext(EmitEnvironment());
    }

    public RuntimeContextBuilder CreateModule(string name)
    {
        var module = new RuntimeContextBuilder(name);
        _context.Define(name, module.EmitEnvironment());
        return module;
    }

    public RuntimeContextBuilder Provide(Expression<Func<object?>> expression)
    {
        var body = expression.Body as MethodCallExpression;

        var ctorBody = expression.Body as NewExpression;
        if (ctorBody == null)
        {
            body ??= ((UnaryExpression)expression.Body).Operand as MethodCallExpression;
            if (body != null)
            {
                Define(body.Method);
                return this;
            }
            throw new ArgumentException($"Expression '{expression}' is not a method call or new expression");
        }
        else throw new ArgumentException("constructor calls not supported");
    }

    public RuntimeContextBuilder Provide(string nameOverride, Expression<Func<object?>> expression)
    {
        var body = expression.Body as MethodCallExpression;

        var ctorBody = expression.Body as NewExpression;
        if (ctorBody == null)
        {
            body ??= ((UnaryExpression)expression.Body).Operand as MethodCallExpression;
            if (body != null)
            {
                Define(nameOverride, body.Method);
                return this;
            }
            throw new ArgumentException($"Expression '{expression}' is not a method call or new expression");
        }
        else throw new ArgumentException("constructor calls not supported");
    }

    public RuntimeContextBuilder Provide(Expression<Action> expression)
    {
        var body = expression.Body as MethodCallExpression;

        body ??= ((UnaryExpression)expression.Body).Operand as MethodCallExpression;

        if (body == null)
        {
            throw new ArgumentException($"Expression '{expression}' is not a method call");
        }

        Define(body.Method);

        return this;
    }

    public RuntimeContextBuilder Provide(string nameOverride, Expression<Action> expression)
    {
        var body = expression.Body as MethodCallExpression;

        body ??= ((UnaryExpression)expression.Body).Operand as MethodCallExpression;

        if (body == null)
        {
            throw new ArgumentException($"Expression '{expression}' is not a method call");
        }

        Define(nameOverride, body.Method);

        return this;
    }



    public RuntimeContextBuilder ProvideEnum<Ty>() where Ty : struct, Enum
    {
        var module = CreateModule(typeof(Ty).Name);
        var values = Enum.GetValues<Ty>();
        var names = Enum.GetNames<Ty>();
        for (int i = 0; i < names.Length; i++)
        {
            module.ProvideValue(names[i], values[i]);
        }
        return this;
    }

    public RuntimeContextBuilder ProvideValue(string alias, object? value)
    {
        _context.Define(alias, value);
        return this;
    }

    private void Define(MethodInfo methodInfo)
    {
        var fullPath = _context.GetFullPath(methodInfo.Name);
        var nativeMethodInfo = new NativeCallable(fullPath, methodInfo.Name, methodInfo);
        _context.Define(methodInfo.Name, nativeMethodInfo);
    }

    private void Define(string nameOverride, MethodInfo methodInfo)
    {
        var fullPath = _context.GetFullPath(nameOverride);
        var nativeMethodInfo = new NativeCallable(fullPath, nameOverride, methodInfo);
        _context.Define(nameOverride, nativeMethodInfo);
    }

}