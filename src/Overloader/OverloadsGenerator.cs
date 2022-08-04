using System.Buffers;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Enums;

namespace Overloader;

// 1. Analyze type attrs by SyntaxReceiver
// 2. Generate custom overloads for basic type
// 3. Generate overloads for all types
[Generator]
internal sealed class OverloadsGenerator : ISourceGenerator
{
	private readonly ArrayPool<(ParameterAction ParameterAction, ITypeSymbol Type)> _arrayPool =
		ArrayPool<(ParameterAction ParameterAction, ITypeSymbol Type)>.Shared;

	private readonly SourceBuilder _sb = new();

	public void Initialize(GeneratorInitializationContext context)
	{
#if DEBUG
		if (!Debugger.IsAttached) Debugger.Launch();
#endif
		context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
	}

	public void Execute(GeneratorExecutionContext context)
	{
		if (context.SyntaxReceiver is not SyntaxReceiver syntaxReceiver
		    || !syntaxReceiver.Candidates.Any()) return;

		var globalFormatters = syntaxReceiver.GlobalFormatterSyntaxes.GetFormatters(context.Compilation);
		foreach (var candidate in syntaxReceiver.Candidates)
		{
			string candidateClassName = candidate.Syntax.Identifier.ValueText;
			var formatters = candidate.FormatterSyntaxes.GetFormatters(context.Compilation);
			context.AddSource($"{candidateClassName}.g.cs", GenerateOverloads(context.Compilation,
				globalFormatters,
				formatters,
				candidate,
				candidateClassName,
				null));
			foreach ((string className, var argSyntax) in candidate.OverloadTypes)
				context.AddSource($"{className}.g.cs",
					GenerateOverloads(context.Compilation,
						globalFormatters,
						formatters,
						candidate,
						className,
						argSyntax.GetType(context.Compilation)));
		}
	}

	private string GenerateOverloads(Compilation compilation,
		Dictionary<ITypeSymbol, Formatter> globalFormatters,
		Dictionary<ITypeSymbol, Formatter> formatters,
		TypeEntrySyntax entry,
		string className,
		ITypeSymbol? template)
	{
		var newTypeSyntax = template is not null ? SyntaxFactory.ParseTypeName(template.Name) : default;
		_sb.AppendUsings(entry.Syntax.GetTopParent().DescendantNodes().Where(node => node is UsingDirectiveSyntax))
			.Append($"namespace {entry.Syntax.GetNamespace()};", 2);

		// Declare class/struct/record signature
		_sb.Append(entry.Syntax.Modifiers.ToFullString(), 1, ' ')
			.Append(entry.Syntax.Keyword.ToFullString(), 1, ' ')
			.AppendLineAndNestedIncrease(className);

		NextMember:
		foreach (var member in entry.Syntax.Members)
		{
			if (member is not MethodDeclarationSyntax method)
			{
				_sb.Append(member.ToFullString());
				continue;
			}

			// Analyze method attributes
			var returnType = method.ReturnType;
			foreach (var attrList in method.AttributeLists)
			foreach (var attribute in attrList.Attributes)
			{
				string attrName = attribute.Name.GetName();
				if (attrName == AttributeNames.IgnoreForAttr)
				{
					if (attribute.ArgumentList is null or {Arguments.Count: < 1}) goto NextMember;
					if (template is null) continue;
					foreach (var arg in attribute.ArgumentList.Arguments)
						if (SymbolEqualityComparer.Default.Equals(arg.GetType(compilation), template))
							goto NextMember;
				}
				else if (attrName == AttributeNames.TAttr)
				{
					switch (attribute.ArgumentList?.Arguments.Count ?? 0)
					{
						case 0 when newTypeSyntax is not null:
							returnType = newTypeSyntax;
							break;
						case 0:
							break;
						case 1:
						case 2 when template is not null && SymbolEqualityComparer.Default.Equals(
							attribute.ArgumentList!.Arguments[1].GetType(compilation), template):
						{
							returnType = SyntaxFactory.ParseTypeName(attribute.ArgumentList!.Arguments[0].GetType(compilation).Name);
							break;
						}
						default:
							throw new ArgumentException($"Unexpected count of arguments in {nameof(TAttribute)}.");
					}
				}
				// TODO: Analyze modifiers and create realization
				else if (attrName == AttributeNames.ChangeAccessModifierAttr) { }
			}

			// Analyze method params
			var parameters = method.ParameterList.Parameters;
			var overloadMap = _arrayPool.Rent(parameters.Count);
			bool isSmthShouldBeReplaced = false;
			bool isAnyFormatter = false;
			for (int index = 0; index < parameters.Count; index++)
			{
				bool shouldBeReplaced = parameters[index].TryGetTAttr(compilation, template, out var attribute);
				var parameterType = (parameters[index].Type ?? throw new ArgumentException("Type is null.")).GetType(compilation);
				var originalTypeDefinition = parameterType.OriginalDefinition;

				var parameterAction = shouldBeReplaced switch
				{
					true when formatters.ContainsKey(originalTypeDefinition) => ParameterAction.FormatterReplacement,
					true when globalFormatters.ContainsKey(originalTypeDefinition) => ParameterAction.GlobalFormatterReplacement,
					true when attribute?.ArgumentList is {Arguments.Count: >= 1} => ParameterAction.CustomReplacement,
					true => ParameterAction.SimpleReplacement,
					false => ParameterAction.Nothing
				};
				var newParameterType = parameterAction switch
				{
					ParameterAction.Nothing => default,
					ParameterAction.SimpleReplacement => template,
					ParameterAction.CustomReplacement => attribute!.ArgumentList!.Arguments[0].GetType(compilation),
					ParameterAction.FormatterReplacement => default,
					ParameterAction.GlobalFormatterReplacement => default,
					_ => throw new ArgumentOutOfRangeException()
				} ?? parameterType;

				overloadMap[index] = (parameterAction, newParameterType);
				isAnyFormatter |= parameterAction is ParameterAction.FormatterReplacement or ParameterAction.GlobalFormatterReplacement;
				isSmthShouldBeReplaced |= shouldBeReplaced;
			}

			if (isAnyFormatter)
			{
				// Formatters overload with type if available
				_sb.Append($"{method.Modifiers.ToFullString()}{method.ReturnType.ToFullString()}{method.Identifier.ToFullString()}(");
				var replacementModifiers = new List<(string, string)>();
				for (int index = 0; index < parameters.Count; index++)
				{
					var mappedParam = overloadMap[index];
					string paramName = parameters[index].Identifier.ToFullString();
					switch (mappedParam.ParameterAction)
					{
						case ParameterAction.Nothing:
							_sb.Append(paramName);
							break;
						case ParameterAction.SimpleReplacement:
						case ParameterAction.CustomReplacement:
							_sb.Append($"{mappedParam.Type.ToDisplayString()} {paramName}");
							break;
						case ParameterAction.FormatterReplacement:
							AppendFormatter(formatters[mappedParam.Type.OriginalDefinition]);
							break;
						case ParameterAction.GlobalFormatterReplacement:
							AppendFormatter(globalFormatters[mappedParam.Type.OriginalDefinition]);
							break;
						default:
							throw new ArgumentException($"Can't find case for {overloadMap[index]} parameterAction.");
					}

					void AppendFormatter(Formatter formatter)
					{
						for (int paramIndex = 0; paramIndex < formatter.ParamsCount; paramIndex++)
						{
							var formatterParam = formatter.GetParamByIndex(paramIndex, template);
							_sb.Append($"{formatterParam.Type} {paramName}{formatterParam.Name}, ");
							replacementModifiers.Add(($"{paramName}.{formatterParam.Name}",
								$"{paramName}{formatterParam.Name}"));
						}
					}
				}

				_sb.Append(")", 1);
				WriteMethodBody(method, replacementModifiers);
			}

			if (!ReferenceEquals(member, method) || isSmthShouldBeReplaced)
			{
				// Type overload

				// TODO: Insert attributes
				_sb.Append($"{method.Modifiers.ToFullString()}{returnType.ToFullString()}{method.Identifier.ToFullString()}(");
				for (int index = 0; index < parameters.Count; index++)
				{
					var mappedParam = overloadMap[index];
					string paramName = parameters[index].Identifier.ToFullString();
					INamedTypeSymbol? originalType;
					switch (mappedParam.ParameterAction)
					{
						case ParameterAction.Nothing:
							_sb.Append(paramName);
							break;
						case ParameterAction.SimpleReplacement:
						case ParameterAction.CustomReplacement:
							_sb.Append($"{mappedParam.Type.ToDisplayString()} {paramName}");
							break;
						case ParameterAction.FormatterReplacement:
							originalType = (INamedTypeSymbol) mappedParam.Type.OriginalDefinition.OriginalDefinition;
							AppendFormatter(originalType, formatters[originalType]);
							break;
						case ParameterAction.GlobalFormatterReplacement:
							originalType = (INamedTypeSymbol) mappedParam.Type.OriginalDefinition.OriginalDefinition;
							AppendFormatter(originalType, globalFormatters[originalType]);
							break;
						default:
							throw new ArgumentException($"Can't find case for {overloadMap[index]} parameterAction.");
					}

					// ReSharper disable once VariableHidesOuterVariable
					void AppendFormatter(INamedTypeSymbol originalType, Formatter formatter)
					{
						var @params = new ITypeSymbol[formatter.GenericParamsCount];
						for (int paramIndex = 0; paramIndex < formatter.GenericParamsCount; paramIndex++)
							@params[paramIndex] = formatter.GetGenericParamByIndex(paramIndex, template) ??
							                      throw new Exception("Can't get type");
						_sb.Append($"{originalType.Construct(@params).ToDisplayString()} {paramName}");
					}
				}

				_sb.Append(")", 1);
				WriteMethodBody(method, null);
			}

			_arrayPool.Return(overloadMap);
		}

		return _sb.NestedDecrease().ToStringAndClear();
	}

	private void WriteMethodBody(MethodDeclarationSyntax method, IList<(string From, string To)>? replaceModifiers)
	{
		// Body
		if (method.ExpressionBody is not null)
		{
			_sb.Append(method.ExpressionBody.ArrowToken.ToFullString())
				.Append(method.ExpressionBody.Expression.ToFullString())
				.Append(";", 1);
		}
		else if (method.Body is not null)
		{
			_sb.NestedIncrease();
			foreach (var statement in method.Body.Statements)
			{
				string strStatement;
				switch (statement)
				{
					// case LocalDeclarationStatementSyntax localDeclarationStatementSyntax:
					// 	mainSb.Append(string.Concat(statement.GetLeadingTrivia().ToFullString(), 
					// 		localDeclarationStatementSyntax.Declaration.WithType(SyntaxFactory.ParseTypeName("double ")).ToFullString(),
					// 		localDeclarationStatementSyntax.SemicolonToken.ToFullString()));
					// 	continue;
					// case ExpressionStatementSyntax expressionStatementSyntax:
					// 	if (expressionStatementSyntax.Expression is not AssignmentExpressionSyntax assignmentExpressionSyntax) goto default;
					// _sb.Append(string.Concat(assignmentExpressionSyntax.Left.ToFullString(),
					// 	" = (", type.Name, ") (",
					// 	assignmentExpressionSyntax.Right.ToFullString(),
					// 	")",
					// 	expressionStatementSyntax.SemicolonToken.ToFullString()));
					// 	continue;
					// case ReturnStatementSyntax returnStatementSyntax:
					// 	if (method.ReturnType is not PredefinedTypeSyntax) goto default;
					// _sb.Append(string.Concat(returnStatementSyntax.ReturnKeyword.ToFullString(),
					// 	"(", type.Name, ") (",
					// 	returnStatementSyntax.Expression?.ToFullString() ?? string.Empty,
					// 	")",
					// 	returnStatementSyntax.SemicolonToken.ToFullString()));
					// continue;
					default:
						strStatement = statement.ToFullString();
						break;
				}

				if (replaceModifiers is not null)
					foreach ((string from, string to) in replaceModifiers)
						strStatement = strStatement.Replace(from, to);

				_sb.Append(strStatement);
			}

			_sb.AppendLineAndNestedDecrease();
		}
	}

	private sealed class SyntaxReceiver : ISyntaxReceiver
	{
		public readonly List<AttributeSyntax> GlobalFormatterSyntaxes = new();
		public List<TypeEntrySyntax> Candidates { get; } = new();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			// TODO: Add globalFormatters
			if (syntaxNode is not TypeDeclarationSyntax {AttributeLists.Count: >= 1} declarationSyntax) return;

			var typeEntry = new TypeEntrySyntax(declarationSyntax);
			foreach (var attributeList in declarationSyntax.AttributeLists)
			foreach (var attribute in attributeList.Attributes)
			{
				string attrName = attribute.Name.GetName();
				if (attrName == AttributeNames.PartialOverloadsAttr
				    && attribute.ArgumentList is not null
				    && attribute.ArgumentList.Arguments.Count > 0)
				{
					foreach (var typeAttributeSyntax in attribute.ArgumentList.Arguments)
						typeEntry.OverloadTypes.Add((declarationSyntax.Identifier.ValueText, typeAttributeSyntax));
				}
				else if (attrName == AttributeNames.NewClassOverloadsAttr
				         && attribute.ArgumentList is not null
				         && attribute.ArgumentList.Arguments.Count > 2)
				{
					var args = attribute.ArgumentList.Arguments;
					string className = Regex.Replace(declarationSyntax.Identifier.ValueText,
						args[0].Expression.GetInnerText(),
						args[1].Expression.GetInnerText());

					typeEntry.OverloadTypes.Add((className, args[2]));
				}
				else if (attrName == AttributeNames.CustomOverloadAttr)
				{
					typeEntry.FormatterSyntaxes.Add(attribute);
					continue;
				}
				else continue;

				Candidates.Add(typeEntry);
			}
		}
	}
}
