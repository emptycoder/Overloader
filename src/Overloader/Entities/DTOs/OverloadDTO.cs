using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Overloader.Entities.DTOs;

public sealed record OverloadDto(
	string ClassName,
	AttributeArgumentSyntax TypeSyntax,
	string[] FormattersToUse
);
