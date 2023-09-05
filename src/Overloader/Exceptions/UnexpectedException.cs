namespace Overloader.Exceptions;

public static partial class ExceptionExtensions
{
	public static Exception Unexpected(this Exception ex) =>
		new UnexpectedException("Unexpected exception was occurred!", ex);
	
	public static Exception Unexpected(this Exception ex, string message) =>
		new UnexpectedException(message, ex);
}

internal class UnexpectedException : Exception
{
	internal UnexpectedException(string message, Exception innerException)
		: base(message, innerException) { }
}
