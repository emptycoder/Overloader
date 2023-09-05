// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using Overloader.Chains.Overloads.Utils;
// using Overloader.ContentBuilders;
// using Overloader.Entities;
// using Overloader.Enums;
// using Overloader.Exceptions;
// using Overloader.Utils;
//
// namespace Overloader.Chains.Overloads;
//
// public sealed class CombinedRefIntegrityOverloads : IChainMember
// {
// 	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
// 	{
// 		if (props.Store.OverloadMap is null
// 		    || !props.Store.IsSmthChanged
// 		    || props.Store.CombineParametersCount == 0)
// 			return ChainAction.NextMember;
//
// 		var entry = (MethodDeclarationSyntax) syntaxNode;
// 		var parameters = entry.ParameterList.Parameters;
// 		
// 		if (parameters.Count == 0) return ChainAction.NextMember;
// 		
// 		Span<sbyte> maxTransitionsCount = stackalloc sbyte[parameters.Count];
// 		Span<sbyte> transitionIndexes = stackalloc sbyte[parameters.Count];
// 		for (int index = 0; index < parameters.Count; index++)
// 		{
// 			var parameter = parameters[index];
// 			transitionIndexes[index] = -1;
// 			
// 			if (!props.Store.OverloadMap[index].IsCombineNotExists
// 				|| parameter.Modifiers.Any(modifier =>
// 				    modifier.IsKind(SyntaxKind.InKeyword)
// 				    || modifier.IsKind(SyntaxKind.RefKeyword)
// 				    || modifier.IsKind(SyntaxKind.OutKeyword))) continue;
// 			
// 			foreach (var attributeList in parameter.AttributeLists)
// 			foreach (var attribute in attributeList.Attributes)
// 			{
// 				if (attribute.Name.GetName() is not nameof(Ref)) continue;
// 				maxTransitionsCount[index] = 1;
// 			}
// 		}
// 		
// 		// Check that ref attribute exists
// 		for (int index = 0;;)
// 		{
// 			if (maxTransitionsCount[index] != 0)
// 			{
// 				// Skip first, because it was generated by IntegrityOverload
// 				transitionIndexes[index] = 0;
// 				break;
// 			}
//
// 			if (++index == maxTransitionsCount.Length) return ChainAction.NextMember;
// 		}
//
// 		using var bodyBuilder = StringSourceBuilder.Instance;
// 		for (;;)
// 		{
// 			bodyBuilder
// 				.TrimAppend(entry.Identifier.ToString())
// 				.AppendAsConstant("(");
//
// 			props.Builder
// 				.AppendChainMemberNameComment(nameof(CombinedRefIntegrityOverloads))
// 				.TrimAppend(entry.GetLeadingTrivia().ToString(), 1)
// 				.AppendMethodDeclarationSpecifics(entry, props.Store.MethodData)
// 				.AppendAsConstant("(");
// 			
// 			for (int index = 0;;)
// 			{
// 				var mappedParam = props.Store.OverloadMap[index];
// 				var parameter = parameters[index];
// 				
// 				if (transitionIndexes[index] != -1)
// 					parameter = parameter.WithModifiers(parameter.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.RefKeyword)));
//
// 				if (mappedParam.IsCombineNotExists)
// 				{
// 					string paramName = parameter.Identifier.ToString();
// 					switch (mappedParam.ParameterReplacement)
// 					{
// 						case ParameterReplacement.None:
// 							props.Builder.TrimAppend(parameter.ToFullString());
// 							break;
// 						case ParameterReplacement.Template:
// 						case ParameterReplacement.UserType:
// 							props.Builder.AppendParameter(parameter, mappedParam, props.Compilation);
// 							break;
// 						case ParameterReplacement.FormatterIntegrity:
// 						case ParameterReplacement.Formatter:
// 							props.Builder.AppendIntegrityParam(props, mappedParam, parameter);
// 							break;
// 						default:
// 							throw new ArgumentException($"Can't find case for {props.Store.OverloadMap[index]} parameterAction.")
// 								.WithLocation(entry);
// 					}
//
// 					if (transitionIndexes[index] != -1)
// 						bodyBuilder
// 							.AppendAsConstant("ref")
// 							.WhiteSpace()
// 							.TrimAppend(paramName);
// 					else
// 						bodyBuilder.AppendVariableToBody(parameter, paramName);
//
// 					if (++index == parameters.Count) break;
// 					if (props.Store.OverloadMap[index].IsCombineNotExists)
// 						props.Builder
// 							.AppendAsConstant(",")
// 							.WhiteSpace();
// 					bodyBuilder
// 						.AppendAsConstant(",")
// 						.WhiteSpace();
// 				}
// 				else
// 				{
// 					bodyBuilder.AppendCombinedSimple(mappedParam, parameters[mappedParam.CombineIndex]);
// 					if (++index == parameters.Count) break;
// 					if (props.Store.OverloadMap[index].IsCombineNotExists)
// 						props.Builder
// 							.AppendAsConstant(",")
// 							.WhiteSpace();
// 					bodyBuilder
// 						.AppendAsConstant(",")
// 						.WhiteSpace();
// 				}
// 			}
// 			
// 			props.Builder
// 				.AppendAsConstant(")")
// 				.WhiteSpace()
// 				.TrimAppend(entry.ConstraintClauses.ToString());
//
// 			if (props.Store.ShouldRemoveBody)
// 				props.Builder.AppendAsConstant(";");
// 			else
// 				props.Builder
// 					.WhiteSpace()
// 					.TrimAppend("=>", 1)
// 					.NestedIncrease()
// 					.AppendRefReturnValues(entry.ReturnType)
// 					.AppendAndClear(bodyBuilder)
// 					.AppendAsConstant(");", 1)
// 					.NestedDecrease();
// 			
// 			/*
// 				0 0 0 0 0
// 				^
//
// 				1 0 0 0 0
// 				^
// 				Repeat until it < maxLength[index]
// 				And when first value equals maxLength[index] reset to zero and add 1 to next rank
// 				0 1 0 0 0
// 				^
// 				
// 				1 1 0 0 0
// 				^
// 				And so on...
// 			 */
// 			for (int index = 0;;)
// 			{
// 				if (transitionIndexes[index] != maxTransitionsCount[index]
// 				    && ++transitionIndexes[index] != maxTransitionsCount[index]) break;
// 				transitionIndexes[index] = -1;
//
// 				if (++index == transitionIndexes.Length)
// 					return ChainAction.NextMember;
// 			}
// 		}
// 	}
// }
