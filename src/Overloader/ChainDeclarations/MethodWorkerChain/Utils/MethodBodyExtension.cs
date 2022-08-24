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
		IList<(string From, string To)>? replaceModifiers)
	{
		if (method.ExpressionBody is not null)
		{
			gsb.Append(method.ExpressionBody.ArrowToken.ToFullString(), 1);

			string strStatement = gsb.ApplySyntaxChanges(method.ExpressionBody.Expression);

			// TODO: Create multiple string replacer
			if (replaceModifiers is not null)
				foreach ((string from, string to) in replaceModifiers)
					strStatement = strStatement.Replace(from, to);

			gsb.Append(strStatement)
				.Append(";", 1);
		}
		else if (method.Body is not null)
		{
			gsb.Append(string.Empty, 1)
				.NestedIncrease();
			foreach (var statement in method.Body.Statements)
			{
				string strStatement = gsb.ApplySyntaxChanges(statement);

				// TODO: Create multiple string replacer
				if (replaceModifiers is not null)
					foreach ((string from, string to) in replaceModifiers)
						strStatement = strStatement.Replace(from, to);

				gsb.Append(strStatement, 1);
			}

			gsb.NestedDecrease()
				.Append(string.Empty, 1);
		}

		return gsb;
	}

	private static string ApplySyntaxChanges<T>(this GeneratorSourceBuilder gsb, T node) where T : SyntaxNode
	{
		string strStatement = node.WithoutLeadingTrivia().ToString();
		if (!node.HasLeadingTrivia) return strStatement;

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
							strStatement = strStatement.Replace(kv.Key, kv.Value
								.Replace("${T}", templateStr));
							break;
						// Change line operation
						case '$':
							var newStatement = strTriviaSpan.Slice(3).Trim();
							if (newStatement.IndexOf("${T}".AsSpan()) != -1 && templateStr is null) break;
							strStatement = newStatement.ToString()
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
					gsb.Append(syntaxTrivia.ToString());
					break;
			}
		}


		return strStatement;
	}
}
