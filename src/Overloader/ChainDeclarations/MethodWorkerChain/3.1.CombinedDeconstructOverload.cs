using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.MethodWorkerChain.Utils;
using Overloader.Entities;
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

		var entry = (MethodDeclarationSyntax) syntaxNode;
		var parameters = entry.ParameterList.Parameters;

		using var bodyBuilder = SourceBuilder.GetInstance();
		bodyBuilder.Append(entry.Identifier.ToString())
			.Append("(");

		props.Builder.AppendMethodDeclarationSpecifics(entry, props.Store.Modifiers, props.Store.ReturnType)
			.Append("(");
		
		for (int index = 0;;)
		{
			var parameter = parameters[index];
			var mappedParam = props.Store.OverloadMap[index];
			if (mappedParam.CombineWith is null)
			{
				string paramName = parameter.Identifier.ToString();
				switch (mappedParam.ParameterAction)
				{
					case ParameterAction.FormatterIntegrityReplacement when props.Template is null:
					case ParameterAction.Nothing:
						props.Builder.Append(parameter.ToFullString());
						break;
					case ParameterAction.SimpleReplacement:
					case ParameterAction.CustomReplacement:
						props.Builder.AppendParameter(parameter, mappedParam.Type, props.Compilation);
						break;
					case ParameterAction.FormatterIntegrityReplacement:
						props.Builder.AppendIntegrityParam(props, mappedParam.Type, parameter);
						break;
					case ParameterAction.FormatterReplacement:
						props.AppendFormatterParam(mappedParam.Type, paramName);
						break;
					default:
						throw new ArgumentException($"Can't find case for {props.Store.OverloadMap[index]} parameterAction.")
							.WithLocation(parameter);
				}

				bodyBuilder.AppendWoTrim(paramName);
				
				if (++index == parameters.Count) break;
				props.Builder.AppendWoTrim(", ");
				bodyBuilder.AppendWoTrim(", ");
			}
			else
			{
				bodyBuilder.AppendWoTrim(mappedParam.CombineWith);
				if (++index == parameters.Count) break;
				bodyBuilder.AppendWoTrim(", ");
			}
		}

		props.Builder.Append(")")
			.AppendWoTrim(" =>\n\t")
			.Append(bodyBuilder.ToString())
			.Append(");");

		return ChainAction.NextMember;
	}
}
