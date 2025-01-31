using Language.Parser.Constants;
using Language.Parser.Expressions;
using ParserLite.Exceptions;
using ParserLite;
using TokenizerCore.Interfaces;
using TokenizerCore.Models.Constants;
using TokenizerCore;

namespace Language.Parser;

public class LanguageParser : TokenParser
{
    private Tokenizer _tokenizer;

    public LanguageParser(Tokenizer tokenizer)
    {
        _tokenizer = tokenizer;
    }

    public LanguageParser()
    {
        _tokenizer = Tokenizers.Default;
    }
    public List<ExpressionBase> ParseFile(string path, out List<ParsingException> errors)
    {
        return ParseFile(File.ReadAllText(path), out errors);
    }

    public List<ExpressionBase> ParseText(string text, out List<ParsingException> errors)
    {
        var tokenizer = Tokenizers.Default;
        errors = new List<ParsingException>();
        var expressions = new List<ExpressionBase>();

        var tokens = tokenizer.Tokenize(text, false)
            .Where(token => token.Type != BuiltinTokenTypes.EndOfFile)
            .ToList();

        Initialize(tokens);
        while (!AtEnd())
        {
            try
            {
                expressions.Add(ParseNext());
            }
            catch (ParsingException e)
            {
                errors.Add(e);
                SeekToNextParsableUnit();
            }
        }
        return expressions;
    }

    private void SeekToNextParsableUnit()
    {
        while (!AtEnd())
        {
            Advance();
            if (Match(TokenTypes.LParen)) break;
        }
    }

    public ExpressionBase ParseNext()
    {
        return ParseExpression();
    }

    public Type ParseType(IToken potentialType)
    {
        if (potentialType.Lexeme == "string") return typeof(string);
        if (potentialType.Lexeme == "char") return typeof(char);
        if (potentialType.Lexeme == "byte") return typeof(byte);
        if (potentialType.Lexeme == "short") return typeof(short);
        if (potentialType.Lexeme == "int") return typeof(int);
        if (potentialType.Lexeme == "long") return typeof(long);
        if (potentialType.Lexeme == "float") return typeof(float);
        if (potentialType.Lexeme == "double") return typeof(double);
        if (potentialType.Lexeme == "object") return typeof(object);
        if (potentialType.Lexeme == "list") return typeof(IEnumerable<object?>);
        throw new ParsingException(potentialType, $"unknown type '{potentialType.Lexeme}'");
    }

   

    public ExpressionBase ParseExpression()
    {
        if (AdvanceIfMatch(TokenTypes.Reflector))
        {
            return new LiteralExpression(Previous(), ParseCall());
        }
        return ParseCall();
    }

    private ExpressionBase ParseCall()
    {
        if (AdvanceIfMatch(TokenTypes.LParen))
        {
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
        if (AdvanceIfMatch(BuiltinTokenTypes.Double))
        {
            return new LiteralExpression(Previous(), double.Parse(Previous().Lexeme));
        }
        if (AdvanceIfMatch(BuiltinTokenTypes.Float))
        {
            return new LiteralExpression(Previous(), float.Parse(Previous().Lexeme));
        }
        if (AdvanceIfMatch(BuiltinTokenTypes.UnsignedInteger))
        {
            return new LiteralExpression(Previous(), uint.Parse(Previous().Lexeme));
        }
        if (AdvanceIfMatch(BuiltinTokenTypes.String))
        {
            return new LiteralExpression(Previous(), Previous().Lexeme);
        }
        throw new ParsingException(Current(), $"encountered unexpected token {Current()}");
    }
}