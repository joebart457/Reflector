﻿
using TokenizerCore.Model;
using TokenizerCore.Models.Constants;
using TokenizerCore;
using System.Runtime.InteropServices;
using Language.Experimental.Constants;
using Language.Experimental.Compiler.Instructions;

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

                    new TokenizerRule(BuiltinTokenTypes.Word, "string", IntrinsicType.String.ToString()),
                    new TokenizerRule(BuiltinTokenTypes.Word, "fn_ptr", IntrinsicType.StdCall_Function_Ptr.ToString()),
                    new TokenizerRule(BuiltinTokenTypes.Word, "fn_ptr_external", IntrinsicType.StdCall_Function_Ptr_External.ToString()),
                    new TokenizerRule(BuiltinTokenTypes.Word, "fn_ptr_internal", IntrinsicType.StdCall_Function_Ptr_Internal.ToString()),
                    new TokenizerRule(BuiltinTokenTypes.Word, "cfn_ptr", IntrinsicType.Cdecl_Function_Ptr.ToString()),
                    new TokenizerRule(BuiltinTokenTypes.Word, "cfn_ptr_external", IntrinsicType.Cdecl_Function_Ptr_External.ToString()),
                    new TokenizerRule(BuiltinTokenTypes.Word, "cfn_ptr_external", IntrinsicType.Cdecl_Function_Ptr_Internal.ToString()),
                    new TokenizerRule(BuiltinTokenTypes.Word, "float", IntrinsicType.Float.ToString()),
                    new TokenizerRule(BuiltinTokenTypes.Word, "int", IntrinsicType.Int.ToString()),
                    new TokenizerRule(BuiltinTokenTypes.Word, "ptr", IntrinsicType.Ptr.ToString()),
                    new TokenizerRule(BuiltinTokenTypes.Word, "void", IntrinsicType.Void.ToString()),

                    new TokenizerRule(TokenTypes.CallingConvention, CallingConvention.Cdecl.ToString(), ignoreCase: true),
                    new TokenizerRule(TokenTypes.CallingConvention, CallingConvention.StdCall.ToString(), ignoreCase: true),

                    new TokenizerRule(TokenTypes.DefineFunction, "defn"),
                    new TokenizerRule(TokenTypes.Import, "import"),
                    new TokenizerRule(TokenTypes.Library, "library"),
                    new TokenizerRule(TokenTypes.Symbol, "symbol"),
                    new TokenizerRule(TokenTypes.Params, "params"),
                    new TokenizerRule(TokenTypes.Param, "param"),
                    new TokenizerRule(TokenTypes.Return, "return"),
                    new TokenizerRule(TokenTypes.Gen, "gen"),

                    new TokenizerRule(TokenTypes.Type, "type"),
                    new TokenizerRule(TokenTypes.Field, "field"),

                    new TokenizerRule(TokenTypes.InlineAssembly, "__asm {", enclosingLeft: "__asm {", enclosingRight: "}", ignoreCase: true),
                    new TokenizerRule(TokenTypes.CompilerIntrinsicGet, "_ci_get"),
                    new TokenizerRule(TokenTypes.CompilerIntrinsicSet, "_ci_set"),

                    new TokenizerRule(TokenTypes.ByteRegister, X86ByteRegister.al.ToString()),
                    new TokenizerRule(TokenTypes.ByteRegister, X86ByteRegister.bl.ToString()),

                    new TokenizerRule(TokenTypes.GeneralRegister32, X86Register.eax.ToString()),
                    new TokenizerRule(TokenTypes.GeneralRegister32, X86Register.ebx.ToString()),
                    new TokenizerRule(TokenTypes.GeneralRegister32, X86Register.ecx.ToString()),
                    new TokenizerRule(TokenTypes.GeneralRegister32, X86Register.edx.ToString()),
                    new TokenizerRule(TokenTypes.GeneralRegister32, X86Register.esi.ToString()),
                    new TokenizerRule(TokenTypes.GeneralRegister32, X86Register.edi.ToString()),
                    new TokenizerRule(TokenTypes.GeneralRegister32, X86Register.esp.ToString()),
                    new TokenizerRule(TokenTypes.GeneralRegister32, X86Register.ebp.ToString()),

                    new TokenizerRule(TokenTypes.XmmRegister, XmmRegister.xmm0.ToString()),
                    new TokenizerRule(TokenTypes.XmmRegister, XmmRegister.xmm1.ToString()),

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