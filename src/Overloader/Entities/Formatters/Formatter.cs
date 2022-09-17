using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities.Formatters.Params;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.Formatters;

internal readonly struct Formatter
{
	public readonly IParam[] GenericParams;
	public readonly IParam[] Params;

	public Formatter() => throw new NotSupportedException("Not allowed!");

	public Formatter(IParam[] genericParams, IParam[] @params)
	{
		GenericParams = genericParams;
		Params = @params;
	}

	public static (ITypeSymbol Type, Formatter Formatter) ParseFormatter(AttributeSyntax formatterSyntax, Compilation compilation)
	{
		var args = formatterSyntax.ArgumentList?.Arguments ??
		           throw new ArgumentException("Argument list for formatter can't be null.")
			           .WithLocation(formatterSyntax.GetLocation());
		if (args.Count != 3)
			throw new ArgumentException("Not enough parameters for formatter.")
				.WithLocation(formatterSyntax.GetLocation());

		var type = args[0].GetType(compilation);
		var namedTypeSymbol = type.GetRootType();
		if (namedTypeSymbol.IsUnboundGenericType) type = type.OriginalDefinition;
		
		if (args[1].Expression is not ArrayCreationExpressionSyntax arg1) throw new ArgumentException(
				$"Arg1 of formatter must be {nameof(ArrayCreationExpressionSyntax)}");
		if (args[2].Expression is not ArrayCreationExpressionSyntax arg2) throw new ArgumentException(
			$"Arg2 of formatter must be {nameof(ArrayCreationExpressionSyntax)}");

		var genericParams = ParseParams(arg1.Initializer, compilation, false);
		var @params = ParseParams(arg2.Initializer, compilation, true);

		return (type, new Formatter(genericParams, @params));
	}
	
	private static IParam[] ParseParams(InitializerExpressionSyntax? initializer, Compilation compilation, bool withNames)
	{
		if (initializer is null || initializer.Expressions.Count == 0) return Array.Empty<IParam>();
		if (withNames && initializer.Expressions.Count % 2 != 0)
			throw new ArgumentException($"Problem with count of expressions for named array in {initializer}.")
				.WithLocation(initializer.GetLocation());

		var @params = new IParam[withNames ? initializer.Expressions.Count / 2 : initializer.Expressions.Count];
		string? name = null;

		for (int index = 0, paramIndex = 0; index < initializer.Expressions.Count; index++)
		{
			if (withNames)
			{
				if (initializer.Expressions[index] is not LiteralExpressionSyntax str)
					throw new ArgumentException("Expression isn't LiteralExpressionSyntax.")
						.WithLocation(initializer.GetLocation());
				name = str.GetInnerText();
				index++;
			}

			@params[paramIndex++] = ParseParam(initializer.Expressions[index], compilation, name);
		}

		return @params;
	}

	private static IParam ParseParam(ExpressionSyntax expressionSyntax, Compilation compilation, string? name)
	{
		switch (expressionSyntax)
		{
			case LiteralExpressionSyntax str when str.GetInnerText() == "T":
				return TemplateParam.Create(name);
			case TypeOfExpressionSyntax typeSyntax:
				return TypeParam.Create(typeSyntax.GetType(compilation), name);
			case ImplicitArrayCreationExpressionSyntax implicitArrayCreationExpressionSyntax:
				var expressions = implicitArrayCreationExpressionSyntax.Initializer.Expressions;
				if (expressions.Count == 0 || expressions.Count % 2 != 0)
					throw new ArgumentException("expressions.Count == 0 || expressions.Count % 2 != 0")
						.WithLocation(implicitArrayCreationExpressionSyntax.GetLocation());

				var switchDict = new Dictionary<ITypeSymbol, IParam>(expressions.Count / 2, SymbolEqualityComparer.Default);
				for (int switchParamIndex = 0; switchParamIndex < expressions.Count; switchParamIndex += 2)
				{
					var value = ParseParam(expressions[switchParamIndex + 1], compilation, name);
					if (value is SwitchParam)
						throw new ArgumentException(
								$"Switch statement in switch statement was detected in {expressionSyntax.ToString()}.")
							.WithLocation(expressions[switchParamIndex + 1].GetLocation());
					switchDict.Add(expressions[switchParamIndex].GetType(compilation), value);
				}

				return SwitchParam.Create(switchDict, name);
			default:
				throw new ArgumentException(
						$"Can't recognize syntax when try to parse parameter in {expressionSyntax.ToString()}.")
					.WithLocation(expressionSyntax.GetLocation());
		}
	}
}
