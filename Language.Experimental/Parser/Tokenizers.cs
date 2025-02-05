
using TokenizerCore.Model;
using TokenizerCore.Models.Constants;
using TokenizerCore;
using System.Runtime.InteropServices;
using Language.Experimental.Constants;

namespace Language.Experimental.Parser;



public static class Tokenizers
{
    public static List<TokenizerRule> _defaultRules => new List<TokenizerRule>()
        {
                    new TokenizerRule(TokenTypes.LParen, "("),
                    new TokenizerRule(TokenTypes.RParen, ")"),
                    new TokenizerRule(TokenTypes.LBracket, "["),
                    new TokenizerRule(TokenTypes.RBracket, "]"),
                    new TokenizerRule(TokenTypes.Dot, "."),
                    new TokenizerRule(TokenTypes.Colon, ":"),
                    new TokenizerRule(TokenTypes.Comma, ","),

                    new TokenizerRule(TokenTypes.IntrinsicType, IntrinsicType.String.ToString(), ignoreCase: true),
                    new TokenizerRule(TokenTypes.IntrinsicType, IntrinsicType.StdCall_Function_Ptr.ToString(), ignoreCase: true),
                    new TokenizerRule(TokenTypes.IntrinsicType, IntrinsicType.StdCall_Function_Ptr_External.ToString(), ignoreCase: true),
                    new TokenizerRule(TokenTypes.IntrinsicType, IntrinsicType.StdCall_Function_Ptr_Internal.ToString(), ignoreCase: true),
                    new TokenizerRule(TokenTypes.IntrinsicType, IntrinsicType.Cdecl_Function_Ptr.ToString(), ignoreCase: true),
                    new TokenizerRule(TokenTypes.IntrinsicType, IntrinsicType.Cdecl_Function_Ptr_External.ToString(), ignoreCase: true),
                    new TokenizerRule(TokenTypes.IntrinsicType, IntrinsicType.Cdecl_Function_Ptr_Internal.ToString(), ignoreCase: true),
                    new TokenizerRule(TokenTypes.IntrinsicType, IntrinsicType.Float.ToString(), ignoreCase: true),
                    new TokenizerRule(TokenTypes.IntrinsicType, IntrinsicType.Int.ToString(), ignoreCase: true),
                    new TokenizerRule(TokenTypes.IntrinsicType, IntrinsicType.Ptr.ToString(), ignoreCase: true),
                    new TokenizerRule(TokenTypes.IntrinsicType, IntrinsicType.Void.ToString(), ignoreCase: true),

                    new TokenizerRule(TokenTypes.CallingConvention, CallingConvention.Cdecl.ToString(), ignoreCase: true),
                    new TokenizerRule(TokenTypes.CallingConvention, CallingConvention.StdCall.ToString(), ignoreCase: true),

                    new TokenizerRule(TokenTypes.DefineFunction, "defn"),
                    new TokenizerRule(TokenTypes.Import, "import"),
                    new TokenizerRule(TokenTypes.Library, "library"),
                    new TokenizerRule(TokenTypes.Symbol, "symbol"),
                    new TokenizerRule(TokenTypes.Params, "params"),
                    new TokenizerRule(TokenTypes.Param, "param"),

                    new TokenizerRule(TokenTypes.InlineAssembly, "__asm {", enclosingLeft: "__asm {", enclosingRight: "}", ignoreCase: true),
                    new TokenizerRule(TokenTypes.CompilerIntrinsicGet, "_ci_get"),
                    new TokenizerRule(TokenTypes.CompilerIntrinsicSet, "_ci_set"),

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
        IgnoreCase = false,
        TabSize = 1,
        CommentsAsTokens = false,
    };
    public static Tokenizer Default => new Tokenizer(_defaultRules, DefaultSettings);

    public static Tokenizer CreateFromDefault(List<TokenizerRule> rules, TokenizerSettings? tokenizerSettings = null)
    {
        var defaultRulesCopy = _defaultRules.Select(x => x).ToList();
        defaultRulesCopy.AddRange(rules);
        return new Tokenizer(defaultRulesCopy, tokenizerSettings ?? DefaultSettings);
    }
}