using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.Utils;

internal static class MethodBodyExtension
{
	public static GeneratorSourceBuilder WriteMethodBody(this GeneratorSourceBuilder gsb,
		MethodDeclarationSyntax method,
		(string Replacment, string ConcatedParams)[] replacement)
	{
		if (method.ExpressionBody is not null)
		{
			gsb.Append(method.ExpressionBody.ArrowToken.ToFullString(), 1);

			gsb.ApplySyntaxChanges(method.ExpressionBody.Expression, replacement)
				.Append(";", 1);
		}
		else if (method.Body is not null)
		{
			gsb.Append(string.Empty, 1)
				.NestedIncrease();
			foreach (var statement in method.Body.Statements)
			{
				gsb.ApplySyntaxChanges(statement, replacement)
					.Append(string.Empty, 1);
			}

			gsb.NestedDecrease()
				.Append(string.Empty, 1);
		}

		return gsb;
	}

	private static StringBuilder ToStringVarsReplacement<T>(this StringBuilder sb,
		T syntaxNode,
		(string VarName, string ConcatedVars)[] replacements) where T : SyntaxNode
	{
		foreach (var nodeOrToken in syntaxNode.ChildNodesAndTokens())
		{
			if (!nodeOrToken.IsNode)
			{
				sb.Append(nodeOrToken.ToFullString());
				continue;
			}

			var node = nodeOrToken.AsNode() ?? throw new ArgumentException("Unexpected exception. Node isn't node.");

			string varName;
			switch (node)
			{
				case MemberAccessExpressionSyntax syntax:
					if (syntax.Expression is not IdentifierNameSyntax)
					{
						sb.Append(syntax.ToFullString());
						continue;
					}

					varName = syntax.Expression.ToString();
					if (FindReplacement() == -1) goto default;
					sb.Append(varName).Append(syntax.Name);
					break;
				case ArgumentSyntax argSyntax:
					varName = argSyntax.ToString();
					int replacementIndex = FindReplacement();
					if (replacementIndex == -1) goto default;
					sb.Append(replacements[replacementIndex].ConcatedVars);
					break;
				default:
					sb.ToStringVarsReplacement(node, replacements);
					break;
			}

			int FindReplacement()
			{
				for (int index = 0; index < replacements.Length; index++)
				{
					if (!replacements[index].VarName.Equals(varName)) continue;
					return index;
				}

				return -1;
			}
		}

		return sb;
	}

	private static GeneratorSourceBuilder ApplySyntaxChanges<T>(this GeneratorSourceBuilder gsb,
		T node,
		(string Replacment, string ConcatedParams)[] replacement) where T : SyntaxNode
	{
		using var sb = StringBuilderPool.GetInstance();
		string strStatementWoTrivia = sb.Builder
			.ToStringVarsReplacement(node, replacement)
			.ToString();
		if (!node.HasLeadingTrivia) return gsb;

		foreach (var syntaxTrivia in node.GetLeadingTrivia())
		{
			switch (syntaxTrivia.Kind())
			{
				case SyntaxKind.SingleLineCommentTrivia:
					string strTrivia = syntaxTrivia.ToString();
					var strTriviaSpan = strTrivia.AsSpan();
					int separatorIndex = strTrivia.LastIndexOf(':');
					string? templateStr = gsb.Template?.ToDisplayString();

					if (separatorIndex != -1)
					{
						if (gsb.Template is null) continue;
						if (templateStr != strTriviaSpan.Slice(separatorIndex + 1).Trim().ToString()) continue;
						strTriviaSpan = strTriviaSpan.Slice(0, separatorIndex);
					}

					switch (strTrivia[2])
					{
						// Replace operation
						case '#':
							var kv = strTriviaSpan.SplitAsKV("->");
							if (kv.Value.IndexOf("${T}", StringComparison.Ordinal) != -1 && templateStr is null) break;
							strStatementWoTrivia = strStatementWoTrivia.Replace(kv.Key, kv.Value
								.Replace("${T}", templateStr));
							break;
						// Change line operation
						case '$':
							var newStatement = strTriviaSpan.Slice(3).Trim();
							if (newStatement.IndexOf("${T}".AsSpan()) != -1 && templateStr is null) break;
							strStatementWoTrivia = newStatement.ToString()
								.Replace("${T}", templateStr);
							break;
						default:
							gsb.Append(strTrivia);
							break;
					}

					break;
				case SyntaxKind.WhitespaceTrivia:
					break;
				default:
					gsb.Append(syntaxTrivia.ToString(), 1);
					break;
			}
		}

		return gsb.Append(strStatementWoTrivia);
	}
}
