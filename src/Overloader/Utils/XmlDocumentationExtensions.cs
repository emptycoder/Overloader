using Overloader.ContentBuilders;
using Overloader.Entities;

namespace Overloader.Utils;

public static class XmlDocumentationExtensions
{
	public static SourceBuilder AppendXmlDocumentation(
		this SourceBuilder sourceBuilder,
		XmlDocumentation documentation)
	{
		documentation.AppendToSb(sourceBuilder);
		return sourceBuilder;
	}
	
	public static SourceBuilder AppendAndClearXmlDocumentation(
		this SourceBuilder sourceBuilder,
		XmlDocumentation documentation)
	{
		documentation.AppendToSb(sourceBuilder);
		documentation.Clear();
		return sourceBuilder;
	}
}
