using System;

namespace NCoreUtils.AspNetCore.Rest.Internal;

public class ShouldNeverHappenException : InvalidOperationException
{
    public ShouldNeverHappenException()
        : base("Should never happen.")
    { }

    public ShouldNeverHappenException(string message)
        : base(message)
    { }

    public ShouldNeverHappenException(string message, Exception innerException)
        : base(message, innerException)
    { }
}