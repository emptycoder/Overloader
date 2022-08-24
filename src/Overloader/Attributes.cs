namespace Overloader;

internal static class Attributes
{
	public const string OverloadAttr = "Overload";
	public const string FormatterAttr = "Formatter";

	// ReSharper disable once InconsistentNaming
	public const string TAttr = "T";
	public const string IntegrityAttr = "Integrity";
	public const string IgnoreForAttr = "IgnoreFor";
	public const string BlackListModeAttr = "BlackListMode";
	public const string AllowForAttr = "AllowFor";
	public const string ChangeModifierAttr = "ChangeModifier";

	public const string AttributesWithHeaderSource = @$"using System;
#nullable enable
namespace {nameof(Overloader)};
{AttributesSource}";

	private const string AttributesSource = @"
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

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
public sealed class TAttribute : Attribute
{
	public TAttribute(Type? newType = null, Type? forType = null) { }
}

[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
public sealed class IntegrityAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class BlackListModeAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class IgnoreForAttribute : Attribute
{
	public IgnoreForAttribute(Type? type = null) { }
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class AllowForAttribute : Attribute
{
	public AllowForAttribute(Type? type = null) { }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ChangeModifierAttribute : Attribute
{
	public ChangeModifierAttribute(string modifier, string newModifier, Type? forType = null) { }
}";
}
