using System.Buffers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

internal sealed class AnalyzeMethodParams : IChainObj
{
	unsafe ChainResult IChainObj.Execute(GeneratorSourceBuilder gsb)
	{
		var entry = (MethodDeclarationSyntax) gsb.Entry;
		var parameters = entry.ParameterList.Parameters;
		gsb.Store.OverloadMap = ArrayPool<(ParameterAction ParameterAction, ITypeSymbol Type)>.Shared.Rent(parameters.Count);
		for (int index = 0; index < parameters.Count; index++)
		{
			bool shouldBeReplaced = parameters[index].TryGetTAttrByTemplate(gsb, out var attribute, out bool forceOverloadIntegrity);
			var parameterType = (parameters[index].Type ?? throw new ArgumentException(
						$"Parameter {parameters[index].Identifier} type is null.")
					.WithLocation(parameters[index].GetLocation()))
				.GetType(gsb.Compilation);

			var parameterAction = shouldBeReplaced switch
			{
				true when gsb.TryGetFormatter(parameterType.GetRootType(), out var formatter) =>
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
				ParameterAction.SimpleReplacement => gsb.Template,
				ParameterAction.CustomReplacement => attribute!.ArgumentList!.Arguments[0].GetType(gsb.Compilation),
				ParameterAction.FormatterReplacement => default,
				ParameterAction.FormatterIntegrityReplacement => default,
				_ => throw new ArgumentOutOfRangeException()
			} ?? parameterType;

			gsb.Store.OverloadMap[index] = (parameterAction, newParameterType);
			bool isFormatter = parameterAction is ParameterAction.FormatterReplacement;
			gsb.Store.FormattersWoIntegrityCount += *(byte*) &isFormatter;
			gsb.Store.IsSmthChanged |= shouldBeReplaced;
		}

		return ChainResult.NextChainMember;
	}
}
