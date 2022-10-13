using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.MethodWorkerChain.Utils;
using Overloader.Entities;
using Overloader.Entities.Builders;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

internal sealed  class CombinedIntegrityOverload : IChainMember {
	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		if (props.Store.OverloadMap is null
		    || props.Store.Modifiers is null
		    || !props.Store.IsSmthChanged
		    || props.Template is null
		    || props.Store.CombineParametersCount == 0)
			return ChainAction.NextMember;

		var entry = (MethodDeclarationSyntax) syntaxNode;
		var parameters = entry.ParameterList.Parameters;

		using var bodyBuilder = SourceBuilder.GetInstance();
		bodyBuilder.Append(entry.Identifier.ToString())
			.Append("(");
		
		props.Builder
			.AppendStepNameComment(nameof(CombinedIntegrityOverload))
			.AppendMethodDeclarationSpecifics(entry, props.Store.Modifiers, props.Store.ReturnType)
			.Append("(");
		
		if (parameters.Count == 0) goto CloseParameterBracket;

		for (int index = 0;;)
		{
			var parameter = parameters[index];
			var mappedParam = props.Store.OverloadMap[index];

			if (mappedParam.CombineIndex == -1)
			{
				string paramName = parameter.Identifier.ToString();
				switch (mappedParam.ParameterAction)
				{
					case ParameterAction.Nothing:
						props.Builder.Append(parameter.ToFullString());
						break;
					case ParameterAction.SimpleReplacement:
					case ParameterAction.CustomReplacement:
						props.Builder.AppendParameter(parameter, mappedParam.Type, props.Compilation);
						break;
					case ParameterAction.FormatterIntegrityReplacement:
					case ParameterAction.FormatterReplacement:
						props.Builder.AppendIntegrityParam(props, mappedParam.Type, parameter);
						break;
					default:
						throw new ArgumentException($"Can't find case for {props.Store.OverloadMap[index]} parameterAction.")
							.WithLocation(entry);
				}
				
				bodyBuilder.AppendVariableToBody(parameter, paramName);
				
				if (++index == parameters.Count) break;
				if (props.Store.OverloadMap[index].CombineIndex == -1)
					props.Builder.AppendWoTrim(", ");
				bodyBuilder.AppendWoTrim(", ");
			}
			else
			{
				bodyBuilder.AppendCombinedSimple(mappedParam, parameters[mappedParam.CombineIndex]);
				if (++index == parameters.Count) break;
				bodyBuilder.AppendWoTrim(", ");
			}
		}

		CloseParameterBracket:
		props.Builder.Append(")")
			.AppendWoTrim(" =>\n\t")
			.AppendWoTrim(entry.ReturnType.GetPreTypeValues())
			.Append(bodyBuilder.ToString())
			.AppendWoTrim(");", 1);

		return ChainAction.NextMember;
	}
}
