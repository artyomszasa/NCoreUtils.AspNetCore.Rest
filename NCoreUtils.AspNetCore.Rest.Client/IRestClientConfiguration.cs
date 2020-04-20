namespace NCoreUtils.Rest
{
    public interface IRestClientConfiguration
    {
        string HttpClient { get; }

        string Endpoint { get; }
    }
}