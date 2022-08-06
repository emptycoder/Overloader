using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.Abstractions;
using Overloader.Entities;
using Overloader.Enums;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

internal sealed class GenerateTypeOverloads : IChainObj
{
	ChainResult IChainObj.Execute(GeneratorSourceBuilder gsb)
	{
		if (gsb.Store.OverloadMap is null || !gsb.Store.IsSmthChanged || gsb.Template is null) return ChainResult.NextChainMember;

		var entry = (MethodDeclarationSyntax) gsb.Entry;
		var parameters = entry.ParameterList.Parameters;

		// TODO: Insert attributes
		gsb.Append($"{entry.Modifiers.ToFullString()}{gsb.Store.ReturnType.ToFullString()}{entry.Identifier.ToFullString()}(");
		for (int index = 0; index < parameters.Count; index++)
		{
			var mappedParam = gsb.Store.OverloadMap[index];
			string paramName = parameters[index].Identifier.ToFullString();
			INamedTypeSymbol? originalType;
			switch (mappedParam.ParameterAction)
			{
				case ParameterAction.Nothing:
					gsb.Append(paramName);
					break;
				case ParameterAction.SimpleReplacement:
				case ParameterAction.CustomReplacement:
					gsb.Append($"{mappedParam.Type.ToDisplayString()} {paramName}");
					break;
				case ParameterAction.FormatterReplacement:
					originalType = (INamedTypeSymbol) mappedParam.Type.OriginalDefinition.OriginalDefinition;
					AppendFormatter(originalType, gsb.Formatters[originalType]);
					break;
				case ParameterAction.GlobalFormatterReplacement:
					originalType = (INamedTypeSymbol) mappedParam.Type.OriginalDefinition.OriginalDefinition;
					AppendFormatter(originalType, gsb.GlobalFormatters[originalType]);
					break;
				default:
					throw new ArgumentException($"Can't find case for {gsb.Store.OverloadMap[index]} parameterAction.");
			}

			// ReSharper disable once VariableHidesOuterVariable
			void AppendFormatter(INamedTypeSymbol originalType, Formatter formatter)
			{
				var @params = new ITypeSymbol[formatter.GenericParamsCount];
				for (int paramIndex = 0; paramIndex < formatter.GenericParamsCount; paramIndex++)
					@params[paramIndex] = formatter.GetGenericParamByIndex(paramIndex, gsb.Template) ??
					                      throw new Exception("Can't get type");
				gsb.Append($"{originalType.Construct(@params).ToDisplayString()} {paramName}");
			}
		}

		gsb.Append(")", 1);
		gsb.WriteMethodBody(entry, null);

		return ChainResult.NextChainMember;
	}
}
