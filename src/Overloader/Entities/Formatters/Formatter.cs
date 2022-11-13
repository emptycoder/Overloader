using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities.Formatters.Params.Value;
using Overloader.Entities.Formatters.Transitions;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.Formatters;

internal sealed record Formatter(
	string Identifier,
	ITypeSymbol[] Types,
	IParamValue[] GenericParams,
	(string Identifier, IParamValue Param)[] Params,
	Memory<IntegrityTransition> IntegrityTransitions,
	Memory<DeconstructTransition> DeconstructTransitions)
{
	public static Formatter Parse(AttributeSyntax formatterSyntax, Compilation compilation)
	{
		var args = formatterSyntax.ArgumentList?.Arguments ??
		           throw new ArgumentException("Argument list for formatter can't be null.")
			           .WithLocation(formatterSyntax);
		const int beforeTransitionParamsCount = 4;
		if (args.Count < beforeTransitionParamsCount)
			throw new ArgumentException("Not enough parameters for formatter.")
				.WithLocation(formatterSyntax);

		if (args[0].Expression is not LiteralExpressionSyntax identifier)
			throw new ArgumentException("Identifier must be LiteralExpressionSyntax.")
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
				throw new ArgumentException(
					$"{args[1].Expression.ToString()} isn't allowed. Use TypeOfExpressionSyntax/ImplicitArrayCreationExpressionSyntax.");
		}

		if (args[2].Expression is not ArrayCreationExpressionSyntax arg1)
			throw new ArgumentException(
					$"{nameof(arg1)} of {nameof(Formatter)} must be {nameof(ArrayCreationExpressionSyntax)}.")
				.WithLocation(args[2].Expression);
		if (args[3].Expression is not ArrayCreationExpressionSyntax arg2)
			throw new ArgumentException(
					$"{nameof(arg2)} of {nameof(Formatter)} must be {nameof(ArrayCreationExpressionSyntax)}.")
				.WithLocation(args[3].Expression);

		var genericParams = ParseParams(arg1.Initializer, compilation);
		var @params = ParseParamsWithNames(arg2.Initializer, compilation);

		int transitionsCount = args.Count - beforeTransitionParamsCount;
		int integrityTransitionIndex = 0;
		int deconstructTransitionIndex = transitionsCount - 1;
		var transitionMemory = new Memory<object>(new object[transitionsCount]);
		var transitions = transitionMemory.Span;
		for (int argIndex = beforeTransitionParamsCount; argIndex < args.Count; argIndex++)
		{
			if (args[argIndex].Expression is not ArrayCreationExpressionSyntax {Initializer.Expressions: var argExpressions})
				throw new ArgumentException(
						$"Arg of {nameof(Formatter)} must be {nameof(ArrayCreationExpressionSyntax)}.")
					.WithLocation(args[argIndex].Expression);
			if (argExpressions.Count < 2)
				throw new ArgumentException("Empty transition not allowed.")
					.WithLocation(args[argIndex].Expression);

			if (argExpressions[1] is ArrayCreationExpressionSyntax)
				transitions[deconstructTransitionIndex--] = DeconstructTransition.Parse(argExpressions, compilation);
			else
				transitions[integrityTransitionIndex++] = IntegrityTransition.Parse(argExpressions, compilation);
				
		}

		var integrityTransitions = transitionMemory.Slice(0, integrityTransitionIndex);
		var deconstructTransitions = transitionMemory.Slice(integrityTransitionIndex);

		return new Formatter(
			identifier.GetInnerText(),
			types,
			genericParams,
			@params,
			Unsafe.As<Memory<object>, Memory<IntegrityTransition>>(ref integrityTransitions),
			Unsafe.As<Memory<object>, Memory<DeconstructTransition>>(ref deconstructTransitions)
		);
	}

	private static IParamValue[] ParseParams(InitializerExpressionSyntax? initializer, Compilation compilation)
	{
		if (initializer is null || initializer.Expressions.Count == 0) return Array.Empty<IParamValue>();

		var @params = new IParamValue[initializer.Expressions.Count];
		for (int index = 0; index < @params.Length; index++)
			@params[index] = ParseParam(initializer.Expressions[index], compilation);

		return @params;
	}

	private static (string, IParamValue)[] ParseParamsWithNames(InitializerExpressionSyntax? initializer, Compilation compilation)
	{
		if (initializer is null || initializer.Expressions.Count == 0) return Array.Empty<(string, IParamValue)>();
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
					throw new ArgumentException("expressions.Count == 0 || expressions.Count % 2 != 0.")
						.WithLocation(implicitArrayCreationExpressionSyntax);

				var switchDict = new Dictionary<ITypeSymbol, IParamValue>(expressions.Count / 2, SymbolEqualityComparer.Default);
				for (int switchParamIndex = 0; switchParamIndex < expressions.Count; switchParamIndex += 2)
				{
					var value = ParseParam(expressions[switchParamIndex + 1], compilation);
					if (value is SwitchParamValue)
						throw new ArgumentException(
								$"Switch statement in switch statement was detected in {expressionSyntax.ToString()}.")
							.WithLocation(expressions[switchParamIndex + 1]);
					switchDict.Add(expressions[switchParamIndex].GetType(compilation), value);
				}

				return SwitchParamValue.Create(switchDict);
			default:
				throw new ArgumentException(
						$"Can't recognize syntax when try to parse parameter in {expressionSyntax.ToString()}.")
					.WithLocation(expressionSyntax);
		}
	}
}
