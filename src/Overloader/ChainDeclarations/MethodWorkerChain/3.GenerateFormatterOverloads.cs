﻿using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.Abstractions;
using Overloader.Entities;
using Overloader.Enums;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

public sealed class GenerateFormatterOverloads : IChainObj
{
	public ChainResult Execute(GeneratorSourceBuilder gsb)
	{
		if (gsb.Store.OverloadMap is null || !gsb.Store.IsAnyFormatter) return ChainResult.NextChainMember;

		var entry = (MethodDeclarationSyntax) gsb.Entry;
		// TODO: Insert attributes
		gsb.Append($"{entry.Modifiers.ToFullString()}{entry.ReturnType.ToFullString()}{entry.Identifier.ToFullString()}(");
		var replacementModifiers = new List<(string, string)>();
		var parameters = entry.ParameterList.Parameters;

		for (int index = 0; index < parameters.Count; index++)
		{
			var mappedParam = gsb.Store.OverloadMap[index];
			string paramName = parameters[index].Identifier.ToFullString();
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
					AppendFormatter(gsb.Formatters[mappedParam.Type.OriginalDefinition]);
					break;
				case ParameterAction.GlobalFormatterReplacement:
					AppendFormatter(gsb.GlobalFormatters[mappedParam.Type.OriginalDefinition]);
					break;
				default:
					throw new ArgumentException($"Can't find case for {gsb.Store.OverloadMap[index]} parameterAction.");
			}

			void AppendFormatter(Formatter formatter)
			{
				for (int paramIndex = 0; paramIndex < formatter.ParamsCount; paramIndex++)
				{
					var formatterParam = formatter.GetParamByIndex(paramIndex, gsb.Template);
					gsb.Append($"{formatterParam.Type} {paramName}{formatterParam.Name}, ");
					replacementModifiers.Add(($"{paramName}.{formatterParam.Name}",
						$"{paramName}{formatterParam.Name}"));
				}
			}
		}

		gsb.Append(")", 1);
		gsb.WriteMethodBody(entry, replacementModifiers);

		return ChainResult.NextChainMember;
	}
}
