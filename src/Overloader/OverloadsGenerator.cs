using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Utils;

namespace Overloader;

[Generator]
internal sealed class OverloadsGenerator : ISourceGenerator
{
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

		// 1. Analyze type attrs
		// 2. Generate custom overloads for basic type
		// 3. Generate overloads for all types
		foreach (var candidate in syntaxReceiver.Candidates)
		{
			var candidateClassName = candidate.Syntax.Identifier.ValueText;
			var formatters = candidate.GetFormatters(context.Compilation);
			context.AddSource($"{candidateClassName}.g.cs",
				GenerateOverloads(context.Compilation, formatters, candidate, candidateClassName, null));
			foreach ((string className, var argSyntax) in candidate.OverloadTypes)
				context.AddSource($"{className}.g.cs",
					GenerateOverloads(context.Compilation, formatters, candidate, className,
						TypeEntrySyntax.GetType(context.Compilation, argSyntax)));
		}
	}

	private string GenerateOverloads(Compilation compilation,
		Dictionary<ITypeSymbol, Formatter> formatters,
		TypeEntrySyntax entry,
		string className,
		ITypeSymbol? initType)
	{
		var initTypeSyntax = initType is not null? SyntaxFactory.ParseTypeName(initType.Name) : default;
		_sb.AppendUsings(entry.Syntax.GetTopParent().DescendantNodes().Where(node => node is UsingDirectiveSyntax))
			.AppendLine();

		_sb.AppendLine($"namespace {entry.Syntax.GetNamespace()};\n");

		// Declare class/struct signature
		_sb.Append(entry.Syntax.Modifiers.ToFullString())
			.Append(entry.Syntax.Keyword.ToFullString())
			.AppendLineAndNestedIncrease(className);

		MemberIterator:
		foreach (var member in entry.Syntax.Members)
		{
			if (member is not MethodDeclarationSyntax method)
			{
				_sb.AppendLine(member.ToFullString());
				continue;
			}

			// Method declaration
			var type = initType ?? TypeEntrySyntax.GetType(compilation, method.ReturnType);
			var typeSyntax = initTypeSyntax ?? method.ReturnType;
			
			if (initType is not null)
				foreach (var attrList in method.AttributeLists)
				foreach (var attribute in attrList.Attributes)
				{
					var attrName = attribute.Name.GetName();
					if (attrName == AttributeNames.IgnoreForAttr)
					{
						if (attribute.ArgumentList is null) continue;
						foreach (var arg in attribute.ArgumentList.Arguments)
							if (SymbolEqualityComparer.Default.Equals(TypeEntrySyntax.GetType(compilation, arg), initType))
								goto MemberIterator;
					}
					else if (attribute.ArgumentList is not null && attrName == AttributeNames.TAttr)
					{
						var argsCount = attribute.ArgumentList.Arguments.Count;
						if (argsCount == 2)
						{
							type = TypeEntrySyntax.GetType(compilation, attribute.ArgumentList.Arguments[0]);
							typeSyntax = SyntaxFactory.ParseTypeName(type.Name);
						}
						else if (argsCount > 1)
						{
								
						}

						method = method.WithReturnType(initTypeSyntax!);
					}
					else if (attrName == AttributeNames.ChangeAccessModifierAttr)
					{
						// TODO: Analyze modifiers and create realization
					}
				}

			_sb.Append($"{method.Modifiers.ToFullString()}{method.ReturnType.ToFullString()}{method.Identifier.ToFullString()}(");
			
			// TODO: Parameter replacement
			foreach (var parameterSyntax in method.ParameterList.Parameters)
				// Parameter with overload
				if (parameterSyntax.TryGetAttribute(AttributeNames.TAttr, out var attribute))
				{
					
				}
				// Default parameter. Nothing to change
				else
					_sb.Append(parameterSyntax.ToFullString());
			_sb.Append(")");

			// Body
			if (method.ExpressionBody is not null)
			{
				_sb.Append(method.ExpressionBody.ArrowToken.ToFullString())
					.Append(method.ExpressionBody.Expression.ToFullString())
					.AppendLine(";");
			}
			else if (method.Body is not null)
			{
				_sb.NestedIncrease();
				foreach (var statement in method.Body.Statements)
				{
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
							_sb.Append(statement.ToFullString());
							break;
					}
				}
				_sb.NestedDecrease();
			}
		}

		return _sb.NestedDecrease().ToStringAndClear();
	}

	private sealed class SyntaxReceiver : ISyntaxReceiver
	{
		public List<TypeEntrySyntax> Candidates { get; } = new();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is not TypeDeclarationSyntax { AttributeLists.Count: >= 1 } declarationSyntax) return;

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
					var className = Regex.Replace(declarationSyntax.Identifier.ValueText,
						args[0].Expression.GetText().GetInnerText(),
						args[1].Expression.GetText().GetInnerText());

					typeEntry.OverloadTypes.Add((className, args[2]));

				}
				else if (attrName == AttributeNames.CustomOverloadAttr)
				{
					typeEntry.Formatters.Add(attribute);
					continue;
				}
				else continue;
				
				Candidates.Add(typeEntry);
			}
		}
	}
	
	public readonly struct TypeEntrySyntax
	{
		public readonly TypeDeclarationSyntax Syntax;
		public readonly List<(string ClassName, AttributeArgumentSyntax TypeSyntax)> OverloadTypes = new();
		public readonly List<AttributeSyntax> Formatters = new();

		public TypeEntrySyntax(TypeDeclarationSyntax syntax) => Syntax = syntax;

		public Dictionary<ITypeSymbol, Formatter> GetFormatters(Compilation compilation)
		{
			var dict = new Dictionary<ITypeSymbol, Formatter>(Formatters.Count, SymbolEqualityComparer.Default);

			foreach (var formatterSyntax in Formatters)
			{
				var args = formatterSyntax.ArgumentList?.Arguments ??
				           throw new ArgumentException("Argument list can't be null.");
				var type = GetType(compilation, args[0]).OriginalDefinition;
				// Variables
				var vars = args[1].Expression.GetText()
					.ToString()
					.Split(',');
				
				// Generic overloads
				var generics = args[2].Expression.GetText()
					.ToString()
					.Split(',')
					.Select(arg => arg == "true")
					.ToArray();
				
				dict.Add(type, new Formatter(vars, generics));
			}

			return dict;
		}

		public static ITypeSymbol GetType(Compilation compilation, AttributeArgumentSyntax type) =>
			GetType(compilation, type.Expression);

		public static ITypeSymbol GetType(Compilation compilation, CSharpSyntaxNode node)
		{
			if (node is TypeOfExpressionSyntax typeOfExpressionSyntax)
				node = typeOfExpressionSyntax.Type;
			
			var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
			return semanticModel.GetTypeInfo(node).Type ??
			       throw new ArgumentException("Type not found.");
		}
	}
}

public readonly struct Formatter
{
	public readonly string[] Variables;
	public readonly bool[] GenericOverloads;
	
	public Formatter(string[] variables, bool[] genericOverloads)
	{
		Variables = variables;
		GenericOverloads = genericOverloads;
	}
}
