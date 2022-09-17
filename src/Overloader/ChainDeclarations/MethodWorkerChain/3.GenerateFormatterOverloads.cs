using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.MethodWorkerChain.Utils;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

internal sealed class GenerateFormatterOverloads : IChainObj
{
	ChainResult IChainObj.Execute(GeneratorSourceBuilder gsb)
	{
		if (gsb.Store.OverloadMap is null || gsb.Store.Modifiers is null || gsb.Store.FormattersWoIntegrityCount == 0)
			return ChainResult.NextChainMember;

		var entry = (MethodDeclarationSyntax) gsb.Entry;

		gsb.Append(entry.AttributeLists.ToFullString(), 1)
			.AppendWith(string.Join(" ", gsb.Store.Modifiers), " ")
			.AppendWith(entry.ReturnType.GetPreTypeValues(), " ")
			.AppendWith(gsb.Store.ReturnType.ToDisplayString(), " ")
			.Append(entry.Identifier.ToFullString())
			.Append("(");

		int replacementVariableIndex = 0;
		var replacementVariableNames = new (string Replacment, string ConcatedParams)[gsb.Store.FormattersWoIntegrityCount];
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
				var parameter = parameters[index];
				gsb.AppendWith(parameter.AttributeLists.ToFullString(), " ");

				switch (mappedParam.ParameterAction)
				{
					case ParameterAction.FormatterIntegrityReplacement when gsb.Template is null:
					case ParameterAction.Nothing:
						gsb.Append(parameter.WithAttributeLists(new SyntaxList<AttributeListSyntax>()).ToFullString());
						break;
					case ParameterAction.SimpleReplacement:
					case ParameterAction.CustomReplacement:
						gsb.AppendWith(parameter.Type!.GetType(gsb.Compilation)
								.SetRootType(mappedParam.Type, gsb.Compilation).ToDisplayString(), " ")
							.Append(parameter.Identifier.ToString());
						break;
					case ParameterAction.FormatterIntegrityReplacement:
						gsb.AppendFormatterIntegrity(mappedParam.Type, parameter);
						break;
					case ParameterAction.FormatterReplacement:
						string paramName = parameter.Identifier.ToString();
						// ReSharper disable once IdentifierTypo
						string concatedParams = gsb.AppendFormatter(mappedParam.Type, paramName);
						replacementVariableNames[replacementVariableIndex++] = (paramName, concatedParams);
						break;
					default:
						throw new ArgumentException($"Can't find case for {gsb.Store.OverloadMap[index]} parameterAction.")
							.WithLocation(parameter.GetLocation());
				}
			}
		}

		gsb.Append(")")
			.WriteMethodBody(entry, replacementVariableNames);

		return ChainResult.NextChainMember;
	}
}
