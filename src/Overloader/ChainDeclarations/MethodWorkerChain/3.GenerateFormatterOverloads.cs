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
	ChainResult IChainObj.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		var gsb = props.Builder;
		if (props.Store.OverloadMap is null || props.Store.Modifiers is null || props.Store.FormattersWoIntegrityCount == 0)
			return ChainResult.NextChainMember;

		var entry = (MethodDeclarationSyntax) syntaxNode;

		gsb.Append(entry.AttributeLists.ToFullString(), 1)
			.AppendWith(string.Join(" ", props.Store.Modifiers), " ")
			.AppendWith(entry.ReturnType.GetPreTypeValues(), " ")
			.AppendWith(props.Store.ReturnType.ToDisplayString(), " ")
			.Append(entry.Identifier.ToFullString())
			.Append("(");

		int replacementVariableIndex = 0;
		var replacementVariableNames = new (string Replacment, string ConcatedParams)[props.Store.FormattersWoIntegrityCount];
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
				var mappedParam = props.Store.OverloadMap[index];
				var parameter = parameters[index];
				gsb.AppendWith(parameter.AttributeLists.ToFullString(), " ");

				switch (mappedParam.ParameterAction)
				{
					case ParameterAction.FormatterIntegrityReplacement when props.Template is null:
					case ParameterAction.Nothing:
						gsb.Append(parameter.WithAttributeLists(new SyntaxList<AttributeListSyntax>()).ToFullString());
						break;
					case ParameterAction.SimpleReplacement:
					case ParameterAction.CustomReplacement:
						gsb.AppendWith(parameter.Type!.GetType(props.Compilation)
								.SetRootType(mappedParam.Type, props.Compilation).ToDisplayString(), " ")
							.Append(parameter.Identifier.ToString());
						break;
					case ParameterAction.FormatterIntegrityReplacement:
						props.AppendFormatterIntegrity(mappedParam.Type, parameter);
						break;
					case ParameterAction.FormatterReplacement:
						string paramName = parameter.Identifier.ToString();
						// ReSharper disable once IdentifierTypo
						string concatedParams = props.AppendFormatter(mappedParam.Type, paramName);
						replacementVariableNames[replacementVariableIndex++] = (paramName, concatedParams);
						break;
					default:
						throw new ArgumentException($"Can't find case for {props.Store.OverloadMap[index]} parameterAction.")
							.WithLocation(parameter.GetLocation());
				}
			}
		}

		gsb.Append(")");
		props.WriteMethodBody(entry, replacementVariableNames);

		return ChainResult.NextChainMember;
	}
}
