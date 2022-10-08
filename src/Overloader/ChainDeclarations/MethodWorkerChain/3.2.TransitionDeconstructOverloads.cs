using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.MethodWorkerChain.Utils;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

/// <summary>
/// Generate transitions for types that don't use specific methods and can be decomposed
/// </summary>
internal sealed class TransitionDeconstructOverloads : IChainMember {
	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		if (props.Store.OverloadMap is null
		    || props.Store.Modifiers is null
		    || props.Store.FormattersWoIntegrityCount == 0)
			return ChainAction.NextMember;

		var entry = (MethodDeclarationSyntax) syntaxNode;
		var parameters = entry.ParameterList.Parameters;
		
		Span<int> maxTransitionsCount = stackalloc int[props.Store.FormattersWoIntegrityCount];
		for (int index = 0, formatterIndex = 0; index < parameters.Count; index++)
		{
			var parameter = parameters[index];
			var mappedParam = props.Store.OverloadMap![index];
			if (mappedParam.ParameterAction != ParameterAction.FormatterReplacement) continue;
			if (!props.TryGetFormatter(parameter.GetType(props.Compilation), out var formatter))
				throw new ArgumentException($"Formatter not found for {parameter.Identifier.ToString()}")
					.WithLocation(parameter.GetLocation());
			maxTransitionsCount[formatterIndex++] = formatter.Transitions.Length;
		}

		for (int index = 0;;)
		{
			if (maxTransitionsCount[index] != 0) break;
			if (++index == maxTransitionsCount.Length) return ChainAction.NextMember;
		}
		
		Span<int> transitionIndexes = stackalloc int[props.Store.FormattersWoIntegrityCount];
		using var bodyBuilder = SourceBuilder.GetInstance();
		for (;;)
		{
			props.Builder.AppendMethodDeclarationSpecifics(entry, props.Store.Modifiers, props.Store.ReturnType)
				.Append("(");
			WriteOverload(props.Builder, bodyBuilder, props, parameters, transitionIndexes);
			props.Builder.Append(")")
				.AppendWoTrim(" =>\n\t")
				.Append(bodyBuilder.ToStringAndClear())
				.Append(");");
			
			/*
				0 0 0 0 0
				^

				1 0 0 0 0
				^
				Repeat until it < maxLength[index]
				And when first value equals maxLength[index] reset to zero and add 1 to next rank
				0 1 0 0 0
				^
				
				1 1 0 0 0
				^
				And so on...
			 */
			for (int index = 0;;)
			{
				if (++transitionIndexes[index] != maxTransitionsCount[index]) continue;
				transitionIndexes[index] = 0;

				if (++index == transitionIndexes.Length)
					return ChainAction.NextMember;
			}
		}
	}

	private static void WriteOverload(SourceBuilder headerBuilder,
		SourceBuilder bodyBuilder,
		GeneratorProperties props,
		SeparatedSyntaxList<ParameterSyntax> parameters,
		Span<int> transitionIndexes)
	{
		for (int index = 0, paramIndex = 0; index < parameters.Count; index++)
		{
			var mappedParam = props.Store.OverloadMap![index];
			var parameter = parameters[index];

			string paramName = parameter.Identifier.ToString();
			switch (mappedParam.ParameterAction)
			{
				case ParameterAction.FormatterIntegrityReplacement when props.Template is null:
				case ParameterAction.Nothing:
					headerBuilder.Append(parameter.ToFullString());
					bodyBuilder.AppendWoTrim(paramName);
					break;
				case ParameterAction.SimpleReplacement:
				case ParameterAction.CustomReplacement:
					headerBuilder.AppendParameter(parameter, mappedParam.Type, props.Compilation);
					bodyBuilder.AppendWoTrim(paramName);
					break;
				case ParameterAction.FormatterIntegrityReplacement:
					headerBuilder.AppendIntegrityParam(props, mappedParam.Type, parameter);
					bodyBuilder.AppendWoTrim(paramName);
					break;
				case ParameterAction.FormatterReplacement:
					// string concatedParams = props.AppendFormatterParam(mappedParam.Type, paramName);
					if (!props.TryGetFormatter(parameter.GetType(props.Compilation), out var formatter))
						throw new ArgumentException("Unexpected exception. Formatters changed in real time.")
							.WithLocation(parameter);
					
					var transition = formatter.Transitions[transitionIndexes[paramIndex++]];
					for (int linkIndex = 0;;)
					{
						var transitionLink = transition.Links[linkIndex];
						string variableName = $"{paramName}{linkIndex.ToString()}";
						headerBuilder.AppendWith(transitionLink.Type.ToDisplayString(), " ")
							.Append(variableName);

						if (++linkIndex == transition.Links.Length) break;
						headerBuilder.AppendWoTrim(", ");
					}
					
					for (int transitionParamIndex = 0;;)
					{
						var formatterParam = formatter.Params[transitionParamIndex];
						var replacement = transition.FindReplacement(formatterParam.Name, out int linkIndex);
						bodyBuilder.Append($"{paramName}{linkIndex.ToString()}.{replacement}");
						
						if (++transitionParamIndex == formatter.Params.Length) break;
						bodyBuilder.AppendWoTrim(", ");
					}

					break;
				default:
					throw new ArgumentException($"Can't find case for {props.Store.OverloadMap[index]} parameterAction.")
						.WithLocation(parameter);
			}
			
			if (++index == parameters.Count) break;
			headerBuilder.AppendWoTrim(", ");
			bodyBuilder.AppendWoTrim(", ");
		}
	}
}
