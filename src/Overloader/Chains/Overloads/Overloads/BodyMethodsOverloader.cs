using System.Buffers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Chains.Overloads.Overloads;

public abstract class BodyMethodsOverloader : MethodOverloader
{
	protected static void WriteMethodBody(
		GeneratorProperties props,
		SourceBuilder body,
		Span<(string, string)> replacements)
	{
		if (props.Store.MethodSyntax.ExpressionBody is not null)
		{
			body.WhiteSpace()
				.NestedIncrease();
			WriteExpressionBody(body, props.Store.MethodSyntax.ExpressionBody, props.TemplatesStr, replacements);
			body.NestedDecrease()
				.AppendAsConstant(";", 1);
		}
		else if (props.Store.MethodSyntax.Body is not null)
		{
			body.BreakLine();
			WriteChildren(body, props.Store.MethodSyntax.Body, props.TemplatesStr, replacements);
		}
		else
		{
			body.AppendAsConstant(";", 1);
		}
	}

	private static void WriteExpressionBody(
		SourceBuilder sb,
		ArrowExpressionClauseSyntax syntaxNode,
		string[] templatesStr,
		Span<(string, string)> replacements)
	{
		sb.TrimAppend(syntaxNode.ArrowToken.ToString())
			.WhiteSpace();
		var node = syntaxNode.Expression;
		var triviaList = node.GetLeadingTrivia();
		var buffer = ArrayPool<(string, string)>.Shared.Rent(triviaList.Count);
		string? changeLine = ParseTrivia(triviaList, buffer, templatesStr, out var localReplacements);
		if (changeLine is not null)
		{
			sb.Append(changeLine, 1);
		}
		else if (localReplacements.IsEmpty)
		{
			WriteChildren(sb, node, templatesStr, replacements);
		}
		else
		{
			using var statementSb = sb.GetDependentInstance();
			WriteChildren(statementSb, node, templatesStr, replacements);
			string statementStr = statementSb.ToString();
			foreach ((string key, string value) in localReplacements)
				statementStr = statementStr.Replace(key, value);

			sb.TrimAppend(statementStr, 1);
		}

		ArrayPool<(string, string)>.Shared.Return(buffer);
	}

	private static void WriteChildren(
		SourceBuilder sb,
		SyntaxNodeOrToken syntaxNode,
		string[] templatesStr,
		Span<(string VarName, string ConcatedVars)> replacements)
	{
		foreach (var nodeOrToken in syntaxNode.ChildNodesAndTokens())
		{
			if (!nodeOrToken.IsNode)
			{
				switch (nodeOrToken.AsToken().Kind())
				{
					case SyntaxKind.OpenBraceToken:
						sb.NestedIncrease(SyntaxKind.OpenBraceToken)
							.BreakLine();
						continue;
					case SyntaxKind.CloseBraceToken:
						sb.NestedDecrease(SyntaxKind.CloseBraceToken);
						continue;
					default:
						sb.Append(nodeOrToken.HasLeadingTrivia ? nodeOrToken.WithLeadingTrivia().ToFullString() : nodeOrToken.ToFullString());
						continue;
				}
			}

			var node = nodeOrToken.AsNode() ?? throw new ArgumentException("Unexpected exception. Node isn't node.")
				.WithLocation(nodeOrToken.GetLocation() ?? Location.None);

			string varName;
			switch (node)
			{
				case ElementAccessExpressionSyntax syntax:
					if (syntax.Expression is not IdentifierNameSyntax)
					{
						sb.TrimAppend(syntax.ToFullString());
						break;
					}

					// Don't need go deep to get name, because that's case can't be supported
					varName = syntax.Expression.ToString();
					if (varName.FindInReplacements(replacements) == -1) goto default;
					sb.Append(varName)
						.Append(string.Join(string.Empty, syntax.ArgumentList.Arguments));
					break;
				case MemberAccessExpressionSyntax syntax:
					if (syntax.Expression is not IdentifierNameSyntax)
					{
						sb.TrimAppend(syntax.ToFullString());
						break;
					}

					// Don't need go deep to get name, because that's case can't be supported
					varName = syntax.Expression.ToString();
					if (varName.FindInReplacements(replacements) == -1) goto default;
					sb.Append(varName)
						.Append(syntax.Name.ToString());
					break;
				case ArgumentSyntax argSyntax:
					varName = argSyntax.Expression.ToString();
					int replacementIndex = varName.FindInReplacements(replacements);
					if (replacementIndex == -1) goto default;
					sb.Append(replacements[replacementIndex].ConcatedVars);
					break;
				case StatementSyntax:
				{
					var triviaList = node.GetLeadingTrivia();
					var buffer = ArrayPool<(string, string)>.Shared.Rent(triviaList.Count);
					string? changeLine = ParseTrivia(triviaList, buffer, templatesStr, out var localReplacements);
					if (changeLine is not null)
					{
						sb.Append(changeLine, 1);
					}
					else if (localReplacements.IsEmpty)
					{
						WriteChildren(sb, node, templatesStr, replacements);
					}
					else
					{
						using var statementSb = sb.GetDependentInstance();
						WriteChildren(statementSb, node, templatesStr, replacements);
						string statementStr = statementSb.ToString();
						foreach ((string key, string value) in localReplacements)
							statementStr = statementStr.Replace(key, value);

						sb.TrimAppend(statementStr, 1);
					}

					ArrayPool<(string, string)>.Shared.Return(buffer);
					break;
				}
				default:
					WriteChildren(sb, node, templatesStr, replacements);
					break;
			}
		}
	}

	private static string? ParseTrivia(
		SyntaxTriviaList triviaList,
		(string, string)[] buffer,
		string[] templatesStr,
		out Span<(string, string)> replacements)
	{
		int size = 0;
		string? changeLine = default;
		foreach (var syntaxTrivia in triviaList)
		{
			// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
			switch (syntaxTrivia.Kind())
			{
				case SyntaxKind.SingleLineCommentTrivia:
					string strTrivia = syntaxTrivia.ToString();
					byte templateIndex;
					string templateStr;
					ReadOnlySpan<char> strTriviaSpan;

					switch (strTrivia[2])
					{
						// Replace operator
						case '#':
							templateIndex = ParseByteFromStr(strTrivia, 3, out _);
							if (templatesStr is null || templateIndex > templatesStr.Length) break;
							templateStr = templatesStr[templateIndex];
							if (!CheckTemplate(strTrivia, templateStr, out strTriviaSpan)) continue;
							
							var kv = strTriviaSpan.SplitAsKV("->");
							if (kv.Key.IndexOf("${T}", StringComparison.Ordinal) != -1)
								kv.Key = kv.Key.Replace("${T}", templateStr);
							if (kv.Value.IndexOf("${T}", StringComparison.Ordinal) != -1)
								kv.Value = kv.Value.Replace("${T}", templateStr);
							buffer[size++] = kv;
							break;
						// Change line operator
						case '$':
							templateIndex = ParseByteFromStr(strTrivia, 3, out int endPrefixIndex);
							if (templatesStr is null || templateIndex > templatesStr.Length) break;
							templateStr = templatesStr[templateIndex];
							if (!CheckTemplate(strTrivia, templateStr, out strTriviaSpan)) continue;
							
							var newStatement = strTriviaSpan.Slice(endPrefixIndex).Trim();
							if (changeLine is not null)
								throw new ArgumentException("Cannot use two 'replace line' ('//$') on one syntax node.");
							changeLine = newStatement.ToString().Replace("${T}", templateStr);
							break;
					}

					break;
			}
		}

		replacements = buffer.AsSpan(0, size);
		return changeLine;

		byte ParseByteFromStr(string strTrivia, int startIndex, out int index)
		{
			index = startIndex;
			while (char.IsDigit(strTrivia[index]))
			{
				index++;
			}

			return index == startIndex ? (byte) 0 : byte.Parse(strTrivia.Substring(startIndex, index - startIndex));
		}

		bool CheckTemplate(string strTrivia, string templateStr, out ReadOnlySpan<char> strTriviaSpan)
		{
			strTriviaSpan = strTrivia.AsSpan();
			int separatorIndex = strTrivia.LastIndexOf(':');

			if (separatorIndex != -1)
			{
				if (!strTriviaSpan.Slice(separatorIndex + 1).TryToFindMatch(templateStr.AsSpan(), ",".AsSpan())) return false;
				strTriviaSpan = strTriviaSpan.Slice(0, separatorIndex);
			}

			return true;
		}
	}
}
