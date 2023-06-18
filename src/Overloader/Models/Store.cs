namespace Overloader.Models;

public sealed class Store
{
	public byte CombineParametersCount;
	public byte FormattersIntegrityCount;
	public byte FormattersWoIntegrityCount;
	public bool IsNeedToRemoveBody;
	public bool IsSmthChanged;
	public MethodData MethodData;
	public ParameterData[]? OverloadMap;
	public bool SkipMember;
}
