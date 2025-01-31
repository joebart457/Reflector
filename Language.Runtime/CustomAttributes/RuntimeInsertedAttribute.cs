
namespace Language.Runtime.CustomAttributes;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public class RuntimeInsertedAttribute : System.Attribute
{
}