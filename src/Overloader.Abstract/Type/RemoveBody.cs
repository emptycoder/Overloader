namespace Overloader;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
public class RemoveBody : Attribute
{
	public const string TagName = nameof(RemoveBody);
}
