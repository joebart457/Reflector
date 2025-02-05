using Language.Experimental.Constants;
using Language.Experimental.Expressions;
using Language.Experimental.Models;
using Language.Experimental.Statements;
using ParserLite;
using ParserLite.Exceptions;
using System.Runtime.InteropServices;
using TokenizerCore;
using TokenizerCore.Interfaces;
using TokenizerCore.Models.Constants;

namespace Language.Experimental.Parser;



public class ProgramParser : TokenParser
{
    private Tokenizer _tokenizer;

    public ProgramParser(Tokenizer tokenizer)
    {
        _tokenizer = tokenizer;
    }

    public ProgramParser()
    {
        _tokenizer = Tokenizers.Default;
    }
    public List<StatementBase> ParseFile(string path, out List<ParsingException> errors)
    {
        return ParseText(File.ReadAllText(path), out errors);
    }

    public List<StatementBase> ParseText(string text, out List<ParsingException> errors)
    {
        var tokenizer = Tokenizers.Default;
        errors = new List<ParsingException>();
        var statements = new List<StatementBase>();

        var tokens = tokenizer.Tokenize(text, false)
            .Where(token => token.Type != BuiltinTokenTypes.EndOfFile)
            .ToList();

        Initialize(tokens);
        while (!AtEnd())
        {
            try
            {
                statements.Add(ParseNext());
            }
            catch (ParsingException e)
            {
                errors.Add(e);
                SeekToNextParsableUnit();
            }
        }
        return statements;
    }

    private void SeekToNextParsableUnit()
    {
        while (!AtEnd())
        {
            Advance();
            if (Match(TokenTypes.LParen)) break;
        }
    }

    public StatementBase ParseNext()
    {
        return ParseStatement();
    }

    public TypeInfo ParseTypeInfo()
    {
        if (!AdvanceIfMatch(TokenTypes.IntrinsicType))
            throw new ParsingException(Current(), "expect type annotation");
        if (!Enum.TryParse<IntrinsicType>(Previous().Lexeme, true, out var type))
            throw new ParsingException(Previous(), $"unsupported type annotation {Previous().Lexeme}");
        if (RequiresTypeArgument(type))
        {
            Consume(TokenTypes.LBracket, $"expect type argument for type {type}");
            if (SupportsMultipleTypeArguments(type))
            {
                List<TypeInfo> typeArguments = new();
                do
                {
                    var typeArgument = ParseTypeInfo();
                    typeArguments.Add(typeArgument);
                } while (AdvanceIfMatch(TokenTypes.Comma));
                Consume(TokenTypes.RBracket, "expect enclosing ] after type arguments");
                return new FunctionPtrTypeInfo(type, typeArguments);

            }else
            {
                var typeArgument = ParseTypeInfo();
                Consume(TokenTypes.RBracket, "expect enclosing ] after type argument");
                return new TypeInfo(type, typeArgument);
            }

        }
        return new TypeInfo(type, null);
    }

    private bool RequiresTypeArgument(IntrinsicType type)
    {
        return type == IntrinsicType.Ptr
            || type == IntrinsicType.StdCall_Function_Ptr
            || type == IntrinsicType.StdCall_Function_Ptr_Internal
            || type == IntrinsicType.StdCall_Function_Ptr_External
            || type == IntrinsicType.Cdecl_Function_Ptr
            || type == IntrinsicType.Cdecl_Function_Ptr_Internal
            || type == IntrinsicType.Cdecl_Function_Ptr_External;
    }
    private bool SupportsMultipleTypeArguments(IntrinsicType type)
    {
        return type == IntrinsicType.StdCall_Function_Ptr
            || type == IntrinsicType.StdCall_Function_Ptr_Internal
            || type == IntrinsicType.StdCall_Function_Ptr_External
            || type == IntrinsicType.Cdecl_Function_Ptr
            || type == IntrinsicType.Cdecl_Function_Ptr_Internal
            || type == IntrinsicType.Cdecl_Function_Ptr_External;
    }

    public StatementBase ParseStatement()
    {
        Consume(TokenTypes.LParen, "expect all statements to begin with (");
        if (AdvanceIfMatch(TokenTypes.DefineFunction)) return ParseFunctionDefinition();
        if (AdvanceIfMatch(TokenTypes.Import)) return ParseImportedFunctionDefinition();
        if (AdvanceIfMatch(TokenTypes.Library)) return ParseImportLibraryDefinition();
        throw new ParsingException(Current(), $"unexpected token {Current()}");
    }

    public FunctionDefinition ParseFunctionDefinition()
    {
        /*
         * (defn main:int (params (param argc int) (param argv ptr[string]))
         * 
         * 
         */
        var name = Consume(BuiltinTokenTypes.Word, "expect function name");
        Consume(TokenTypes.Colon, "expect functionName:returnType");
        var returnType = ParseTypeInfo();
        Consume(TokenTypes.LParen, "expect parameter list");
        Consume(TokenTypes.Params, "expect paramter list. IE (params (param argc int) (param argv ptr<string>))");
        var parameters = new List<Parameter>();
        if (!AdvanceIfMatch(TokenTypes.RParen))
        {
            do
            {
                Consume(TokenTypes.LParen, "expect parameter definition");
                Consume(TokenTypes.Param, "expect parameter definition");
                var parameterName = Consume(BuiltinTokenTypes.Word, "expect parameter name");
                var parameterType = ParseTypeInfo();
                Consume(TokenTypes.RParen, "expect enclosing ) in parameter definition");
                parameters.Add(new Parameter(parameterName, parameterType));
            } while(!AtEnd() && !Match(TokenTypes.RParen));
            Consume(TokenTypes.RParen, "expect enclosing ) in parameter list");
        }
        var body = new List<ExpressionBase>();
        if (!AdvanceIfMatch(TokenTypes.RParen))
        {
            do
            {
                body.Add(ParseExpression());
            } while (!AtEnd() && !Match(TokenTypes.RParen));
            Consume(TokenTypes.RParen, "expect enclosing ) in function body");
        }
        return new FunctionDefinition(name, returnType, parameters, body);
    }



    public ImportedFunctionDefinition ParseImportedFunctionDefinition()
    {
        /*
         * (import mscvrt cdecl (symbol `_printf`) 
         *          printf:void (params (param string s)))
         * 
         * 
         */
        var libraryAlias = Consume(BuiltinTokenTypes.Word, "expect import library alias");
        var callingConvention = ParseCallingConvention();
        var functionName = Consume(BuiltinTokenTypes.Word, "expect function name");
        Consume(TokenTypes.Colon, "expect functionName:returnType");
        var returnType = ParseTypeInfo();
        IToken importSymbol = functionName;
        if (Match(TokenTypes.LParen) && PeekMatch(1, TokenTypes.Symbol))
        {
            Advance();
            Advance();
            importSymbol = Consume(BuiltinTokenTypes.Word, "expect import symbol");
            Consume(TokenTypes.RParen, "expect enclosing ) in symbol annotation");
        }
        Consume(TokenTypes.LParen, "expect parameter list");
        Consume(TokenTypes.Params, "expect paramter list. IE (params (param argc int) (param argv ptr<string>))");
        var parameters = new List<Parameter>();
        if (!AdvanceIfMatch(TokenTypes.RParen))
        {
            do
            {
                Consume(TokenTypes.LParen, "expect parameter definition");
                Consume(TokenTypes.Param, "expect parameter definition");
                var parameterName = Consume(BuiltinTokenTypes.Word, "expect parameter name");
                var parameterType = ParseTypeInfo();
                Consume(TokenTypes.RParen, "expect enclosing ) in parameter definition");
                parameters.Add(new Parameter(parameterName, parameterType));
            } while (!AtEnd() && !Match(TokenTypes.RParen));
            Consume(TokenTypes.RParen, "expect enclosing ) in parameter list");
        }
        Consume(TokenTypes.RParen, "expect enclosing ) after imported function definition");
        return new ImportedFunctionDefinition(functionName, returnType, parameters, callingConvention, libraryAlias, importSymbol);
    }

    public ImportLibraryDefinition ParseImportLibraryDefinition()
    {
        /*
         * (library mscvrt `msvcrt.dll`)
         * 
         * 
         */
        var libraryAlias = Consume(BuiltinTokenTypes.Word, "expect import library alias");
        var libraryPath = Consume(BuiltinTokenTypes.Word, "expect path to dll");
        Consume(TokenTypes.RParen, "expect enclosing ) after import library definition");    

        return new ImportLibraryDefinition(libraryAlias, libraryPath);
    }

    private CallingConvention ParseCallingConvention()
    {
        if (!AdvanceIfMatch(TokenTypes.CallingConvention))
            throw new ParsingException(Current(), "expect calling convention");
        if (!Enum.TryParse<CallingConvention>(Previous().Lexeme, true, out var callingConvention))
            throw new ParsingException(Previous(), $"unsupported calling convention {Previous().Lexeme}");
        return callingConvention;
    }

    public ExpressionBase ParseExpression()
    {
        if (AdvanceIfMatch(TokenTypes.InlineAssembly)) return new InlineAssemblyExpression(Previous(), Previous().Lexeme);
        return ParseCall();
    }

    private ExpressionBase ParseCall()
    {
        if (AdvanceIfMatch(TokenTypes.LParen))
        {
            if (AdvanceIfMatch(TokenTypes.CompilerIntrinsicGet)) return ParseCompilerIntrinsicGet();
            if (AdvanceIfMatch(TokenTypes.CompilerIntrinsicSet)) return ParseCompilerIntrinsicSet();
            if (AdvanceIfMatch(TokenTypes.Return)) return ParseReturn();
            var token = Previous();
            if (AdvanceIfMatch(TokenTypes.RParen))
                throw new ParsingException(Previous(), "empty call encountered");
            var callTarget = ParseExpression();
            var arguments = new List<ExpressionBase>();
            if (!AdvanceIfMatch(TokenTypes.RParen))
            {
                do
                {
                    arguments.Add(ParseExpression());
                } while (!AtEnd() && !Match(TokenTypes.RParen));
                Consume(TokenTypes.RParen, "expect enclosing ) after call");
            }
            return new CallExpression(token, callTarget, arguments);
        }
        else return ParseGet();

    }

    private ExpressionBase ParseCompilerIntrinsicGet()
    {
        var token = Previous(); 
        Consume(TokenTypes.Colon, "expect _ci_get:returnType");
        var returnType = ParseTypeInfo();
        var contextPointer = ParseExpression();
        int offset = int.Parse(Consume(BuiltinTokenTypes.Integer, "expect integer offset").Lexeme);
        Consume(TokenTypes.RParen, "expect enclosing ) after call to _ci_get");
        return new CompilerIntrinsic_GetExpression(token, returnType, contextPointer, offset);
    }

    private ExpressionBase ParseCompilerIntrinsicSet()
    {
        var token = Previous();
        var contextPointer = ParseExpression();
        int offset = int.Parse(Consume(BuiltinTokenTypes.Integer, "expect integer offset to memory location").Lexeme);
        var valueToAssign = ParseExpression();
        Consume(TokenTypes.RParen, "expect enclosing ) after call to _ci_set");
        return new CompilerIntrinsic_SetExpression(token, contextPointer, offset, valueToAssign);
    }

    private ExpressionBase ParseReturn()
    {
        var token = Previous();
        ExpressionBase? returnValue = null;
        if (!AdvanceIfMatch(TokenTypes.RParen))
        {
            returnValue = ParseExpression();
            Consume(TokenTypes.RParen, "expect enclosing ) after return statement");
        }
        return new ReturnExpression(token, returnValue);
    }

    private ExpressionBase ParseGet()
    {

        var expr = ParsePrimary();
        if (expr is IdentifierExpression identifierExpression)
        {
            while (Match(TokenTypes.Dot) || Match(TokenTypes.NullDot))
            {
                if (AdvanceIfMatch(TokenTypes.Dot))
                {
                    var targetField = Consume(BuiltinTokenTypes.Word, "expect member name after '.'");
                    expr = new GetExpression(Previous(), expr, targetField, false);
                }
                else
                {
                    Advance();
                    var targetField = Consume(BuiltinTokenTypes.Word, "expect member name after '?.'");
                    expr = new GetExpression(Previous(), expr, targetField, true);
                }
            }
        }


        return expr;
    }

    private ExpressionBase ParsePrimary()
    {
        if (AdvanceIfMatch(BuiltinTokenTypes.Word)) return new IdentifierExpression(Previous());
        return ParseLiteral();
    }

    public LiteralExpression ParseLiteral()
    {
        if (AdvanceIfMatch(BuiltinTokenTypes.Integer))
        {
            return new LiteralExpression(Previous(), int.Parse(Previous().Lexeme));
        }
        if (AdvanceIfMatch(BuiltinTokenTypes.Float))
        {
            return new LiteralExpression(Previous(), float.Parse(Previous().Lexeme));
        }
        if (AdvanceIfMatch(BuiltinTokenTypes.String))
        {
            return new LiteralExpression(Previous(), Previous().Lexeme);
        }
        throw new ParsingException(Current(), $"encountered unexpected token {Current()}");
    }
}