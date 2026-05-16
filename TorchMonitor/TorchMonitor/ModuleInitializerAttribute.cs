// C# 9 [ModuleInitializer] requires this attribute in System.Runtime.CompilerServices.
// .NET Framework 4.8 does not include it, so we define it here.
// The C# 9 compiler recognizes this definition and emits a proper module .cctor.
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    sealed class ModuleInitializerAttribute : Attribute { }
}
