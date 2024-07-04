using Microsoft.CodeAnalysis;
using Overloader.Entities;
using Overloader.Entities.DTOs;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Chains.Overloads;

public sealed class AnalyzeMethodParams : IChainMember
{
	unsafe ChainAction IChainMember.Execute(GeneratorProperties props)
	{
		props.Store.CombineParametersCount = 0;

		var parameters = props.Store.MethodSyntax.ParameterList.Parameters;
		props.Store.MethodData.Parameters = new ParameterData[parameters.Count];

		for (int index = 0; index < parameters.Count; index++)
		{
			var parameterType = (parameters[index].Type ?? throw new NullReferenceException(
						$"Parameter {parameters[index].Identifier} type is null.")
					.WithLocation(parameters[index]))
				.GetType(props.Compilation);

			bool shouldBeReplaced = ParameterDto.TryGetParameterDtoByTemplate(parameters[index], props, out var paramDto);
			var parameterAction = shouldBeReplaced switch
			{
				true when paramDto.Attribute!.NewType is not null => RequiredReplacement.UserType,
				true when parameterType is INamedTypeSymbol {IsGenericType: true} =>
					props.TryGetFormatter(parameterType.GetClearType(), out var formatter)
						? paramDto.HasForceOverloadIntegrity || !formatter.Params.Any()
							? RequiredReplacement.FormatterIntegrity
							: RequiredReplacement.Formatter
						: throw new ArgumentException($"Not found formatter for {parameterType}")
							.WithLocation(parameters[index]),
				true when props.TryGetFormatter(parameterType.GetClearType(), out var formatter) =>
					paramDto.HasForceOverloadIntegrity || !formatter.Params.Any() || parameterType is not INamedTypeSymbol
						? RequiredReplacement.FormatterIntegrity
						: RequiredReplacement.Formatter,
				true => RequiredReplacement.Template,
				false => RequiredReplacement.None
			};
			var newParameterType = parameterAction switch
			{
				RequiredReplacement.None => default,
				RequiredReplacement.Template => props.Templates[paramDto.Attribute!.TemplateIndex],
				RequiredReplacement.UserType => paramDto.Attribute!.NewType,
				RequiredReplacement.Formatter => default,
				RequiredReplacement.FormatterIntegrity => default,
				_ => throw new ArgumentOutOfRangeException()
			} ?? parameterType;

			bool isCombineWith = paramDto.CombineWith is not null;
			props.Store.MethodData.Parameters[index] = new ParameterData(
				paramDto.Attribute?.TemplateIndex ?? 0,
				parameterAction,
				newParameterType,
				paramDto.ModifierChangers,
				isCombineWith
					? (byte) parameters.IndexOf(param => param.Identifier.ValueText == paramDto.CombineWith)
					: Byte.MaxValue);

			props.Store.CombineParametersCount += *(byte*) &isCombineWith;
			props.Store.IsSmthChanged |= shouldBeReplaced;
		}

		props.Store.XmlDocumentation = XmlDocumentation.Parse(props.Store.MethodSyntax.GetLeadingTrivia());
		return props.Store.IsSmthChanged
			? ChainAction.NextMember
			: ChainAction.Break;
	}
}
