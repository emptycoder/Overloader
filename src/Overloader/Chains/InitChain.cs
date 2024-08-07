﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Chains;

internal class InitChain : IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props)
	{
		var expectedLength = props.StartEntry.TSpecifyDto.DefaultTypeSyntaxes.Length;
		foreach (var overloadDto in props.StartEntry.OverloadTypes)
		{
			if (overloadDto.TypeSyntaxes.Length != expectedLength)
				throw new ArgumentException($"{TOverload.TagName} has a different count of template arguments than the {TSpecify.TagName}")
					.WithLocation(props.StartEntry.Syntax);
		}
		
		var sb = props.Builder;
		switch (props.IsDefaultOverload)
		{
			case true when props.StartEntry.IsTransitionsIgnored:
			case true when !props.StartEntry.Syntax.Modifiers.Any(SyntaxKind.PartialKeyword):
				return ChainAction.Break;
		}

		var entrySyntax = props.StartEntry.Syntax;
		if (entrySyntax.AttributeLists.Any(attrList => attrList.Attributes.Any(attr =>
			    attr.Name.GetName() == RemoveBody.TagName)))
			props.Store.ShouldRemoveBody = true;

		sb.AppendUsings(entrySyntax.GetTopParent())
			.AppendNamespace(entrySyntax.GetNamespace())
			.TrimAppend(string.Empty, 2);

		// Declare class/struct/record signature
		sb.AppendAttributes(entrySyntax.AttributeLists, "\n")
			.TrimAppend(entrySyntax.Modifiers.ToString())
			.WhiteSpace()
			.TrimAppend(entrySyntax.Keyword.ToFullString())
			.WhiteSpace()
			.TrimAppend(props.ClassName)
			.TrimAppend(entrySyntax.BaseList?.ToFullString() ?? string.Empty)
			.TrimAppend(entrySyntax.TypeParameterList?.ToString() ?? string.Empty)
			.WhiteSpace()
			.TrimAppend(entrySyntax.ConstraintClauses.ToFullString(), 1)
			.NestedIncrease(SyntaxKind.OpenBraceToken);

		foreach (var member in props.StartEntry.Syntax.Members)
		{
			if (member is not MethodDeclarationSyntax methodSyntax) continue;

			props.Store.MethodSyntax = methodSyntax;
			props.Store.SkipMember = props.StartEntry.IsInvertedMode;
			foreach (var worker in ChainDeclarations.MethodWorkers)
			{
				try
				{
					if (worker.Execute(props) == ChainAction.Break)
						break;
				}
				catch (LocationException)
				{
					throw;
				}
				catch (Exception ex)
				{
					throw ex.NotExpected($"Something went wrong during executing of {worker.GetType().Name}.");
				}
			}
		}

		sb.NestedDecrease(SyntaxKind.CloseBraceToken);

		return ChainAction.NextMember;
	}
}
