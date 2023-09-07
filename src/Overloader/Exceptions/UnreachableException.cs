namespace Overloader.Exceptions;

public static partial class ExceptionExtensions
{
	public static Exception Unreachable(this Exception ex) =>
		new UnreachableException("Unreachable exception was occurred!", ex);

	public static Exception Unreachable(this Exception ex, string message) =>
		new UnreachableException(message, ex);
}

internal class UnreachableException : Exception
{
	internal UnreachableException(string message, Exception innerException)
		: base(message, innerException) { }
}
