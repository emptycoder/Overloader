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
// public sealed class RefIntegrityOverloads : IChainMember
// {
// 	private enum Ref : byte
// 	{
// 		None,
// 		Enable,
// 		Disable
// 	}
// 	
// 	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
// 	{
// 		if (props.Store.OverloadMap is null
// 		    || !props.Store.IsSmthChanged)
// 			return ChainAction.NextMember;
//
// 		var entry = (MethodDeclarationSyntax) syntaxNode;
// 		var parameters = entry.ParameterList.Parameters;
// 		
// 		if (parameters.Count == 0) return ChainAction.NextMember;
// 		
// 		Span<Ref> transitions = stackalloc Ref[parameters.Count];
// 		for (int index = 0; index < parameters.Count; index++)
// 		{
// 			var parameter = parameters[index];
// 			transitions[index] = Ref.None;
// 			
// 			if (parameter.Modifiers.Any(modifier =>
// 				    modifier.IsKind(SyntaxKind.InKeyword)
// 				    || modifier.IsKind(SyntaxKind.RefKeyword)
// 				    || modifier.IsKind(SyntaxKind.OutKeyword))) continue;
// 			
// 			foreach (var attributeList in parameter.AttributeLists)
// 			foreach (var attribute in attributeList.Attributes)
// 			{
// 				if (attribute.Name.GetName() is not nameof(Ref)) continue;
// 				transitions[index] = Ref.Disable;
// 			}
// 		}
// 		
// 		// Check that ref attribute exists
// 		for (int index = 0;;)
// 		{
// 			if (transitions[index] == Ref.Disable)
// 			{
// 				// Skip first, because it was generated by IntegrityOverload
// 				transitions[index] = Ref.Enable;
// 				break;
// 			}
//
// 			if (++index == transitions.Length) return ChainAction.NextMember;
// 		}
// 		
// 		using var bodyBuilder = StringSourceBuilder.Instance;
// 		for (;;)
// 		{
// 			props.Builder
// 				.AppendChainMemberNameComment(nameof(MainRefIntegrityOverload))
// 				.TrimAppend(entry.GetLeadingTrivia().ToString(), 1)
// 				.AppendMethodDeclarationSpecifics(entry, props.Store.MethodData)
// 				.AppendAsConstant("(");
// 			
// 			for (int index = 0;;)
// 			{
// 				var mappedParam = props.Store.OverloadMap[index];
// 				var parameter = parameters[index];
//
// 				if (transitions[index] is Ref.Enable)
// 					parameter = parameter.WithModifiers(parameter.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.RefKeyword)));
//
// 				switch (mappedParam.ParameterReplacement)
// 				{
// 					case ParameterReplacement.None:
// 						props.Builder.TrimAppend(parameter.ToFullString());
// 						break;
// 					case ParameterReplacement.Template:
// 					case ParameterReplacement.UserType:
// 						props.Builder.AppendParameter(parameter, mappedParam, props.Compilation);
// 						break;
// 					case ParameterReplacement.FormatterIntegrity:
// 					case ParameterReplacement.Formatter:
// 						props.Builder.AppendIntegrityParam(props, mappedParam, parameter);
// 						break;
// 					default:
// 						throw new ArgumentException($"Can't find case for {props.Store.OverloadMap[index]} parameterAction.")
// 							.WithLocation(entry);
// 				}
//
// 				if (transitions[index] is not Ref.None)
// 					bodyBuilder
// 						.AppendAsConstant("ref")
// 						.WhiteSpace()
// 						.TrimAppend(parameter.Identifier.ValueText);
// 				else
// 					bodyBuilder.AppendVariableToBody(parameter);
//
// 				if (++index == parameters.Count) break;
// 				props.Builder
// 					.AppendAsConstant(",")
// 					.WhiteSpace();
// 				bodyBuilder
// 					.AppendAsConstant(",")
// 					.WhiteSpace();
// 			}
//
// 			props.Builder
// 				.AppendAsConstant(")")
// 				.WhiteSpace()
// 				.TrimAppend("=>", 1)
// 				.NestedIncrease()
// 				.AppendRefReturnValues(entry.ReturnType)
// 				.AppendAndClear(bodyBuilder)
// 				.AppendAsConstant(");", 1)
// 				.NestedDecrease();
// 			
// 			for (int index = 0;;)
// 			{
// 				if (transitions[index] is Ref.None) continue;
// 				var data = transitions[index] = transitions[index] is Ref.Disable
// 					? Ref.Enable
// 					: Ref.Disable;
//
// 				// Skip last, because it was generated by MainRefIntegrityOverload
// 				if (++index == transitions.Length)
// 					return ChainAction.NextMember;
// 				
// 				if (data is Ref.Enable) break;
// 			}
// 		}
// 	}
// }
