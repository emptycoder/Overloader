using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.MethodWorkerChain.Utils;
using Overloader.Entities;
using Overloader.Entities.Builders;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

internal sealed class CombinedDeconstructOverload : IChainMember {
	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		if (props.Store.OverloadMap is null
		    || props.Store.Modifiers is null
		    || props.Store.FormattersWoIntegrityCount == 0
		    || props.Store.CombineParametersCount == 0)
			return ChainAction.NextMember;
		
		if (!props.Store.OverloadMap.Any(param =>
			    param.ParameterAction is ParameterAction.FormatterReplacement
			    && param.CombineIndex == -1))
			return ChainAction.NextMember;

		var entry = (MethodDeclarationSyntax) syntaxNode;
		var parameters = entry.ParameterList.Parameters;

		using var bodyBuilder = SourceBuilder.GetInstance();
		bodyBuilder
			.Append(entry.Identifier.ToString())
			.Append("(");

		props.Builder
			.AppendStepNameComment(nameof(CombinedDeconstructOverload))
			.AppendMethodDeclarationSpecifics(entry, props.Store.Modifiers, props.Store.ReturnType)
			.Append("(");
		
		for (int index = 0;;)
		{
			var parameter = parameters[index];
			var mappedParam = props.Store.OverloadMap[index];
			if (mappedParam.CombineIndex == -1)
			{
				string paramName = parameter.Identifier.ToString();
				switch (mappedParam.ParameterAction)
				{
					case ParameterAction.FormatterIntegrityReplacement when props.Template is null:
					case ParameterAction.Nothing:
						props.Builder.Append(parameter.ToFullString());
						bodyBuilder.AppendVariableToBody(parameter, paramName);
						break;
					case ParameterAction.SimpleReplacement:
					case ParameterAction.CustomReplacement:
						props.Builder.AppendParameter(parameter, mappedParam.Type, props.Compilation);
						bodyBuilder.AppendVariableToBody(parameter, paramName);
						break;
					case ParameterAction.FormatterIntegrityReplacement:
						props.Builder.AppendIntegrityParam(props, mappedParam.Type, parameter);
						bodyBuilder.AppendVariableToBody(parameter, paramName);
						break;
					case ParameterAction.FormatterReplacement:
						var concatedParams = props.Builder.AppendFormatterParam(props, mappedParam.Type, paramName);
						bodyBuilder.AppendWoTrim(concatedParams);
						break;
					default:
						throw new ArgumentException($"Can't find case for {props.Store.OverloadMap[index]} parameterAction.")
							.WithLocation(parameter);
				}

				if (++index == parameters.Count) break;
				if (props.Store.OverloadMap[index].CombineIndex == -1)
					props.Builder.AppendWoTrim(", ");
				bodyBuilder.AppendWoTrim(", ");
			}
			else
			{
				bodyBuilder.AppendCombined(props, mappedParam, parameters[mappedParam.CombineIndex]);
				if (++index == parameters.Count) break;
				bodyBuilder.AppendWoTrim(", ");
			}
		}

		props.Builder.Append(")")
			.AppendWoTrim(" =>\n\t")
			.AppendWoTrim(entry.ReturnType.GetPreTypeValues())
			.Append(bodyBuilder.ToString())
			.AppendWoTrim(");", 1);

		return ChainAction.NextMember;
	}
}
