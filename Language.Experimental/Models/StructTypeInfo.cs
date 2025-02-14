using Language.Experimental.Constants;
using Language.Experimental.Parser;
using ParserLite.Exceptions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Models;


public class StructTypeInfo : TypeInfo
{
    public IToken Name { get; private set; }
    public List<StructFieldInfo> Fields {  get; private set; }
    public StructTypeInfo(IToken name, List<StructFieldInfo> fields) : base(IntrinsicType.Struct, null)
    {
        Name = name;
        Fields = fields;
        ValidateFields();
    }

    public void ValidateFields()
    {
        if (Fields.DistinctBy(x => x.Name.Lexeme).Count() != Fields.Count)
            throw new ParsingException(Name, $"duplicate field name in type {Name.Lexeme}");
        foreach(var field in Fields)
        {
            if (field.TypeInfo.IsStructType || field.TypeInfo.Is(IntrinsicType.Void))
                throw new ParsingException(field.Name, $"invalid field type {field.TypeInfo}");
            if (field.TypeInfo.Equals(this))
                throw new ParsingException(field.Name, $"invalid recursive type definition: field {field.Name.Lexeme}");
        }
    }

    public override int SizeInMemory() => Fields.Sum(x => x.TypeInfo.SizeInMemory());

    public override int StackSize() => throw new InvalidOperationException($"stack allocation is not currently supported for struct types (type: {Name.Lexeme})");
    public override bool IsStructType => true;
    public override int GetFieldOffset(IToken fieldName)
    {
        int offset = 0;
        for (int i = 0; i < Fields.Count; i++)
        {
            if (Fields[i].Name.Lexeme == fieldName.Lexeme) return offset;
            offset += Fields[i].TypeInfo.SizeInMemory();
        }
        throw new ParsingException(fieldName, $"type {Name.Lexeme} does not contain a field called {fieldName.Lexeme}");
    }

    public override TypeInfo GetFieldType(IToken fieldName)
    {
        var foundField = Fields.Find(x => x.Name.Lexeme == fieldName.Lexeme);
        if (foundField != null) return foundField.TypeInfo;
        throw new ParsingException(fieldName, $"type {Name.Lexeme} does not contain a field called {fieldName.Lexeme}");
    }

    public override int GetHashCode()
    {
        return Name.Lexeme.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is StructTypeInfo typeInfo)
        {
            if (Name.Lexeme != typeInfo.Name.Lexeme) return false;
            if (Fields.Count != typeInfo.Fields.Count) return false;
            for (int i = 0; i < Fields.Count; i++)
            {
                if (Fields[i].Name.Lexeme != typeInfo.Fields[i].Name.Lexeme) return false;
                if (!Fields[i].TypeInfo.Equals(typeInfo.Fields[i])) return false;
            }
            return true;    
        }
        return false;
    }

    public override bool TryExtractGenericArgumentTypes(Dictionary<TypeSymbol, TypeInfo> genericParameterToArgumentTypeMap, TypeSymbol parameterType)
    {
        // Note parameter type is the actual function parameter type, not the type parameter type
        if (parameterType.IsGenericTypeSymbol)
        {
            if (genericParameterToArgumentTypeMap.TryGetValue(parameterType, out var resolvedTypeArgument))
            {
                if (!resolvedTypeArgument.Equals(this)) return false;
                return true;
            }
            genericParameterToArgumentTypeMap[parameterType] = this;
            return true;
        }
        if (parameterType.TypeName.Lexeme != Name.Lexeme) return false;
        if (GenericTypeArgument != null)
        {
            if (parameterType.TypeArguments.Count == 1) return GenericTypeArgument.TryExtractGenericArgumentTypes(genericParameterToArgumentTypeMap, parameterType.TypeArguments[0]);
            return false;
        }
        return parameterType.TypeArguments.Count == 0;
    }

    public override TypeSymbol ToTypeSymbol()
    {
        return new TypeSymbol(Name, []);
    }

    public override string ToString()
    {
        return Name.Lexeme;
    }
}

public class StructFieldInfo
{
    public TypeInfo TypeInfo { get; set; }
    public IToken Name { get; set; }

    public StructFieldInfo(TypeInfo typeInfo, IToken name)
    {
        TypeInfo = typeInfo;
        Name = name;
    }
}