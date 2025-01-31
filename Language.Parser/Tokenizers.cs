using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TokenizerCore.Model;
using TokenizerCore.Models.Constants;
using TokenizerCore;
using Language.Parser.Constants;

namespace Language.Parser;

public static class Tokenizers
{
    public static List<TokenizerRule> _defaultRules => new List<TokenizerRule>()
        {
                    new TokenizerRule(TokenTypes.LParen, "("),
                    new TokenizerRule(TokenTypes.RParen, ")"),
                    new TokenizerRule(TokenTypes.Dot, "."),
                    new TokenizerRule(TokenTypes.NullDot, "?."),
                    new TokenizerRule(TokenTypes.Reflector, "+"),

                    new TokenizerRule(BuiltinTokenTypes.EndOfLineComment, "//"),
                    new TokenizerRule(BuiltinTokenTypes.String, "\"", enclosingLeft: "\"", enclosingRight: "\""),
                    new TokenizerRule(BuiltinTokenTypes.String, "'", enclosingLeft: "'", enclosingRight: "'"),
                    new TokenizerRule(BuiltinTokenTypes.Word, "`", enclosingLeft: "`", enclosingRight: "`"),
        };
    public static TokenizerSettings DefaultSettings => new TokenizerSettings
    {
        AllowNegatives = true,
        NegativeChar = '-',
        NewlinesAsTokens = false,
        WordStarters = "_@",
        WordIncluded = "_@?",
        IgnoreCase = true,
        TabSize = 1,
        CommentsAsTokens = true,
    };
    public static Tokenizer Default => new Tokenizer(_defaultRules, DefaultSettings);

    public static Tokenizer CreateFromDefault(List<TokenizerRule> rules, TokenizerSettings? tokenizerSettings = null)
    {
        var defaultRulesCopy = _defaultRules.Select(x => x).ToList();
        defaultRulesCopy.AddRange(rules);
        return new Tokenizer(defaultRulesCopy, tokenizerSettings ?? DefaultSettings);
    }
}