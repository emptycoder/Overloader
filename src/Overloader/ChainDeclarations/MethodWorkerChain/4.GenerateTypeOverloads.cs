using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.MethodWorkerChain.Utils;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

internal sealed class GenerateTypeOverloads : IChainObj
{
	ChainResult IChainObj.Execute(GeneratorSourceBuilder gsb)
	{
		if (gsb.Store.OverloadMap is null || !gsb.Store.IsSmthChanged || gsb.Template is null) return ChainResult.NextChainMember;

		var entry = (MethodDeclarationSyntax) gsb.Entry;
		var parameters = entry.ParameterList.Parameters;

		// TODO: Insert attributes
		gsb.AppendWith(entry.Modifiers.ToFullString(), " ")
			.AppendWith(entry.ReturnType.GetPreTypeValues(), " ")
			.AppendWith(gsb.Store.ReturnType.ToDisplayString(), " ")
			.Append(entry.Identifier.ToFullString())
			.Append("(");

		if (parameters.Count > 0)
		{
			int index = 0;
			AppendParam();
			for (index = 1; index < parameters.Count; index++)
			{
				gsb.AppendWoTrim(", ");
				AppendParam();
			}

			void AppendParam()
			{
				var mappedParam = gsb.Store.OverloadMap[index];
				switch (mappedParam.ParameterAction)
				{
					case ParameterAction.Nothing:
						gsb.Append(parameters[index].ToFullString());
						break;
					case ParameterAction.SimpleReplacement:
					case ParameterAction.CustomReplacement:
						gsb.AppendWith(mappedParam.Type.ToDisplayString(), " ")
							.Append(parameters[index].Identifier.ToFullString());
						break;
					case ParameterAction.FormatterIntegrityReplacement:
					case ParameterAction.FormatterReplacement:
						gsb.AppendFormatterIntegrity(mappedParam.Type, parameters[index]);
						break;
					default:
						throw new ArgumentException($"Can't find case for {gsb.Store.OverloadMap[index]} parameterAction.");
				}
			}
		}

		gsb.Append(")", 1)
			.WriteMethodBody(entry, ImmutableList<(string From, string To)>.Empty);

		return ChainResult.NextChainMember;
	}
}
