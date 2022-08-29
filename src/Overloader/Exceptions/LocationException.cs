using Microsoft.CodeAnalysis;

namespace Overloader.Exceptions;

internal static class ExceptionExtensions
{
	public static Exception WithLocation(this Exception ex, Location location) =>
		new LocationException("Expected exception occured.", ex, location);
}

internal class LocationException : Exception
{
	public readonly Location Location;

	internal LocationException(string message, Exception innerException, Location location) :
		base(message, innerException) => Location = location;
}
