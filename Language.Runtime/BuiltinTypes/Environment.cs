using Language.Runtime.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TokenizerCore.Interfaces;
using TokenizerCore.Model;

namespace Language.Runtime.BuiltinTypes;


public class Environment<Ty>
{
    internal class VariableTokenEqualityComparer : IEqualityComparer<IToken>
    {
        public bool Equals(IToken? x, IToken? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
            return x.Lexeme == y.Lexeme;
        }

        public int GetHashCode([DisallowNull] IToken obj)
        {
            return obj.Lexeme.GetHashCode();
        }
    }
    public string EnvironmentAlias { get; private set; } = "";
    private Dictionary<IToken, Ty> _lookup = new Dictionary<IToken, Ty>(new VariableTokenEqualityComparer());
    public Environment<Ty>? Enclosing { get; private set; }
    public Environment(Environment<Ty>? enclosing)
    {
        Enclosing = enclosing;
    }

    public Environment(string alias, Environment<Ty>? enclosing)
    {
        EnvironmentAlias = alias;
        Enclosing = enclosing;
    }

    public bool Exists(IToken key)
    {
        if (_lookup.ContainsKey(key)) return true;
        if (Enclosing != null) return Enclosing.Exists(key);
        return false;
    }

    public bool Exists(string key)
    {
        return Exists(GenerateToken(key));
    }

    public Ty Get(IToken key)
    {
        if (_lookup.TryGetValue(key, out var value)) return value;
        if (Enclosing != null) return Enclosing.Get(key);
        throw new RuntimeException(key, $"symbol {key} is not defined");
    }

    public Ty Get(string key)
    {
        return Get(GenerateToken(key));
    }

    public void Define(IToken key, Ty value)
    {
        if (Exists(key)) throw new RuntimeException(key, $"redefinition of symbol {key}");
        _lookup[key] = value;
    }

    public void Define(string key, Ty value)
    {
        Define(GenerateToken(key), value);
    }

    public void Assign(IToken key, Ty value)
    {
        if (_lookup.ContainsKey(key))
        {
            _lookup[key] = value;
        }
        else if (Enclosing != null) Enclosing.Assign(key, value);
        else throw new RuntimeException(key, $"undefined symbol: {key.Lexeme}");
    }

    public void Assign(string key, Ty value)
    {
        Assign(GenerateToken(key), value);
    }

    public List<Ty> GetValues()
    {
        return _lookup.Values.ToList();
    }

    public List<IToken> GetKeys()
    {
        return _lookup.Keys.ToList();
    }

    public Environment<Ty> Wrap()
    {
        return new Environment<Ty>(this);
    }

    public string GetFullPath()
    {

        var current = this;
        var aliasList = new List<string>();
        while (current != null)
        {
            if (string.IsNullOrEmpty(current.EnvironmentAlias))
                break;
            aliasList.Add(current.EnvironmentAlias);
            current = current.Enclosing;
        }
        aliasList.Reverse();
        return string.Join(".", aliasList);
    }

    public string GetFullPath(string symbol)
    {
        var current = this;
        var aliasList = new List<string>();
        while (current != null)
        {
            if (string.IsNullOrEmpty(current.EnvironmentAlias))
                break;
            aliasList.Add(current.EnvironmentAlias);
            current = current.Enclosing;
        }
        aliasList.Reverse();
        aliasList.Add(symbol);
        return string.Join(".", aliasList);
    }

    private static IToken GenerateToken(string key) => new Token(key, key, 0, 0);
}