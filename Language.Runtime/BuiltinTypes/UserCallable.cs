using Language.Parser.Expressions;
using Language.Runtime.Exceptions;
using Language.Runtime.Extensions;
using Language.Runtime.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TokenizerCore.Interfaces;

namespace Language.Runtime.BuiltinTypes;


public class UserCallable : ICallable
{
    public string FullName { get; private set; }
    public IToken Name { get; private set; }
    public List<Parameter> Parameters { get; private set; }
    public List<ExpressionBase> Body { get; private set; }

    private Environment<object?> _closure;
    public UserCallable(Environment<object?> closure, IToken name, List<Parameter> parameters, List<ExpressionBase> body)
    {
        _closure = closure;
        Name = name;
        Parameters = parameters;
        Body = body;
        FullName = _closure.GetFullPath(name.Lexeme);
    }

    public object? Invoke(RuntimeContext context, List<object?> args)
    {
        var previous = _closure;
        try
        {
            _closure = new Environment<object?>(_closure);

            if (args.Count != Parameters.Count)
                throw new RuntimeException(Name, $"parity mismatch in call {FullName}; expected {Parameters.Count} argument(s) but got {args.Count}");
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (!Parameters[i].TypeInfo.IsCompatibleType(args[i]?.GetType()))
                    throw new RuntimeException(Name, $"argument type mismatch in call {FullName}; expected argument[{i}] to be of type {Parameters[i].TypeInfo.UndecoratedName()} but got {args[i]?.GetType().UndecoratedName()}");
                _closure.Define(Parameters[i].Symbol, args[i]);
            }

            var fnRuntime = new RuntimeContext(_closure);
            foreach (var statement in Body)
                fnRuntime.Evaluate(statement);
            return null;
        }
        catch (ReturnException re)
        {
            return re.Value;
        }
        finally
        {
            _closure = previous;
        }
    }
}