using System.Buffers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.MethodWorkerChain.Utils;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

/// <summary>
/// Generate main overload which decompose method on simple params using formatters
/// </summary>
internal sealed class DeconstructOverload : IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		if (props.Store.OverloadMap is null
		    || props.Store.Modifiers is null
		    || props.Store.FormattersWoIntegrityCount == 0)
			return ChainAction.NextMember;

		var entry = (MethodDeclarationSyntax) syntaxNode;
		var parameters = entry.ParameterList.Parameters;
		
		int replacementVariableIndex = 0;
		var replacementVariableNames = ArrayPool<(string Replacement, string ConcatedParams)>
			.Shared.Rent(props.Store.FormattersWoIntegrityCount);

		props.Builder.AppendMethodDeclarationSpecifics(entry, props.Store.Modifiers, props.Store.ReturnType)
			.Append("(");

		for (int index = 0;;)
		{
			var mappedParam = props.Store.OverloadMap[index];
			var parameter = parameters[index];

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
					string paramName = parameter.Identifier.ToString();
					string concatedParams = props.AppendFormatterParam(mappedParam.Type, paramName);
					replacementVariableNames[replacementVariableIndex++] = (paramName, concatedParams);
					break;
				default:
					throw new ArgumentException($"Can't find case for {props.Store.OverloadMap[index]} parameterAction.")
						.WithLocation(parameter);
			}
			
			if (++index == parameters.Count) break;
			props.Builder.AppendWoTrim(", ");
		}

		props.Builder.Append(")");
		props.WriteMethodBody(entry, replacementVariableNames.AsSpan(0, replacementVariableIndex));
		ArrayPool<(string, string)>.Shared.Return(replacementVariableNames);

		return ChainAction.NextMember;
	}
}
