using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities.Formatters.Params;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.Formatters;

internal sealed record Formatter(IParam[] GenericParams, (string Name, IParam Param)[] Params, Transition[] Transitions)
{
	public static (ITypeSymbol Type, Formatter Formatter) Parse(AttributeSyntax formatterSyntax, Compilation compilation)
	{
		var args = formatterSyntax.ArgumentList?.Arguments ??
		           throw new ArgumentException("Argument list for formatter can't be null.").WithLocation(formatterSyntax);
		if (args.Count < 3)
			throw new ArgumentException("Not enough parameters for formatter.").WithLocation(formatterSyntax);
		
		if (args[1].Expression is not ArrayCreationExpressionSyntax arg1)
			throw new ArgumentException(
				$"{nameof(arg1)} of {nameof(Formatter)} must be {nameof(ArrayCreationExpressionSyntax)}.");
		if (args[2].Expression is not ArrayCreationExpressionSyntax arg2)
			throw new ArgumentException(
				$"{nameof(arg2)} of {nameof(Formatter)} must be {nameof(ArrayCreationExpressionSyntax)}.");

		var type = args[0].GetType(compilation);
		var namedTypeSymbol = type.GetClearType();
		if (namedTypeSymbol.IsUnboundGenericType) type = type.OriginalDefinition;

		var genericParams = ParseParams(arg1.Initializer, compilation);
		var @params = ParseParamsWithNames(arg2.Initializer, compilation);

		int transitionsCount = args.Count - 3;
		var transitions = new Transition[transitionsCount];
		for (int argIndex = 3, mapIndex = 0; argIndex < args.Count; argIndex++, mapIndex++)
		{
			if (args[argIndex].Expression is not ArrayCreationExpressionSyntax { Initializer: var initializer })
				throw new ArgumentException(
					$"Arg of {nameof(Formatter)} must be {nameof(ArrayCreationExpressionSyntax)}.")
					.WithLocation(args[argIndex]);
			if (initializer is null) throw new ArgumentException($"Can't get initializer {argIndex}.")
				.WithLocation(args[argIndex].Expression);
			transitions[mapIndex] = Transition.Parse(initializer, compilation);
		}

		return (type, new Formatter(genericParams, @params, transitions));
	}

	private static IParam[] ParseParams(InitializerExpressionSyntax? initializer, Compilation compilation)
	{
		if (initializer is null || initializer.Expressions.Count == 0) return Array.Empty<IParam>();

		var @params = new IParam[initializer.Expressions.Count];
		for (int index = 0; index < @params.Length; index++)
			@params[index] = ParseParam(initializer.Expressions[index], compilation);

		return @params;
	}

	private static (string, IParam)[] ParseParamsWithNames(InitializerExpressionSyntax? initializer, Compilation compilation)
	{
		if (initializer is null || initializer.Expressions.Count == 0) return Array.Empty<(string, IParam)>();
		if (initializer.Expressions.Count % 2 != 0) throw new ArgumentException(
				$"Problem with count of expressions for named array in {initializer}.").WithLocation(initializer);
		
		var @params = new (string, IParam)[initializer.Expressions.Count / 2];

		for (int index = 0, paramIndex = 0; index < initializer.Expressions.Count; index++)
		{
			string name = initializer.Expressions[index++].GetVariableName();
			@params[paramIndex++] = (name, ParseParam(initializer.Expressions[index], compilation));
		}
		
		return @params;
	}

	private static IParam ParseParam(ExpressionSyntax expressionSyntax, Compilation compilation)
	{
		switch (expressionSyntax)
		{
			case LiteralExpressionSyntax str when str.GetInnerText() == "T":
				return TemplateParam.Create();
			case TypeOfExpressionSyntax typeSyntax:
				return TypeParam.Create(typeSyntax.GetType(compilation));
			case ImplicitArrayCreationExpressionSyntax implicitArrayCreationExpressionSyntax:
				var expressions = implicitArrayCreationExpressionSyntax.Initializer.Expressions;
				if (expressions.Count == 0 || expressions.Count % 2 != 0) throw new ArgumentException(
					"expressions.Count == 0 || expressions.Count % 2 != 0").WithLocation(implicitArrayCreationExpressionSyntax);

				var switchDict = new Dictionary<ITypeSymbol, IParam>(expressions.Count / 2, SymbolEqualityComparer.Default);
				for (int switchParamIndex = 0; switchParamIndex < expressions.Count; switchParamIndex += 2)
				{
					var value = ParseParam(expressions[switchParamIndex + 1], compilation);
					if (value is SwitchParam) throw new ArgumentException(
								$"Switch statement in switch statement was detected in {expressionSyntax.ToString()}.")
							.WithLocation(expressions[switchParamIndex + 1]);
					switchDict.Add(expressions[switchParamIndex].GetType(compilation), value);
				}

				return SwitchParam.Create(switchDict);
			default:
				throw new ArgumentException($"Can't recognize syntax when try to parse parameter in {expressionSyntax.ToString()}.")
					.WithLocation(expressionSyntax);
		}
	}
}
