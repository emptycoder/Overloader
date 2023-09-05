using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Utils;

namespace Overloader.Chains.Overloads.Overloads;

public abstract class ArrowMethodOverloader : MethodOverloader
{
	protected sealed override void WriteMethodBody(
		GeneratorProperties props,
		SourceBuilder body) =>
		props.Builder
			.AppendAsConstant("=>", 1)
			.NestedIncrease()
			.AppendRefReturnValues(props.Store.MethodSyntax.ReturnType)
			.Append(props.Store.MethodSyntax.Identifier.ToString())
			.AppendAsConstant("(")
			.AppendAndClear(body)
			.AppendAsConstant(");", 1)
			.NestedDecrease();
}
