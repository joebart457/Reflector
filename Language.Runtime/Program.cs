

using Language.Parser;
using Language.Runtime;
using Language.Runtime.Builders;
using Language.Runtime.BuiltinTypes;

var context = new RuntimeContextBuilder()
                .ProvideValue("int", typeof(int))
                .ProvideValue("long", typeof(long))
                .ProvideValue("float", typeof(float))
                .ProvideValue("double", typeof(double))
                .ProvideValue("string", typeof(string))
                .ProvideValue("bool", typeof(bool))
                .ProvideValue("any", typeof(Environment<object?>))
                .ProvideValue("list", typeof(IEnumerable<object?>))
                .Provide("defn", () => Runtime.DefineFunction(default!, default!, default!, default!))
                .Provide("defna", () => Runtime.DefineAnonymousFunction(default!, default!, default!, default!))
                .Provide("return", () => Runtime.Return(default!))
                .Provide("params", () => Runtime.Params(default!))
                .Provide("param", () => Runtime.Param(default!, default!))
                .Provide("if", () => Runtime.If(default!, default!, default!, default!))
                .Provide("print", () => Console.Write(""))
                .Provide("println", () => Console.WriteLine(""))
                .Provide("eq", () => Runtime.Equal(default, default))
                .EmitContext();


var parser = new LanguageParser();

var exprs = parser.ParseText("""
    (defn +main (params (param +msg string))
        +(if (eq msg 'test') 
            (defna (params) +(println 'this is just a test message'))
            (defna (params) +(println msg))
        )
        +(println msg)
    )
    (main 'test')

    """, out var errors);

context.Evaluate(exprs);