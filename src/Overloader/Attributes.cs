// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedParameter.Local
namespace Overloader;

public static class AttributeNames
{
	public static readonly string PartialOverloadsAttr = nameof(PartialOverloadsAttribute).Replace("Attribute", "");
	public static readonly string NewClassOverloadsAttr = nameof(NewClassOverloadAttribute).Replace("Attribute", "");
		
	public static readonly string CustomOverloadAttr = nameof(CustomOverloadAttribute).Replace("Attribute", "");
	// ReSharper disable once InconsistentNaming
	public static readonly string TAttr = nameof(TAttribute).Replace("Attribute", "");
	public static readonly string IgnoreForAttr = nameof(IgnoreForAttribute).Replace("Attribute", "");
	public static readonly string ChangeAccessModifierAttr = nameof(ChangeAccessModifierAttribute).Replace("Attribute", "");
}

public sealed class PartialOverloadsAttribute : Attribute
{
	public PartialOverloadsAttribute(params Type[] types) {}
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class NewClassOverloadAttribute : Attribute
{
	public NewClassOverloadAttribute(string nameRegex, string regexReplace, Type type) {}
}

public sealed class CustomOverloadAttribute : Attribute
{
	public CustomOverloadAttribute(Type type, string[] variables, bool[] genericOverloads) {}
}

// ReSharper disable once InconsistentNaming
public sealed class TAttribute : Attribute
{
	public TAttribute(Type? newType = null, Type? forType = null) {}
}

public sealed class IgnoreForAttribute : Attribute
{
	public IgnoreForAttribute(Type? type) {}
}

public sealed class ChangeAccessModifierAttribute : Attribute
{
	public ChangeAccessModifierAttribute(string newModifier, Type? forType) {}
}
