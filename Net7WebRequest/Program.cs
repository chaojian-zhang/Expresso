using System.Net;

namespace Net7WebRequest
{
    public enum SupportedMethod
    {
        GET,
        POST
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            Dictionary<string, string> parameters = ParseParameters(args);
            string url = parameters["url"];
            SupportedMethod method = Enum.Parse<SupportedMethod>(parameters["method"]);
            string requestContent = parameters["content"];

            try
            {
                string responseBody = string.Empty;
                switch (method)
                {
                    case SupportedMethod.GET:
                        responseBody = await MakeGETRequest(url);
                        break;
                    case SupportedMethod.POST:
                        responseBody = await MakePOSTRequest(url, requestContent);
                        break;
                    default:
                        break;
                }
                Console.WriteLine(responseBody);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.Message);
            }

            static async Task<string> MakeGETRequest(string url)
            {
                using HttpClient client = new();
                return await client.GetStringAsync(url);
            }
            static async Task<string> MakePOSTRequest(string url, string payload)
            {
                using HttpClient client = new();
                using HttpResponseMessage response = await client.PostAsync(url, new StringContent(payload));
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }
        private static Dictionary<string, string> ParseParameters(string[] args)
        {
            // Format: --url=http://127.0.0.1:8000
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            foreach (string param in args)
            {
                var pair = param.Split('=');
                string key = pair[0].TrimStart('-');
                string value = pair[1];
                parameters[key] = value;
            }
            return parameters;
        }
    }
}