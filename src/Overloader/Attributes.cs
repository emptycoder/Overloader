// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedParameter.Local

namespace Overloader;

internal static class AttributeNames
{
	public static readonly string PartialOverloadsAttr = nameof(PartialOverloadsAttribute).Replace("Attribute", "");
	public static readonly string NewClassOverloadsAttr = nameof(NewClassOverloadAttribute).Replace("Attribute", "");

	public static readonly string FormatterAttr = nameof(FormatterAttribute).Replace("Attribute", "");

	// ReSharper disable once InconsistentNaming
	public static readonly string TAttr = nameof(TAttribute).Replace("Attribute", "");
	public static readonly string IgnoreForAttr = nameof(IgnoreForAttribute).Replace("Attribute", "");
	public static readonly string ChangeAccessModifierAttr = nameof(ChangeAccessModifierAttribute).Replace("Attribute", "");
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class PartialOverloadsAttribute : Attribute
{
	public PartialOverloadsAttribute(params Type[] types) { }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class NewClassOverloadAttribute : Attribute
{
	public NewClassOverloadAttribute(string nameRegex, string regexReplace, Type type) { }
}

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class FormatterAttribute : Attribute
{
	public FormatterAttribute(Type type, object[] genericParams, object[] @params) { }
}

// ReSharper disable once InconsistentNaming
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class TAttribute : Attribute
{
	public TAttribute(Type? newType = null, Type? forType = null) { }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class IgnoreForAttribute : Attribute
{
	public IgnoreForAttribute(Type? type) { }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ChangeAccessModifierAttribute : Attribute
{
	public ChangeAccessModifierAttribute(string newModifier, Type? forType) { }
}
