using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

internal sealed class AnalyzeMethodAttributes : IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		var entry = (MethodDeclarationSyntax) syntaxNode;
		props.Store.IsSmthChanged = false;
		props.Store.ReturnType = entry.ReturnType.GetType(props.Compilation);
		props.Store.Modifiers = new string[entry.Modifiers.Count];

		for (int index = 0; index < props.Store.Modifiers.Length; index++)
			props.Store.Modifiers[index] = entry.Modifiers[index].ToString();

		foreach (var attrList in entry.AttributeLists)
		foreach (var attribute in attrList.Attributes)
		{
			string attrName = attribute.Name.GetName();
			switch (attrName)
			{
				case Constants.IgnoreForAttr when attribute.ArgumentList is null or {Arguments.Count: < 1}:
					return ChainAction.Break;
				case Constants.IgnoreForAttr:
				{
					foreach (var arg in attribute.ArgumentList.Arguments)
						if (arg.EqualsToTemplate(props))
							return ChainAction.Break;
					break;
				}
				case Constants.AllowForAttr when attribute.ArgumentList is null or {Arguments.Count: < 1}:
					props.Store.SkipMember = false;
					continue;
				case Constants.AllowForAttr:
				{
					foreach (var arg in attribute.ArgumentList.Arguments)
						if (arg.EqualsToTemplate(props))
						{
							props.Store.SkipMember = false;
							goto End;
						}

					props.Store.SkipMember = true;

					End:
					break;
				}
				case Constants.TAttr:
				{
					var returnTypeSymbol = entry.ReturnType.GetType(props.Compilation);
					var returnTypeSymbolRoot = returnTypeSymbol.GetClearType();
					switch (attribute.ArgumentList?.Arguments.Count ?? 0)
					{
						case 1:
						case 2 when attribute.ArgumentList!.Arguments[1].EqualsToTemplate(props):
						{
							props.Store.ReturnType = attribute.ArgumentList!.Arguments[0].GetType(props.Compilation);
							props.Store.IsSmthChanged = true;
							break;
						}
						case 2:
							break;
						case 0 when props.TryGetFormatter(returnTypeSymbolRoot, out var formatter):
							var @params = new ITypeSymbol[formatter.GenericParams.Length];

							for (int paramIndex = 0; paramIndex < formatter.GenericParams.Length; paramIndex++)
								@params[paramIndex] = formatter.GenericParams[paramIndex].GetType(props.Template);

							props.Store.ReturnType = returnTypeSymbol.ConstructWithClearType(
								returnTypeSymbolRoot
									.OriginalDefinition
									.Construct(@params),
								props.Compilation);
							props.Store.IsSmthChanged = true;
							break;
						case 0:
							props.Store.ReturnType = props.Template;
							props.Store.IsSmthChanged = true;
							break;
						default:
							throw new ArgumentException(
								$"Unexpected count of arguments in {Constants.TAttr}.").WithLocation(attribute);
					}

					break;
				}
				case Constants.ChangeModifierAttr when (attribute.ArgumentList?.Arguments.Count ?? 0) <= 1:
					throw new ArgumentException($"Unexpected count of arguments in {Constants.ChangeModifierAttr}.")
						.WithLocation(attribute);
				case Constants.ChangeModifierAttr:
				{
					var arguments = attribute.ArgumentList!.Arguments;
					if (arguments.Count == 3 && !arguments[2].EqualsToTemplate(props)) continue;

					string modifier = arguments[0].Expression.GetInnerText();
					string newModifier = arguments[1].Expression.GetInnerText();

					for (int index = 0; index < props.Store.Modifiers.Length; index++)
					{
						if (!props.Store.Modifiers[index].Equals(modifier)) continue;

						props.Store.Modifiers[index] = newModifier;
						props.Store.IsSmthChanged = true;
						break;
					}

					break;
				}
			}
		}

		return props.Store.SkipMember ? ChainAction.Break : ChainAction.NextMember;
	}
}
