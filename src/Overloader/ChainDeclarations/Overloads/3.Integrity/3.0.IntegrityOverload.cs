using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.Overloads.Utils;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;

namespace Overloader.ChainDeclarations.Overloads;

public sealed class IntegrityOverload : IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		if (props.Store.OverloadMap is null
		    || !props.Store.IsSmthChanged
		    || props.IsTSpecified)
			return ChainAction.NextMember;

		var entry = (MethodDeclarationSyntax) syntaxNode;
		var parameters = entry.ParameterList.Parameters;
		
		props.Builder
			.AppendChainMemberNameComment(nameof(IntegrityOverload))
			.Append(entry.GetLeadingTrivia().ToString(), 1)
			.AppendMethodDeclarationSpecifics(entry, props.Store.MethodData)
			.AppendAsConstant("(");

		if (parameters.Count == 0) goto CloseParameterBracket;

		for (int index = 0;;)
		{
			var mappedParam = props.Store.OverloadMap[index];
			var parameter = parameters[index];

			switch (mappedParam.ParameterAction)
			{
				case ParameterAction.Nothing:
					props.Builder.Append(parameter.ToFullString());
					break;
				case ParameterAction.SimpleReplacement:
				case ParameterAction.CustomReplacement:
					props.Builder.AppendParameter(parameter, mappedParam, props.Compilation);
					break;
				case ParameterAction.FormatterIntegrityReplacement:
				case ParameterAction.FormatterReplacement:
					props.Builder.AppendIntegrityParam(props, mappedParam, parameter);
					break;
				default:
					throw new ArgumentException($"Can't find case for {props.Store.OverloadMap[index]} parameterAction.")
						.WithLocation(entry);
			}

			if (++index == parameters.Count) break;
			props.Builder
				.AppendAsConstant(",")
				.WhiteSpace();
		}

		CloseParameterBracket:
		props.Builder
			.AppendAsConstant(")")
			.WhiteSpace()
			.Append(entry.ConstraintClauses.ToString());
		props.WriteMethodBody(entry, Array.Empty<(string, string)>());

		return ChainAction.NextMember;
	}
}
