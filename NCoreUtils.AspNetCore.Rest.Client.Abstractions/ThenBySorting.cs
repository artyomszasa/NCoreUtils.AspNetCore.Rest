namespace NCoreUtils.Rest;

public readonly struct ThenBySorting(string by, string direction)
{
    public string By { get; } = by;

    public string Direction { get; } = direction;
}