using System.Buffers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Entities.Builders;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.Utils;

internal static class MethodBodyExtension
{
	public static GeneratorProperties WriteMethodBody(this GeneratorProperties props,
		MethodDeclarationSyntax method,
		Span<(string VarName, string ConcatedVars)> replacements)
	{
		if (method.ExpressionBody is not null)
		{
			props.Builder.WriteChildren(method.ExpressionBody, replacements, props.Template?.ToDisplayString());
			props.Builder.Append(";", 1);
		}
		else if (method.Body is not null)
		{
			props.Builder.Append(String.Empty, 1)
				.WriteChildren(method.Body, replacements, props.Template?.ToDisplayString());
		}

		return props;
	}

	private static void WriteChildren(this SourceBuilder sb,
		SyntaxNodeOrToken syntaxNode,
		Span<(string VarName, string ConcatedVars)> replacements,
		string? templateStr)
	{
		string varName;
		foreach (var nodeOrToken in syntaxNode.ChildNodesAndTokens())
		{
			if (!nodeOrToken.IsNode)
			{
				switch (nodeOrToken.AsToken().Kind())
				{
					case SyntaxKind.OpenBraceToken:
						sb.NestedIncrease();
						continue;
					case SyntaxKind.CloseBraceToken:
						sb.NestedDecrease();
						continue;
					default:
						sb.AppendWoTrim(nodeOrToken.HasLeadingTrivia ? nodeOrToken.WithLeadingTrivia().ToFullString() : nodeOrToken.ToFullString());
						continue;
				}
			}

			var node = nodeOrToken.AsNode() ?? throw new ArgumentException("Unexpected exception. Node isn't node.")
				.WithLocation(nodeOrToken.GetLocation() ?? Location.None);

			switch (node)
			{
				case MemberAccessExpressionSyntax syntax:
					if (syntax.Expression is not IdentifierNameSyntax)
					{
						sb.Append(syntax.ToFullString());
						break;
					}

					// Don't need go deep to get name, because that's case can't be supported
					varName = syntax.Expression.ToString();
					if (varName.FindInReplacements(replacements) == -1) goto default;
					sb.AppendWoTrim(varName)
						.AppendWoTrim(syntax.Name.ToString());
					break;
				case ArgumentSyntax argSyntax:
					varName = argSyntax.Expression.ToString();
					int replacementIndex = varName.FindInReplacements(replacements);
					if (replacementIndex == -1) goto default;
					sb.AppendWoTrim(replacements[replacementIndex].ConcatedVars);
					break;
				case {Parent: ArrowExpressionClauseSyntax}:
				case StatementSyntax:
				{
					var triviaList = node.GetLeadingTrivia();
					var buffer = ArrayPool<(string, string)>.Shared.Rent(triviaList.Count);
					string? changeLine = triviaList.ParseTrivia(buffer, templateStr, out var localReplacements);
					if (changeLine is not null)
					{
						sb.AppendWoTrim(changeLine, 1);
					}
					else if (localReplacements.IsEmpty)
					{
						sb.WriteChildren(node, replacements, templateStr);
					}
					else
					{
						using var statementSb = sb.GetDependentInstance();
						statementSb.WriteChildren(node, replacements, templateStr);
						string statementStr = statementSb.ToString();
						foreach ((string key, string value) in localReplacements)
							statementStr = statementStr.Replace(key, value);

						sb.Append(statementStr, 1);
					}

					ArrayPool<(string, string)>.Shared.Return(buffer);
					break;
				}
				default:
					sb.WriteChildren(node, replacements, templateStr);
					break;
			}
		}
	}

	private static int FindInReplacements(this string value, Span<(string VarName, string ConcatedVars)> replacements)
	{
		for (int index = 0; index < replacements.Length; index++)
		{
			if (!replacements[index].VarName.Equals(value)) continue;
			return index;
		}

		return -1;
	}
	
	private static string? ParseTrivia(this SyntaxTriviaList triviaList, (string, string)[] buffer,
		string? templateStr, out Span<(string, string)> replacements)
	{
		int size = 0;
		string? changeLine = default;
		foreach (var syntaxTrivia in triviaList)
		{
			switch (syntaxTrivia.Kind())
			{
				case SyntaxKind.SingleLineCommentTrivia:
					string strTrivia = syntaxTrivia.ToString();
					var strTriviaSpan = strTrivia.AsSpan();
					int separatorIndex = strTrivia.LastIndexOf(':');

					if (separatorIndex != -1)
					{
						if (templateStr is null) continue;
						if (templateStr != strTriviaSpan.Slice(separatorIndex + 1).Trim().ToString()) continue;
						strTriviaSpan = strTriviaSpan.Slice(0, separatorIndex);
					}

					switch (strTrivia[2])
					{
						// Replace operator
						case '#':
							var kv = strTriviaSpan.SplitAsKV("->");
							if (kv.Value.IndexOf("${T}", StringComparison.Ordinal) != -1 && templateStr is null) break;
							kv.Value = kv.Value.Replace("${T}", templateStr);
							buffer[size++] = kv;
							break;
						// Change line operator
						case '$':
							var newStatement = strTriviaSpan.Slice(3).Trim();
							if (newStatement.IndexOf("${T}".AsSpan()) != -1 && templateStr is null) break;
							if (changeLine is not null) throw new ArgumentException("Can't be two replace line on one syntax node");
							changeLine = newStatement.ToString().Replace("${T}", templateStr);
							break;
					}

					break;
			}
		}

		replacements = buffer.AsSpan(0, size);
		return changeLine;
	}
}
