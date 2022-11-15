using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Overloader.Entities.DTOs;

internal sealed record OverloadDto(
	string ClassName,
	AttributeArgumentSyntax TypeSyntax,
	string[] FormattersToUse
);
