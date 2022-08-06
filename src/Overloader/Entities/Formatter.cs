using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Overloader.Enums;

namespace Overloader.Entities;

internal readonly struct Formatter
{
	private readonly (Template Action, object Data)[] _genericParams;
	private readonly (string Name, Template Action, object Data)[] _params;
	public int ParamsCount => _params.Length;
	public int GenericParamsCount => _genericParams.Length;

	public Formatter() => throw new Exception("Not allowed!");

	public Formatter((Template Action, object Data)[] genericParams, (string Name, Template Action, object Data)[] @params)
	{
		_genericParams = genericParams;
		_params = @params;
	}

	public (string Name, ITypeSymbol? Type) GetParamByIndex(int index, ITypeSymbol? template)
	{
		var variable = _params[index];
		return (variable.Name, GetType(variable.Action, variable.Data, template));
	}

	public ITypeSymbol? GetGenericParamByIndex(int index, ITypeSymbol? template)
	{
		var variable = _genericParams[index];
		return GetType(variable.Action, variable.Data, template);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ITypeSymbol? GetType(Template paramAction, object data, ITypeSymbol? template) =>
		paramAction switch
		{
			Template.Template => template,
			Template.Type => (ITypeSymbol) data,
			Template.Switch when template is null => template,
			Template.Switch => ((Dictionary<ITypeSymbol, ITypeSymbol>) data)[template],
			_ => throw new ArgumentOutOfRangeException()
		};

	public static Formatter CreateFromString(Compilation compilation, string data)
	{
		int startOfGenericParamsPos = data.IndexOf('<');
		int startOfParamsObjectPos = data.IndexOf('{', startOfGenericParamsPos);
		int endOfGenericParamsPos = data.LastIndexOf('>', startOfParamsObjectPos);


		return default;
	}
}
