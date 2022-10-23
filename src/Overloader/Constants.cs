namespace Overloader;

internal static class Constants
{
	public const string DefaultHeader = @$"// <auto-generated/>
/* This source was auto-generated by {nameof(Overloader)} */

#nullable enable
#pragma warning disable CS0162 // Unreachable code
#pragma warning disable CS0164 // Unreferenced label
#pragma warning disable CS0219 // Variable assigned but never used

";

	// ReSharper disable once InconsistentNaming
	public const string TSpecifyAttr = "TSpecify";
	public const string OverloadAttr = "Overload";
	public const string FormatterAttr = "Formatter";

	// ReSharper disable once InconsistentNaming
	public const string TAttr = "T";
	public const string CombineWithAttr = "CombineWith";
	public const string IntegrityAttr = "Integrity";
	public const string IgnoreForAttr = "IgnoreFor";
	public const string BlackListModeAttr = "BlackListMode";
	public const string AllowForAttr = "AllowFor";
	public const string ChangeModifierAttr = "ChangeModifier";

	public const string AttributesFileNameWoExt = "Attributes";

	public const string AttributesWithHeaderSource = @$"{DefaultHeader}
using System;

namespace {nameof(Overloader)};
{AttributesSource}";

	private const string AttributesSource = $@"
/* Global attributes */

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class {FormatterAttr}Attribute : Attribute
{{
	public {FormatterAttr}Attribute(Type type, object[] genericParams, object[] @params, params object[] transitions) {{ }}
}}

/* Class or struct attributes */

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class {TSpecifyAttr}Attribute : Attribute
{{
	public {TSpecifyAttr}Attribute(Type templateType) {{ }}
}}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class {OverloadAttr}Attribute : Attribute
{{
	public {OverloadAttr}Attribute(Type? type = null, string? nameRegex = null, string? regexReplace = null) {{ }}
}}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class {BlackListModeAttr}Attribute : Attribute {{ }}

/* Method attribtes */

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class {IgnoreForAttr}Attribute : Attribute
{{
	public {IgnoreForAttr}Attribute(Type? type = null) {{ }}
}}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class {AllowForAttr}Attribute : Attribute
{{
	public {AllowForAttr}Attribute(Type? type = null) {{ }}
}}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class {ChangeModifierAttr}Attribute : Attribute
{{
	public {ChangeModifierAttr}Attribute(string modifier, string newModifier, Type? forType = null) {{ }}
}}

/* Parameter attributes */

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
public sealed class {TAttr}Attribute : Attribute
{{
	public {TAttr}Attribute(Type? newType = null, Type? forType = null) {{ }}
}}

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class {IntegrityAttr}Attribute : Attribute {{ }}

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class {CombineWithAttr}Attribute : Attribute
{{
	public {CombineWithAttr}Attribute(string parameterName) {{ }}
}}
";
}
