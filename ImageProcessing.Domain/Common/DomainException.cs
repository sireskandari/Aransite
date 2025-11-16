namespace ImageProcessing.Domain.Common;

/// <summary>
/// A minimal exception to represent domain rule violations.
/// Throw this instead of ArgumentException when a business invariant is broken.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
