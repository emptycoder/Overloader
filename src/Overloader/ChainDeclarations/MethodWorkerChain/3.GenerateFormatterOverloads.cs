using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.MethodWorkerChain.Utils;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

internal sealed class GenerateFormatterOverloads : IChainObj
{
	ChainResult IChainObj.Execute(GeneratorSourceBuilder gsb)
	{
		if (gsb.Store.OverloadMap is null || !gsb.Store.IsAnyFormatter) return ChainResult.NextChainMember;

		var entry = (MethodDeclarationSyntax) gsb.Entry;
		// TODO: Insert attributes
		// TODO: Insert this, ref, in, attrs for parameters
		gsb.AppendWith(entry.Modifiers.ToFullString(), " ")
			.AppendWith(entry.ReturnType.GetPreTypeValues(), " ")
			.AppendWith(gsb.Store.ReturnType.ToDisplayString(), " ")
			.Append(entry.Identifier.ToFullString())
			.Append("(");

		var replacementModifiers = new List<(string, string)>();
		var parameters = entry.ParameterList.Parameters;

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
						gsb.Append(parameters[index].ToString());
						break;
					case ParameterAction.SimpleReplacement:
					case ParameterAction.CustomReplacement:
						gsb.AppendWith(mappedParam.Type.ToDisplayString(), " ")
							.Append(parameters[index].Identifier.ToString());
						break;
					case ParameterAction.FormatterIntegrityReplacement:
						gsb.AppendFormatterIntegrity(mappedParam.Type, parameters[index]);
						break;
					case ParameterAction.FormatterReplacement:
						gsb.AppendFormatter(mappedParam.Type,
							parameters[index].Identifier.ToString(),
							replacementModifiers);
						break;
					default:
						throw new ArgumentException($"Can't find case for {gsb.Store.OverloadMap[index]} parameterAction.");
				}
			}
		}

		gsb.Append(")", 1)
			.WriteMethodBody(entry, replacementModifiers);

		return ChainResult.NextChainMember;
	}
}
