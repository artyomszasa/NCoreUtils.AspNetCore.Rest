namespace NCoreUtils.AspNetCore.Rest;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Kept for backward compatibility.")]
public abstract class RestOperation
{
    public sealed class Create : RestOperation
    {
        private Create() { }
    }

    public sealed class Update : RestOperation
    {
        private Update() { }
    }

    public sealed class Delete : RestOperation
    {
        private Delete() { }
    }

    public sealed class Query : RestOperation
    {
        private Query() { }
    }
}