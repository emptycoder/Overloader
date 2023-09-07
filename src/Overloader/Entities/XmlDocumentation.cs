using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;

namespace Overloader.Entities;

public class XmlDocumentation
{
	public static readonly XmlDocumentation Empty = new(SyntaxTriviaList.Empty);
	private readonly Dictionary<string, List<string>> _paramsMap = new(StringComparer.Ordinal);
	private readonly SyntaxTriviaList _trivia;

	private XmlDocumentation(in SyntaxTriviaList trivia) =>
		_trivia = trivia;

	public void AddOverload(string oldParamName, string newParamName)
	{
		if (!_paramsMap.TryGetValue(oldParamName, out var paramsList)) return;
		paramsList.Add(newParamName);
	}

	public void Clear()
	{
		foreach (var kv in _paramsMap)
			kv.Value.Clear();
	}

	public void AppendToSb(SourceBuilder sourceBuilder)
	{
		bool hasSmthData = false;
		XmlNodeSyntax? lastXmlText = null;
		ReadOnlySpan<char> trimChars = stackalloc char[1] {' '};
		foreach (var data in _trivia)
		{
			switch (data.Kind())
			{
				case SyntaxKind.SingleLineDocumentationCommentTrivia:
					var content = ((DocumentationCommentTriviaSyntax) data.GetStructure()!).Content;
					foreach (var xmlNode in content)
					{
						if (xmlNode is not XmlElementSyntax elementSyntax
						    || elementSyntax.StartTag.Name.LocalName.Text is not "param"
						    || elementSyntax.StartTag.Attributes.FirstOrDefault(attr => attr.Name.LocalName.Text is "name")
							    is not XmlNameAttributeSyntax xmlNameAttributeSyntax)
						{
							if (xmlNode is XmlTextSyntax)
								lastXmlText = xmlNode;
							else
							{
								if (lastXmlText is not null)
								{
									sourceBuilder
										.TrimEndAppend(lastXmlText.ToFullString(), trimChars)
										.WhiteSpace();
									lastXmlText = null;
								}

								sourceBuilder.Append(xmlNode.ToFullString());
							}

							continue;
						}

						string varName = xmlNameAttributeSyntax.Identifier.ToString();
						foreach (string identifier in _paramsMap[varName])
						{
							sourceBuilder
								.Append(lastXmlText!.ToFullString())
								.WhiteSpace()
								.Append(elementSyntax.ToFullString().Replace($"name=\"{varName}\"", $"name=\"{identifier}\""));
						}

						lastXmlText = null;
					}

					if (lastXmlText is not null)
						sourceBuilder.TrimAppend(lastXmlText.ToFullString());

					hasSmthData = true;
					break;
				case SyntaxKind.MultiLineDocumentationCommentTrivia:
					throw new NotSupportedException();
				case SyntaxKind.EndOfLineTrivia:
				case SyntaxKind.WhitespaceTrivia:
					break;
				default:
					sourceBuilder.TrimAppend(data.ToFullString());
					hasSmthData = true;
					break;
			}
		}

		if (hasSmthData)
			sourceBuilder.BreakLine();
	}

	public static XmlDocumentation Parse(in SyntaxTriviaList trivia)
	{
		var xmlDocumentation = new XmlDocumentation(trivia);
		foreach (var data in trivia)
		{
			switch (data.Kind())
			{
				case SyntaxKind.SingleLineDocumentationCommentTrivia:
					var content = ((DocumentationCommentTriviaSyntax) data.GetStructure()!).Content;
					foreach (var xmlNode in content)
					{
						if (xmlNode is not XmlElementSyntax elementSyntax
						    || elementSyntax.StartTag.Name.LocalName.Text is not "param"
						    || elementSyntax.StartTag.Attributes.FirstOrDefault(attr => attr.Name.LocalName.Text is "name")
							    is not XmlNameAttributeSyntax xmlNameAttributeSyntax) continue;

						xmlDocumentation._paramsMap.Add(xmlNameAttributeSyntax.Identifier.ToString(), new List<string>());
					}

					break;
				case SyntaxKind.MultiLineDocumentationCommentTrivia:
					throw new NotSupportedException();
			}
		}

		return xmlDocumentation;
	}
}
