using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities.DTOs.Attributes.Formatters.Params;
using Overloader.Entities.DTOs.Attributes.Formatters.Transitions;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.DTOs.Attributes.Formatters;

public sealed record FormatterDto(
	string Identifier,
	ITypeSymbol[] Types,
	IParamValue[] GenericParams,
	(string Identifier, IParamValue Param)[] Params,
	List<DecomposeTransitionDto> Decompositions,
	List<CastTransitionDto> Casts,
	List<CastTransitionDto> CastsForDecomposition,
	List<CastTransitionDto> CastsForIntegrity)
{
	public static FormatterDto Parse(AttributeSyntax formatterSyntax, Compilation compilation)
	{
		var args = formatterSyntax.ArgumentList?.Arguments ?? [];
		const int beforeTransitionParamsCount = 4;
		if (args.Count < beforeTransitionParamsCount)
			throw new ArgumentException("Not enough parameters for formatter.")
				.WithLocation(formatterSyntax);

		if (args[0].Expression is not LiteralExpressionSyntax identifier || !identifier.IsKind(SyntaxKind.StringLiteralExpression))
			throw new ArgumentException($"Identifier should be {nameof(SyntaxKind.StringLiteralExpression)}.")
				.WithLocation(args[0].Expression);

		var types = ParseFormatterTypes(args[1].Expression, compilation);
		var genericParams = ParseParams(args[2].Expression, compilation);
		var @params = ParseParamsWithNames(args[3].Expression, compilation);

		var decompositions = new List<DecomposeTransitionDto>();
		var casts = new List<CastTransitionDto>();
		var castsForDecomposition = new List<CastTransitionDto>();
		var castsForIntegrity = new List<CastTransitionDto>();
		for (int argIndex = beforeTransitionParamsCount; argIndex < args.Count; argIndex++)
		{
			if (args[argIndex].Expression is not ArrayCreationExpressionSyntax {Initializer.Expressions: var argExpressions})
				throw new ArgumentException($"Argument of {nameof(FormatterDto)} should be {nameof(ArrayCreationExpressionSyntax)}.")
					.WithLocation(args[argIndex].Expression);


			if (argExpressions[0] is not MemberAccessExpressionSyntax transitionTypeExpression)
				throw new ArgumentException($"{nameof(TransitionType)} should be specified as a first parameter.")
					.WithLocation(argExpressions[0]);

			switch (transitionTypeExpression.Name.ToString())
			{
				case nameof(TransitionType.Decomposition):
					decompositions.Add(DecomposeTransitionDto.Parse(argExpressions, compilation));
					break;
				case nameof(TransitionType.Cast):
					casts.Add(CastTransitionDto.Parse(argExpressions, compilation));
					break;
				case nameof(TransitionType.CastForDecomposition):
					castsForDecomposition.Add(CastTransitionDto.Parse(argExpressions, compilation));
					break;
				case nameof(TransitionType.CastForIntegrity):
					castsForIntegrity.Add(CastTransitionDto.Parse(argExpressions, compilation));
					break;
				default:
					throw new ArgumentException($"Value '{transitionTypeExpression.Name}' out of valid range.").WithLocation(argExpressions[0]);
			}
		}

		return new FormatterDto(
			identifier.GetInnerText(),
			types,
			genericParams,
			@params,
			decompositions,
			casts,
			castsForDecomposition,
			castsForIntegrity
		);

		
	}

	private static ITypeSymbol[] ParseFormatterTypes(ExpressionSyntax expression, Compilation compilation)
	{
		ITypeSymbol[] types;
		switch (expression)
		{
			case CollectionExpressionSyntax { Elements: { Count: >= 1 } collectionElements }:
				var collectionExpressions = new ExpressionSyntax[collectionElements.Count];
				for (int index = 0; index < collectionElements.Count; index++)
				{
					var element = collectionElements[index];
					if (element is not ExpressionElementSyntax {Expression: TypeOfExpressionSyntax typeSyntax})
						throw new ArgumentException($"Expression isn't {nameof(TypeOfExpressionSyntax)}.")
							.WithLocation(expression);

					collectionExpressions[index] = typeSyntax;
				}

				types = ParseExpressions(compilation, collectionExpressions);
				break;
			case ImplicitArrayCreationExpressionSyntax { Initializer.Expressions: { Count: >= 1 } expressions }:
				types = ParseExpressions(compilation, expressions.ToArray());
				break;
			case ArrayCreationExpressionSyntax { Initializer.Expressions: { Count: >= 1 } expressions }:
				types = ParseExpressions(compilation, expressions.ToArray());
				break;
			case TypeOfExpressionSyntax:
				types = ParseExpressions(compilation, expression);
				break;
			default:
				throw new ArgumentException($"{expression.ToString()} isn't allowed. Should be Type or [ Type0, Type1 ].");
		}

		return types;
		
		static ITypeSymbol[] ParseExpressions(Compilation compilation, params ExpressionSyntax[] expressions)
		{
			var types = new ITypeSymbol[expressions.Length];
			for (int index = 0; index < expressions.Length; index++)
			{
				var type = expressions[index].GetType(compilation);
				if (type.GetClearType().IsUnboundGenericType) type = type.OriginalDefinition;
				types[index] = type;
			}

			return types;
		}
	}

	private static IParamValue[] ParseParams(ExpressionSyntax expression, Compilation compilation)
	{
		IParamValue[] @params;
		switch (expression)
		{
			case CollectionExpressionSyntax { Elements: var collectionElements }:
				var collectionExpressions = new ExpressionSyntax[collectionElements.Count];
				for (int index = 0; index < collectionElements.Count; index++)
				{
					var element = collectionElements[index];
					if (element is not ExpressionElementSyntax {Expression: TypeOfExpressionSyntax typeSyntax})
						throw new ArgumentException($"Expression isn't {nameof(TypeOfExpressionSyntax)}.")
							.WithLocation(expression);

					collectionExpressions[index] = typeSyntax;
				}

				@params = ParseExpressions(compilation, collectionExpressions);
				break;
			case ImplicitArrayCreationExpressionSyntax { Initializer.Expressions: var expressions }:
				@params = ParseExpressions(compilation, expressions.ToArray());
				break;
			case ArrayCreationExpressionSyntax { Initializer.Expressions: var expressions }:
				@params = ParseExpressions(compilation, expressions.ToArray());
				break;
			case TypeOfExpressionSyntax:
				@params = ParseExpressions(compilation, expression);
				break;
			default:
				throw new ArgumentException($"{expression.ToString()} isn't allowed. Should be Type or [ Type0, Type1 ].");
		}

		return @params;
		
		static IParamValue[] ParseExpressions(Compilation compilation, params ExpressionSyntax[] expressions)
		{
			var @params = new IParamValue[expressions.Length];
			for (int index = 0; index < expressions.Length; index++)
			{
				@params[index] = ParseParam(expressions[index], compilation);
			}

			return @params;
		}
	}

	private static (string Name, IParamValue Value)[] ParseParamsWithNames(ExpressionSyntax expression, Compilation compilation)
	{
		(string, IParamValue)[] @params;
		switch (expression)
		{
			case CollectionExpressionSyntax { Elements: var collectionElements }:
				var collectionExpressions = new SeparatedSyntaxList<ExpressionSyntax>();
				foreach (var element in collectionElements)
				{
					if (element is not ExpressionElementSyntax { Expression: TypeOfExpressionSyntax typeSyntax })
						throw new ArgumentException($"Expression isn't {nameof(TypeOfExpressionSyntax)}.")
							.WithLocation(expression);

					collectionExpressions.Add(typeSyntax);
				}
				@params = ParseExpressions(collectionExpressions, compilation);
				break;
			case ImplicitArrayCreationExpressionSyntax { Initializer.Expressions: var expressions }:
				@params = ParseExpressions(expressions, compilation);
				break;
			case ArrayCreationExpressionSyntax { Initializer.Expressions: var expressions }:
				@params = ParseExpressions(expressions, compilation);
				break;
			case TypeOfExpressionSyntax:
				@params = ParseExpressions([ expression ], compilation);
				break;
			default:
				throw new ArgumentException($"{expression.ToString()} isn't allowed. Should be Type or [ Type0, Type1 ].");
		}

		return @params;
		
		static (string Name, IParamValue Value)[] ParseExpressions(SeparatedSyntaxList<ExpressionSyntax> expressions, Compilation compilation)
		{
			if (expressions.Count % 2 != 0)
				throw new ArgumentException("Problem with count of expressions for named array.")
					.WithLocation(expressions[0]);
			
			var @params = new (string, IParamValue)[expressions.Count / 2];
			for (int index = 0, paramIndex = 0; index < expressions.Count; index++)
			{
				string name = expressions[index++].GetVariableName();
				var value = ParseParam(expressions[index], compilation);
				@params[paramIndex++] = (name, value);
			}

			return @params;
		}
	}

	private static IParamValue ParseParam(ExpressionSyntax expressionSyntax, Compilation compilation)
	{
		switch (expressionSyntax)
		{
			case LiteralExpressionSyntax str when str.GetInnerText() == "T":
				return TemplateParamValue.Create();
			case TypeOfExpressionSyntax typeSyntax:
				return TypeParamValue.Create(typeSyntax.GetType(compilation));
			case ImplicitArrayCreationExpressionSyntax implicitArrayCreationExpressionSyntax:
				var expressions = implicitArrayCreationExpressionSyntax.Initializer.Expressions;
				if (expressions.Count == 0 || expressions.Count % 2 != 0)
					throw new ArgumentException("Rule (expressions.Count == 0 || expressions.Count % 2 != 0) wasn't applied.")
						.WithLocation(implicitArrayCreationExpressionSyntax);

				var switchDictionary = new Dictionary<ITypeSymbol, IParamValue>(expressions.Count / 2, SymbolEqualityComparer.Default);
				for (int switchParamIndex = 0; switchParamIndex < expressions.Count; switchParamIndex += 2)
				{
					var value = ParseParam(expressions[switchParamIndex + 1], compilation);
					if (value is SwitchParamValue)
						throw new ArgumentException($"Not allowed nested switch statement was detected in {expressionSyntax.ToString()}.")
							.WithLocation(expressions[switchParamIndex + 1]);
					switchDictionary.Add(expressions[switchParamIndex].GetType(compilation), value);
				}

				return SwitchParamValue.Create(switchDictionary);
			default:
				throw new ArgumentException(
						$"Can't recognize syntax when try to parse parameter in {expressionSyntax.ToString()}.")
					.WithLocation(expressionSyntax);
		}
	}
}
