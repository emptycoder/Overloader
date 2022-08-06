using System.Buffers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.Abstractions;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

public sealed class AnalyzeMethodParams : IChainObj
{
	public ChainResult Execute(GeneratorSourceBuilder gsb)
	{
		var entry = (MethodDeclarationSyntax) gsb.Entry;
		var parameters = entry.ParameterList.Parameters;
		gsb.Store.OverloadMap = ArrayPool<(ParameterAction ParameterAction, ITypeSymbol Type)>.Shared.Rent(parameters.Count);
		for (int index = 0; index < parameters.Count; index++)
		{
			bool shouldBeReplaced = parameters[index].TryGetTAttr(gsb, out var attribute);
			var parameterType = (parameters[index].Type ?? throw new ArgumentException("Type is null.")).GetType(gsb.Compilation);
			var originalTypeDefinition = parameterType.OriginalDefinition;

			var parameterAction = shouldBeReplaced switch
			{
				true when gsb.Formatters.ContainsKey(originalTypeDefinition) => ParameterAction.FormatterReplacement,
				true when gsb.GlobalFormatters.ContainsKey(originalTypeDefinition) => ParameterAction.GlobalFormatterReplacement,
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
				ParameterAction.GlobalFormatterReplacement => default,
				_ => throw new ArgumentOutOfRangeException()
			} ?? parameterType;

			gsb.Store.OverloadMap[index] = (parameterAction, newParameterType);
			gsb.Store.IsAnyFormatter |= parameterAction is ParameterAction.FormatterReplacement
				or ParameterAction.GlobalFormatterReplacement;
			gsb.Store.IsSmthChanged |= shouldBeReplaced;
		}

		return ChainResult.NextChainMember;
	}
}
