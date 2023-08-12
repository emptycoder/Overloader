using Microsoft.CodeAnalysis;
using Overloader.Entities;
using Overloader.Enums;

namespace Overloader.ChainDeclarations.Overloads._7.CastForIntegrityTransition;

// ReSharper disable once InconsistentNaming
public sealed class CombinedCastForITOverloads : IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		return ChainAction.NextMember;
	}
}
