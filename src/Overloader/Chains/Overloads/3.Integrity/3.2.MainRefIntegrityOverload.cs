// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using Overloader.Chains.Overloads.Overloads;
// using Overloader.Chains.Overloads.Utils;
// using Overloader.ContentBuilders;
// using Overloader.Entities;
// using Overloader.Enums;
// using Overloader.Exceptions;
// using Overloader.Utils;
//
// namespace Overloader.Chains.Overloads;
//
// public sealed class MainRefIntegrityOverload : BodyMethodsOverloader, IChainMember
// {
// 	private enum Ref : byte
// 	{
// 		None,
// 		Enable
// 	}
// 	
// 	ChainAction IChainMember.Execute(GeneratorProperties props)
// 	{
// 		if (props.Store.OverloadMap is null
// 		    || !props.Store.IsSmthChanged)
// 			return ChainAction.NextMember;
// 		
// 		var parameters = props.Store.MethodSyntax.ParameterList.Parameters;
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
// 				transitions[index] = Ref.Enable;
// 			}
// 		}
// 		
// 		// Check that ref attribute exists
// 		for (int index = 0;;)
// 		{
// 			if (transitions[index] == Ref.Enable) break;
// 			if (++index == transitions.Length) return ChainAction.NextMember;
// 		}
// 		
// 		props.Builder
// 			.AppendChainMemberNameComment(nameof(MainRefIntegrityOverload))
// 			.TrimAppend(props.Store.MethodSyntax.GetLeadingTrivia().ToString(), 1)
// 			.AppendMethodDeclarationSpecifics(props.Store.MethodSyntax, props.Store.MethodData)
// 			.AppendAsConstant("(");
// 			
// 		for (int index = 0;;)
// 		{
// 			var mappedParam = props.Store.OverloadMap[index];
// 			var parameter = parameters[index];
// 				
// 			if (transitions[index] == Ref.Enable)
// 				parameter = parameter.WithModifiers(parameter.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.RefKeyword)));
//
// 			switch (mappedParam.ReplacementType)
// 			{
// 				case ParameterReplacement.None:
// 					props.Builder.TrimAppend(parameter.ToFullString());
// 					break;
// 				case ParameterReplacement.Template:
// 				case ParameterReplacement.UserType:
// 					props.Builder.AppendParameter(parameter, mappedParam, props.Compilation);
// 					break;
// 				case ParameterReplacement.FormatterIntegrity:
// 				case ParameterReplacement.Formatter:
// 					props.Builder.AppendIntegrityParam(props, mappedParam, parameter);
// 					break;
// 				default:
// 					throw new ArgumentException($"Can't find case for {props.Store.OverloadMap[index]} parameterAction.")
// 						.Unexpected()
// 						.WithLocation(props.Store.MethodSyntax);
// 			}
//
// 			if (++index == parameters.Count) break;
// 			props.Builder
// 				.AppendAsConstant(",")
// 				.WhiteSpace();
// 		}
// 			
// 		props.Builder
// 			.AppendAsConstant(")")
// 			.WhiteSpace()
// 			.TrimAppend(entry.ConstraintClauses.ToString())
// 			.WriteMethodBody(props, entry, Array.Empty<(string, string)>());
// 		
// 		return ChainAction.NextMember;
// 	}
//
// 	protected override void ParameterSeparatorAppender(
// 		GeneratorProperties props,
// 		SourceBuilder head,
// 		SourceBuilder body,
// 		int paramIndex) => throw new NotImplementedException();
//
// 	protected override void WriteMethodBody(
// 		GeneratorProperties props,
// 		SourceBuilder body) => throw new NotImplementedException();
//
// 	protected override bool WriteParameter(
// 		GeneratorProperties props,
// 		SourceBuilder head,
// 		SourceBuilder body,
// 		XmlDocumentation xmlDocumentation,
// 		int paramIndex,
// 		int transitionIndex) => throw new NotImplementedException();
// }
