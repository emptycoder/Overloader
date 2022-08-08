using Overloader.Formatters.Params;

namespace Overloader.Formatters;

internal readonly struct Formatter
{
	public readonly IParam[] GenericParams;
	public readonly IParam[] Params;

	public Formatter() => throw new NotSupportedException("Not allowed!");

	public Formatter(IParam[] genericParams, IParam[] @params)
	{
		GenericParams = genericParams;
		Params = @params;
	}
}
