namespace NCoreUtils.Rest
{
    public class RestClientConfiguration : IRestClientConfiguration
    {
        public const string DefaultHttpClient = "NCoreUtils.Rest";

        public string HttpClient { get; set; } = DefaultHttpClient;

        public string Endpoint { get; set; } = string.Empty;
    }
}