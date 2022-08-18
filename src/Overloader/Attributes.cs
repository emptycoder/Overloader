// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedParameter.Local

namespace Overloader;

internal static class AttributeNames
{
	public static readonly string OverloadsAttr = nameof(OverloadAttribute).Replace("Attribute", "");
	public static readonly string FormatterAttr = nameof(FormatterAttribute).Replace("Attribute", "");

	// ReSharper disable once InconsistentNaming
	public static readonly string TAttr = nameof(TAttribute).Replace("Attribute", "");
	public static readonly string IntegrityAttr = nameof(IntegrityAttribute).Replace("Attribute", "");
	public static readonly string IgnoreForAttr = nameof(IgnoreForAttribute).Replace("Attribute", "");
	public static readonly string BlackListModeAttr = nameof(BlackListModeAttribute).Replace("Attribute", "");
	public static readonly string AllowForAttr = nameof(AllowForAttribute).Replace("Attribute", "");
	public static readonly string ChangeModifierAttr = nameof(ChangeModifierAttribute).Replace("Attribute", "");
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class OverloadAttribute : Attribute
{
	public OverloadAttribute(Type type, string? nameRegex = null, string? regexReplace = null) { }
}

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class FormatterAttribute : Attribute
{
	public FormatterAttribute(Type type, object[] genericParams, object[] @params) { }
}

// ReSharper disable once InconsistentNaming
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
public sealed class TAttribute : Attribute
{
	public TAttribute(Type? newType = null, Type? forType = null) { }
}

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class IntegrityAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class BlackListModeAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public sealed class IgnoreForAttribute : Attribute
{
	public IgnoreForAttribute(Type? type = null) { }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class AllowForAttribute : Attribute
{
	public AllowForAttribute(Type? type = null) { }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ChangeModifierAttribute : Attribute
{
	public ChangeModifierAttribute(string modifier, string newModifier, Type? forType = null) { }
}
