using System.Buffers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

internal sealed class AnalyzeMethodParams : IChainObj
{
	ChainResult IChainObj.Execute(GeneratorSourceBuilder gsb)
	{
		var entry = (MethodDeclarationSyntax) gsb.Entry;
		var parameters = entry.ParameterList.Parameters;
		gsb.Store.OverloadMap = ArrayPool<(ParameterAction ParameterAction, ITypeSymbol Type)>.Shared.Rent(parameters.Count);
		for (int index = 0; index < parameters.Count; index++)
		{
			bool shouldBeReplaced = parameters[index].TryGetTAttrByTemplate(gsb, out var attribute);
			var parameterType = (parameters[index].Type ?? throw new ArgumentException(
					$"Parameter {parameters[index].Identifier} type is null."))
				.GetType(gsb.Compilation);

			var parameterAction = shouldBeReplaced switch
			{
				true when gsb.TryGetFormatter(parameterType, out _) => ParameterAction.FormatterReplacement,
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
				_ => throw new ArgumentOutOfRangeException()
			} ?? parameterType;

			gsb.Store.OverloadMap[index] = (parameterAction, newParameterType);
			gsb.Store.IsAnyFormatter |= parameterAction is ParameterAction.FormatterReplacement;
			gsb.Store.IsSmthChanged |= shouldBeReplaced;
		}

		return ChainResult.NextChainMember;
	}
}
