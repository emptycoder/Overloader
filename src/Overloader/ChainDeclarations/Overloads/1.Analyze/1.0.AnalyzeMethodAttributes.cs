using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Models;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.Overloads;

public sealed class AnalyzeMethodAttributes : IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		var entry = (MethodDeclarationSyntax) syntaxNode;
		props.Store.IsSmthChanged = false;
		props.Store.MethodData.ReturnType = entry.ReturnType.GetType(props.Compilation);
		props.Store.MethodData.MethodModifiers = new string[entry.Modifiers.Count];
		props.Store.MethodData.MethodName = entry.Identifier.ToString();

		for (int index = 0; index < props.Store.MethodData.MethodModifiers.Length; index++)
			props.Store.MethodData.MethodModifiers[index] = entry.Modifiers[index].ToString();

		bool isAllowForAttrSet = false;
		foreach (var attrList in entry.AttributeLists)
		foreach (var attribute in attrList.Attributes)
		{
			string attrName = attribute.Name.GetName();
			switch (attrName)
			{
				case nameof(IgnoreFor) when attribute.ArgumentList is null or {Arguments.Count: < 1}:
					return ChainAction.Break;
				case nameof(IgnoreFor):
				{
					foreach (var arg in attribute.ArgumentList.Arguments)
						if (arg.EqualsToTemplate(props))
							return ChainAction.Break;
					break;
				}
				case nameof(AllowFor) when attribute.ArgumentList is null or {Arguments.Count: < 1}:
					props.Store.SkipMember = false;
					continue;
				case nameof(AllowFor):
				{
					foreach (var arg in attribute.ArgumentList.Arguments)
						if (arg.EqualsToTemplate(props))
						{
							isAllowForAttrSet = true;
							props.Store.SkipMember = false;
							break;
						}

					if (!isAllowForAttrSet)
						props.Store.SkipMember = true;
					break;
				}
				case TAttribute.TagName:
				{
					var returnTypeSymbol = entry.ReturnType.GetType(props.Compilation);
					var returnTypeSymbolRoot = returnTypeSymbol.GetClearType();
					switch (attribute.ArgumentList?.Arguments.Count ?? 0)
					{
						case 1:
						case 2 when attribute.ArgumentList!.Arguments[1].EqualsToTemplate(props):
							props.Store.MethodData.ReturnType = attribute.ArgumentList!.Arguments[0].GetType(props.Compilation);
							props.Store.IsSmthChanged = true;
							break;
						case 2:
							break;
						case 0 when props.TryGetFormatter(returnTypeSymbolRoot, out var formatter):
							var @params = new ITypeSymbol[formatter.GenericParams.Length];

							for (int paramIndex = 0; paramIndex < formatter.GenericParams.Length; paramIndex++)
								@params[paramIndex] = formatter.GenericParams[paramIndex].GetType(props.Template);

							props.Store.MethodData.ReturnType = returnTypeSymbol.ConstructWithClearType(
								returnTypeSymbolRoot
									.OriginalDefinition
									.Construct(@params),
								props.Compilation);
							props.Store.IsSmthChanged = true;
							break;
						case 0:
							props.Store.MethodData.ReturnType = props.Template;
							props.Store.IsSmthChanged = true;
							break;
						default:
							throw new ArgumentException($"Unexpected count of arguments in {TAttribute.TagName}.")
								.WithLocation(attribute);
					}
					break;
				}
				case nameof(ChangeModifier) when (attribute.ArgumentList?.Arguments.Count ?? 0) <= 1:
					throw new ArgumentException($"Unexpected count of arguments in {nameof(ChangeModifier)}.")
						.WithLocation(attribute);
				case nameof(ChangeModifier):
				{
					var arguments = attribute.ArgumentList!.Arguments;
					if (arguments.Count == 3 && !arguments[2].EqualsToTemplate(props)) continue;

					string modifier = arguments[0].Expression.GetInnerText();
					string newModifier = arguments[1].Expression.GetInnerText();

					for (int index = 0; index < props.Store.MethodData.MethodModifiers.Length; index++)
					{
						if (!props.Store.MethodData.MethodModifiers[index].Equals(modifier)) continue;

						props.Store.MethodData.MethodModifiers[index] = newModifier;
						props.Store.IsSmthChanged = true;
						break;
					}

					break;
				}
				case nameof(ChangeName) when !props.IsTSpecified:
					switch (attribute.ArgumentList?.Arguments.Count ?? 0)
					{
						case 1:
							props.Store.MethodData.MethodName = attribute.ArgumentList!.Arguments[0].Expression.GetVariableName();
							props.Store.IsSmthChanged = true;
							break;
						case 2 when attribute.ArgumentList!.Arguments[1].EqualsToTemplate(props):
							props.Store.MethodData.MethodName = attribute.ArgumentList!.Arguments[0].Expression.GetVariableName();
							props.Store.IsSmthChanged = true;
							break;
						case 2:
							break;
						default:
							throw new ArgumentException($"Unexpected count of arguments in {nameof(ChangeName)}.")
								.WithLocation(attribute);
					}

					break;
				case nameof(ForceChanged):
					props.Store.IsSmthChanged = true;
					break;
			}
		}

		return props.Store.SkipMember ? ChainAction.Break : ChainAction.NextMember;
	}
}
