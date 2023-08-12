using Microsoft.CodeAnalysis;
using Overloader.Entities;
using Overloader.Enums;

namespace Overloader.ChainDeclarations.Overloads;

// ReSharper disable once InconsistentNaming
public sealed class CastForDTOverloads : IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		return ChainAction.NextMember;
	}
}
