namespace Overloader.Exceptions;

public static partial class ExceptionExtensions
{
	public static Exception NotExpected(this Exception ex) =>
		new NotExpectedException("Unreachable exception was occurred!", ex);

	public static Exception NotExpected(this Exception ex, string message) =>
		new NotExpectedException(message, ex);
}

internal class NotExpectedException : Exception
{
	internal NotExpectedException(string message, Exception innerException)
		: base(message, innerException) { }
}
