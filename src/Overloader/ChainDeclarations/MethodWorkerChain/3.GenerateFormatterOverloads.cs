using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.MethodWorkerChain.Utils;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

internal sealed class GenerateFormatterOverloads : IChainObj
{
	ChainResult IChainObj.Execute(GeneratorSourceBuilder gsb)
	{
		if (gsb.Store.OverloadMap is null || !gsb.Store.IsAnyFormatter) return ChainResult.NextChainMember;

		var entry = (MethodDeclarationSyntax) gsb.Entry;
		// TODO: Insert attributes
		gsb.AppendWith(entry.Modifiers.ToFullString(), " ")
			.AppendWith(entry.ReturnType.ToFullString(), " ")
			.Append(entry.Identifier.ToFullString())
			.Append("(");

		var replacementModifiers = new List<(string, string)>();
		var parameters = entry.ParameterList.Parameters;

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
						if (!gsb.TryGetFormatter(mappedParam.Type, out var formatter))
							throw new ArgumentException();

						if (formatter.Params.Length == 0) throw new ArgumentException();
						int paramIndex = 0;
						AppendFormatterParam();
						for (paramIndex = 1; paramIndex < formatter.Params.Length; paramIndex++)
						{
							gsb.AppendWoTrim(", ");
							AppendFormatterParam();
						}

						void AppendFormatterParam()
						{
							var formatterParam = formatter.Params[paramIndex];
							gsb.AppendWith((formatterParam.GetType(gsb.Template) ??
							                mappedParam.Type.GetMemberType(formatterParam.Name!)).ToDisplayString(), " ")
								.Append(paramName)
								.Append(formatterParam.Name);
							replacementModifiers.Add(($"{paramName}.{formatterParam.Name}", $"{paramName}{formatterParam.Name}"));
						}

						break;
					default:
						throw new ArgumentException($"Can't find case for {gsb.Store.OverloadMap[index]} parameterAction.");
				}
			}
		}

		gsb.Append(")", 1)
			.WriteMethodBody(entry, replacementModifiers);

		return ChainResult.NextChainMember;
	}
}
