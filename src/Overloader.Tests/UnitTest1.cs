using Overloader;
using TestProject.Utils;

namespace TestProject;

public class Tests
{
	[SetUp]
	public void Setup() { }

	[Test]
	public void Test1()
	{
		var tester = new CSharpSourceGeneratorVerifier<OverloadsGenerator>.Test();
	}
}
