using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.MethodWorkerChain.Utils;
using Overloader.Entities;
using Overloader.Enums;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

internal sealed class GenerateTypeOverloads : IChainObj
{
	ChainResult IChainObj.Execute(GeneratorSourceBuilder gsb)
	{
		if (gsb.Store.OverloadMap is null || !gsb.Store.IsSmthChanged || gsb.Template is null) return ChainResult.NextChainMember;

		var entry = (MethodDeclarationSyntax) gsb.Entry;
		var parameters = entry.ParameterList.Parameters;

		// TODO: Insert attributes
		gsb.AppendWith(entry.Modifiers.ToFullString(), " ")
			.AppendWith(gsb.Store.ReturnType.ToFullString(), " ")
			.Append(entry.Identifier.ToFullString())
			.Append("(");

		if (parameters.Count > 0)
		{
			int index = 0;
			AppendParam();
			for (index = 1; index < parameters.Count; index++)
			{
				gsb.AppendWoTrim(", ");
				AppendParam();
			}

			void AppendParam()
			{
				var mappedParam = gsb.Store.OverloadMap[index];
				string paramName = parameters[index].Identifier.ToFullString();
				switch (mappedParam.ParameterAction)
				{
					case ParameterAction.Nothing:
						gsb.Append(parameters[index].ToFullString());
						break;
					case ParameterAction.SimpleReplacement:
					case ParameterAction.CustomReplacement:
						gsb.AppendWith(mappedParam.Type.ToDisplayString(), " ")
							.Append(paramName);
						break;
					case ParameterAction.FormatterReplacement:
						if (!gsb.TryGetFormatter(mappedParam.Type, out var formatter)) throw new ArgumentException();

						var originalType = (INamedTypeSymbol) mappedParam.Type.OriginalDefinition;
						var @params = new ITypeSymbol[formatter.GenericParams.Length];

						for (int paramIndex = 0; paramIndex < formatter.GenericParams.Length; paramIndex++)
							@params[paramIndex] = formatter.GenericParams[paramIndex].GetType(gsb.Template) ??
							                      throw new Exception("Can't get type");

						gsb.AppendWith(originalType.Construct(@params).ToDisplayString(), " ")
							.Append(paramName);
						break;
					default:
						throw new ArgumentException($"Can't find case for {gsb.Store.OverloadMap[index]} parameterAction.");
				}
			}
		}

		gsb.Append(")", 1)
			.WriteMethodBody(entry, ImmutableList<(string From, string To)>.Empty);

		return ChainResult.NextChainMember;
	}
}
