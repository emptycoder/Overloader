﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.DTOs;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Models;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

public sealed class AnalyzeMethodParams : IChainMember
{
	unsafe ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		props.Store.FormattersWoIntegrityCount = 0;
		props.Store.FormattersIntegrityCount = 0;
		props.Store.CombineParametersCount = 0;

		var entry = (MethodDeclarationSyntax) syntaxNode;
		var parameters = entry.ParameterList.Parameters;
		props.Store.OverloadMap = new ParameterData[parameters.Count];

		for (int index = 0; index < parameters.Count; index++)
		{
			var parameterType = (parameters[index].Type ?? throw new NullReferenceException(
						$"Parameter {parameters[index].Identifier} type is null.")
					.WithLocation(parameters[index]))
				.GetType(props.Compilation);

			bool shouldBeReplaced = ParameterDto.TryGetParameterDtoByTemplate(parameters[index], props, out var paramDto);
			var parameterAction = shouldBeReplaced switch
			{
				true when paramDto.Attribute.ArgumentList is {Arguments.Count: >= 1} => ParameterAction.CustomReplacement,
				true when parameterType is INamedTypeSymbol { IsGenericType: true } =>
					props.TryGetFormatter(parameterType.GetClearType(), out var formatter)
						? paramDto.HasForceOverloadIntegrity || !formatter.Params.Any()
							? ParameterAction.FormatterIntegrityReplacement
							: ParameterAction.FormatterReplacement
						: throw new ArgumentException($"Not found formatter for {parameterType}")
							.WithLocation(parameters[index]),
				true when props.TryGetFormatter(parameterType.GetClearType(), out var formatter) =>
					paramDto.HasForceOverloadIntegrity || !formatter.Params.Any() || parameterType is not INamedTypeSymbol
						? ParameterAction.FormatterIntegrityReplacement
						: ParameterAction.FormatterReplacement,
				true => ParameterAction.SimpleReplacement,
				false => ParameterAction.Nothing
			};
			var newParameterType = parameterAction switch
			{
				ParameterAction.Nothing => default,
				ParameterAction.SimpleReplacement => props.Template,
				ParameterAction.CustomReplacement => paramDto.Attribute.ArgumentList!.Arguments[0].GetType(props.Compilation),
				ParameterAction.FormatterReplacement => default,
				ParameterAction.FormatterIntegrityReplacement => default,
				_ => throw new ArgumentOutOfRangeException()
			} ?? parameterType;

			bool isCombineWith = paramDto.CombineWith is not null;
			props.Store.OverloadMap[index] = new ParameterData(
				parameterAction,
				newParameterType,
				paramDto.ModifierChangers,
				isCombineWith
					? (byte) parameters.IndexOf(param => param.Identifier.ValueText == paramDto.CombineWith)
					: Byte.MaxValue);

			bool isFormatterWoIntegrity = parameterAction is ParameterAction.FormatterReplacement;
			bool isFormatterIntegrity = parameterAction is ParameterAction.FormatterIntegrityReplacement;

			props.Store.FormattersWoIntegrityCount += *(byte*) &isFormatterWoIntegrity;
			props.Store.FormattersIntegrityCount += *(byte*) &isFormatterIntegrity;
			props.Store.CombineParametersCount += *(byte*) &isCombineWith;
			props.Store.IsSmthChanged |= shouldBeReplaced;
		}

		return ChainAction.NextMember;
	}
}
