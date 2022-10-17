﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

internal sealed class AnalyzeMethodParams : IChainMember
{
	unsafe ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		props.Store.FormattersWoIntegrityCount = 0;
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
			
			bool shouldBeReplaced = parameters[index].TryGetTAttrByTemplate(props, out var tAttrDto);
			var parameterAction = shouldBeReplaced switch
			{
				true when props.TryGetFormatter(parameterType.GetClearType(), out var formatter) =>
					tAttrDto.ForceOverloadIntegrity || !formatter.Params.Any() || parameterType is not INamedTypeSymbol
						? ParameterAction.FormatterIntegrityReplacement
						: ParameterAction.FormatterReplacement,
				true when tAttrDto.Attribute.ArgumentList is {Arguments.Count: >= 1} => ParameterAction.CustomReplacement,
				true => ParameterAction.SimpleReplacement,
				false => ParameterAction.Nothing
			};
			var newParameterType = parameterAction switch
			{
				ParameterAction.Nothing => default,
				ParameterAction.SimpleReplacement => props.Template,
				ParameterAction.CustomReplacement => tAttrDto.Attribute.ArgumentList!.Arguments[0].GetType(props.Compilation),
				ParameterAction.FormatterReplacement => default,
				ParameterAction.FormatterIntegrityReplacement => default,
				_ => throw new ArgumentOutOfRangeException()
			} ?? parameterType;

			bool isCombineWith = tAttrDto.CombineWith is not null;
			props.Store.OverloadMap[index] = new ParameterData(
				parameterAction,
				newParameterType,
				isCombineWith ?
					(sbyte) parameters.IndexOf(param => param.Identifier.ValueText == tAttrDto.CombineWith)
					: SByte.MaxValue);

			bool isFormatterWoIntegrity = parameterAction is ParameterAction.FormatterReplacement;
			props.Store.FormattersWoIntegrityCount += *(sbyte*) &isFormatterWoIntegrity;
			props.Store.CombineParametersCount += *(sbyte*) &isCombineWith;
			props.Store.IsSmthChanged |= shouldBeReplaced;
		}

		return ChainAction.NextMember;
	}
}
