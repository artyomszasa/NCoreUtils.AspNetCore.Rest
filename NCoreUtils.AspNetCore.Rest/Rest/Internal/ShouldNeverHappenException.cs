using System;

namespace NCoreUtils.AspNetCore.Rest.Internal;

public class ShouldNeverHappenException()
    : InvalidOperationException("Should never happen.")
{ }