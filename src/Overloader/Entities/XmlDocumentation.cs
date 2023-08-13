using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Overloader.Entities;

public class XmlDocumentation
{
	public readonly Dictionary<string, List<string>> ParamsMap = new();
	public readonly SyntaxTriviaList Trivia;

	private XmlDocumentation(in SyntaxTriviaList trivia) =>
		Trivia = trivia;
	public void AddOverload(string newParamName, string forParamName)
	{
		if (!ParamsMap.TryGetValue(forParamName, out var paramsList)) return;
		paramsList.Add(newParamName);
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
						
						xmlDocumentation.ParamsMap.Add(xmlNameAttributeSyntax.Identifier.ToString(), new List<string>());
					}
					break;
				case SyntaxKind.MultiLineDocumentationCommentTrivia:
					throw new NotSupportedException();
			}
		}

		return xmlDocumentation;
	}
}
