namespace NCoreUtils.AspNetCore.Rest.Internal;

public static class AccessStatusValidatorResultExtensions
{
    public static void ThrowOnFailure(this in AccessStatusValidatorResult result)
    {
        if (!result.Success)
        {
            throw new AccessValidationFailedException(result.StatusCode, result.Message);
        }
    }
}