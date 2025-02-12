using Language.Experimental.Expressions;
using Language.Experimental.Parser;
using Language.Experimental.Statements;
using Language.Experimental.TypedExpressions;
using Language.Experimental.TypedStatements;

namespace Language.Experimental.Interfaces
{
    public interface ITypeResolver
    {
        void GatherSignature(FunctionDefinition functionDefinition);
        void GatherSignature(GenericFunctionDefinition genericFunctionDefinition);
        void GatherSignature(GenericTypeDefinition genericTypeDefinition);
        void GatherSignature(ImportedFunctionDefinition importedFunctionDefinition);
        void GatherSignature(ImportLibraryDefinition importLibraryDefinition);
        void GatherSignature(TypeDefinition typeDefinition);
        TypedExpression Resolve(CallExpression callExpression);
        TypedExpression Resolve(CastExpression castExpression);
        TypedExpression Resolve(CompilerIntrinsic_GetExpression compilerIntrinsic_GetExpression);
        TypedExpression Resolve(CompilerIntrinsic_SetExpression compilerIntrinsic_SetExpression);
        TypedStatement Resolve(FunctionDefinition functionDefinition);
        TypedExpression Resolve(GenericFunctionReferenceExpression genericFunctionReferenceExpression);
        TypedExpression Resolve(GetExpression getExpression);
        TypedExpression Resolve(IdentifierExpression identifierExpression);
        TypedStatement Resolve(ImportedFunctionDefinition importedFunctionDefinition);
        TypedStatement Resolve(ImportLibraryDefinition importLibraryDefinition);
        TypedExpression Resolve(InlineAssemblyExpression inlineAssemblyExpression);
        TypedExpression Resolve(LambdaExpression lambdaExpression);
        TypedExpression Resolve(LiteralExpression literalExpression);
        TypedExpression Resolve(LocalVariableExpression localVariableExpression);
        TypedExpression Resolve(ReturnExpression returnExpression);
        TypedExpression Resolve(SetExpression setExpression);
    }
}