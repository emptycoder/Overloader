using Microsoft.CodeAnalysis;
using Overloader.Enums;

namespace Overloader.Entities;

internal sealed record ParameterData(ParameterAction ParameterAction, ITypeSymbol Type, int CombineIndex);
