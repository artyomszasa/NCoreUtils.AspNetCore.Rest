namespace NCoreUtils.AspNetCore.Rest
{
    public abstract class RestOperation
    {
        public sealed class Create : RestOperation
        {
            Create() { }
        }

        public sealed class Update : RestOperation
        {
            Update() { }
        }

        public sealed class Delete : RestOperation
        {
            Delete() { }
        }

        public sealed class Query : RestOperation
        {
            Query() { }
        }
    }
}