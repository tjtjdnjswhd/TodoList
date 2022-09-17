namespace TodoList.Client.Extensions
{
    public static class HttpClientExtension
    {
        public static HttpClient SetHeaderAuthorization(this HttpClient httpClient, string scheme, string code)
        {
            httpClient.DefaultRequestHeaders.Authorization = new(scheme, code);
            return httpClient;
        }
    }
}
