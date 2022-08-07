using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Overloader.Formatters.Params;
using Overloader.Utils;

namespace Overloader.Formatters;

internal readonly struct Formatter
{
	public readonly IParam[] GenericParams;
	public readonly IParam[] Params;

	public Formatter() => throw new NotSupportedException("Not allowed!");
	private Formatter(IParam[] genericParams, IParam[] @params)
	{
		GenericParams = genericParams;
		Params = @params;
	}
	public static Formatter CreateFromString(Compilation compilation, string data)
	{
		System.Diagnostics.Debugger.Break();
		
		var genericParamsData = BlockInfo(0, data, stackalloc char[] { '{', '}' }, '<', '>');
		var genericParams = genericParamsData.BlockCount == 0? Array.Empty<IParam>() : new IParam[genericParamsData.BlockCount];

		int paramsStartPos = data.IndexOf('{', genericParamsData.EndPos);
		if (paramsStartPos == -1) throw new ArgumentException();
		
		var paramsData = BlockInfo(paramsStartPos, data, ReadOnlySpan<char>.Empty);
		var @params = paramsData.BlockCount == 0? Array.Empty<IParam>() : new IParam[paramsData.BlockCount];
		
		if (genericParams.Length != 0)
			FillData(data.AsSpan().Slice(0, genericParamsData.EndPos),
				compilation, genericParams, false);
		if (@params.Length != 0)
			FillData(data.AsSpan().Slice(paramsStartPos, paramsData.EndPos - paramsStartPos),
				compilation, @params, true);

		return new Formatter(genericParams, @params);
	}

	private static void FillData(ReadOnlySpan<char> data, Compilation compilation, IParam[] target, bool withNames)
	{
		int nested = 0, paramIndex = 0, startIndex = 0;
		for (int index = 0; index < data.Length; index++)
		{
			switch (data[index])
			{
				case ',' when nested == 0:
					target[paramIndex] = CreateParam(data.Slice(startIndex), compilation, withNames);
					paramIndex++;
					startIndex = index + 1;
					break;
				// For switch type statements
				case '{':
					nested++;
					break;
				case '}':
					nested--;
					break;
			}
		}

		target[paramIndex] = CreateParam(data.Slice(startIndex), compilation, withNames);
	}

	private static IParam CreateParam(ReadOnlySpan<char> data, Compilation compilation, bool withNames)
	{
		string? name = null;
		if (withNames)
		{
			var nameEndIndex = data.IndexOf(':');
			if (nameEndIndex == -1) throw new ArgumentException();
			name = data.Slice(0, nameEndIndex).ToString();
			data = data.Slice(nameEndIndex);
		}

		string dataValue = data.ToString().Trim();
		if (dataValue == "T")  return TemplateParam.Create(name);
		if (dataValue.StartsWith("{"))
		{
			
			return default!;
		}

		return TypeParam.Create(SyntaxFactory.ParseTypeName(dataValue).GetType(compilation), name);
	}

	private static (int BlockCount, int EndPos) BlockInfo(int startIndex, string data, ReadOnlySpan<char> breakChars, char openChar = '{', char closeChar = '}')
	{
		bool blockFound = false;
		int nested = 0, commaCount = 0, index = startIndex;
		for (; index < data.Length; index++)
		{
			var character = data[index];
			if (character == openChar)
			{
				blockFound = true;
				nested++;
			}
			else if (character == closeChar)
				switch (nested--)
				{
					case 0: goto Result;
					case < 0: throw new ArgumentException();
				}
			else if (character == ',' && nested == 1) commaCount++;
			else if (breakChars.IndexOf(character) != -1) goto Result;
		}

		Result:
		if (nested != 0) throw new ArgumentException();
		return (commaCount + (blockFound? 1 : 0), blockFound? index : startIndex);
	}
}
