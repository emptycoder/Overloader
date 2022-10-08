using System.Buffers;
using Microsoft.CodeAnalysis;
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
		
		
		var entry = (MethodDeclarationSyntax) syntaxNode;
		var parameters = entry.ParameterList.Parameters;
		props.Store.OverloadMap = ArrayPool<ParameterData>.Shared.Rent(parameters.Count);
		for (int index = 0; index < parameters.Count; index++)
		{
			bool shouldBeReplaced = parameters[index].TryGetTAttrByTemplate(props, out var attribute,
				out bool forceOverloadIntegrity,
				out string? combineWith);
			var parameterType = (parameters[index].Type ?? throw new ArgumentException(
						$"Parameter {parameters[index].Identifier} type is null.").WithLocation(parameters[index]))
				.GetType(props.Compilation);

			var parameterAction = shouldBeReplaced switch
			{
				true when props.TryGetFormatter(parameterType.GetClearType(), out var formatter) =>
					forceOverloadIntegrity || formatter.Params.Length == 0 || parameterType is not INamedTypeSymbol
						? ParameterAction.FormatterIntegrityReplacement
						: ParameterAction.FormatterReplacement,
				true when attribute?.ArgumentList is {Arguments.Count: >= 1} => ParameterAction.CustomReplacement,
				true => ParameterAction.SimpleReplacement,
				false => ParameterAction.Nothing
			};
			var newParameterType = parameterAction switch
			{
				ParameterAction.Nothing => default,
				ParameterAction.SimpleReplacement => props.Template,
				ParameterAction.CustomReplacement => attribute!.ArgumentList!.Arguments[0].GetType(props.Compilation),
				ParameterAction.FormatterReplacement => default,
				ParameterAction.FormatterIntegrityReplacement => default,
				_ => throw new ArgumentOutOfRangeException()
			} ?? parameterType;

			props.Store.OverloadMap[index] = new ParameterData(parameterAction, newParameterType, combineWith);
			bool isFormatter = parameterAction is ParameterAction.FormatterReplacement;
			props.Store.FormattersWoIntegrityCount += *(byte*) &isFormatter;
			props.Store.IsSmthChanged |= shouldBeReplaced;
		}

		return ChainAction.NextMember;
	}
}
