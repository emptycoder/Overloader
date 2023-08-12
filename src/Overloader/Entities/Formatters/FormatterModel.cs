using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities.Formatters.Params;
using Overloader.Entities.Formatters.Transitions;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.Formatters;

public sealed record FormatterModel(
	string Identifier,
	ITypeSymbol[] Types,
	IParamValue[] GenericParams,
	(string Identifier, IParamValue Param)[] Params,
	List<DecompositionModel> Decompositions,
	List<CastModel> Casts,
	List<CastModel> CastsForDecomposition,
	List<CastModel> CastsForIntegrity)
{

	public static FormatterModel Parse(AttributeSyntax formatterSyntax, Compilation compilation)
	{
		var args = formatterSyntax.ArgumentList?.Arguments ??
		           throw new ArgumentException("Argument list for formatter can't be null.")
			           .WithLocation(formatterSyntax);
		const int beforeTransitionParamsCount = 4;
		if (args.Count < beforeTransitionParamsCount)
			throw new ArgumentException("Not enough parameters for formatter.")
				.WithLocation(formatterSyntax);

		if (args[0].Expression is not LiteralExpressionSyntax identifier)
			throw new ArgumentException($"Identifier should be {nameof(LiteralExpressionSyntax)}.")
				.WithLocation(args[0].Expression);

		ITypeSymbol[] types;
		ITypeSymbol? type;
		switch (args[1].Expression)
		{
			case ImplicitArrayCreationExpressionSyntax {Initializer.Expressions: var expressions}:
				if (expressions.Count == 0)
					throw new ArgumentException("Count of types for formatter can't be 0.")
						.WithLocation(args[1].Expression);

				types = new ITypeSymbol[expressions.Count];
				for (int index = 0; index < expressions.Count; index++)
				{
					type = expressions[index].GetType(compilation);
					if (type.GetClearType().IsUnboundGenericType) type = type.OriginalDefinition;
					types[index] = type;
				}

				break;
			case TypeOfExpressionSyntax:
				type = args[1].Expression.GetType(compilation);
				if (type.GetClearType().IsUnboundGenericType) type = type.OriginalDefinition;
				types = new[] {type};
				break;
			default:
				throw new ArgumentException($"{args[1].Expression.ToString()} isn't allowed. Should be Type or [ Type0, Type1 ].");
		}

		if (args[2].Expression is not ArrayCreationExpressionSyntax arg1)
			throw new ArgumentException(
					$"{nameof(arg1)} of {nameof(FormatterModel)} should be {nameof(ArrayCreationExpressionSyntax)}.")
				.WithLocation(args[2].Expression);
		if (args[3].Expression is not ArrayCreationExpressionSyntax arg2)
			throw new ArgumentException(
					$"{nameof(arg2)} of {nameof(FormatterModel)} should be {nameof(ArrayCreationExpressionSyntax)}.")
				.WithLocation(args[3].Expression);

		var genericParams = ParseParams(arg1.Initializer, compilation);
		var @params = ParseParamsWithNames(arg2.Initializer, compilation);
        
		var decompositions = new List<DecompositionModel>();
		var casts = new List<CastModel>();
		var castsForDecomposition = new List<CastModel>();
		var castsForIntegrity = new List<CastModel>();
		for (int argIndex = beforeTransitionParamsCount; argIndex < args.Count; argIndex++)
		{
			if (args[argIndex].Expression is not ArrayCreationExpressionSyntax {Initializer.Expressions: var argExpressions})
				throw new ArgumentException($"Argument of {nameof(FormatterModel)} should be {nameof(ArrayCreationExpressionSyntax)}.")
					.WithLocation(args[argIndex].Expression);

			
			if (argExpressions[0] is not MemberAccessExpressionSyntax transitionTypeExpression)
				throw new ArgumentException($"{nameof(TransitionType)} should be specified as a first parameter.")
					.WithLocation(argExpressions[0]);

			switch (transitionTypeExpression.Name.ToString())
			{
				case nameof(TransitionType.Decomposition):
					decompositions.Add(DecompositionModel.Parse(argExpressions, compilation));
					break;
				case nameof(TransitionType.Cast):
					casts.Add(CastModel.Parse(argExpressions, compilation));
					break;
				case nameof(TransitionType.CastForDecomposition):
					castsForIntegrity.Add(CastModel.Parse(argExpressions, compilation));
					break;
				case nameof(TransitionType.CastForIntegrity):
					castsForIntegrity.Add(CastModel.Parse(argExpressions, compilation));
					break;
				default:
					throw new ArgumentException($"Value '{transitionTypeExpression.Name}' out of valid range ('{
							string.Join(", ", Enum.GetNames(typeof(TransitionType)))
						}').").WithLocation(argExpressions[0]);
			}
		}

		return new FormatterModel(
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

	private static IParamValue[] ParseParams(InitializerExpressionSyntax? initializer, Compilation compilation)
	{
		if (initializer is null || initializer.Expressions.Count == 0)
			return Array.Empty<IParamValue>();

		var @params = new IParamValue[initializer.Expressions.Count];
		for (int index = 0; index < @params.Length; index++)
			@params[index] = ParseParam(initializer.Expressions[index], compilation);

		return @params;
	}

	private static (string, IParamValue)[] ParseParamsWithNames(InitializerExpressionSyntax? initializer, Compilation compilation)
	{
		if (initializer is null || initializer.Expressions.Count == 0)
			return Array.Empty<(string, IParamValue)>();
		
		if (initializer.Expressions.Count % 2 != 0)
			throw new ArgumentException($"Problem with count of expressions for named array in {initializer}.")
				.WithLocation(initializer);

		var @params = new (string, IParamValue)[initializer.Expressions.Count / 2];
		for (int index = 0, paramIndex = 0; index < initializer.Expressions.Count; index++)
		{
			string name = initializer.Expressions[index++].GetVariableName();
			@params[paramIndex++] = (name, ParseParam(initializer.Expressions[index], compilation));
		}

		return @params;
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
