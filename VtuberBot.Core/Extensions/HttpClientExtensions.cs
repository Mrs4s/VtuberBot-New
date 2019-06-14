using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VtuberBot.Core.Extensions
{
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> PostFormAsync(this HttpClient @this, string url, object obj)
        {
            var p = obj.GetType().GetRuntimeFields().Select(v =>
                new KeyValuePair<string, string>(v.Name.Split('<').Last().Split('>').First(),
                    v.GetValue(obj).ToString()));
            return await @this.PostAsync(url, new FormUrlEncodedContent(p));
        }

        public static async Task<HttpResponseMessage> PostJsonAsync(this HttpClient @this, string url, object obj)
        {
            try
            {
                var json = JsonConvert.SerializeObject(obj);
                return await @this.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            }
            catch
            {
                return null;
            }

        }

        public static HttpClient CreateClient(string userAgent = null, string referer = null, string cookies = null, string cookiesDomain = null, bool useGZip = false, IWebProxy proxy = null)
        {
            var handler = new HttpClientHandler()
            { AutomaticDecompression = useGZip ? DecompressionMethods.GZip : DecompressionMethods.None };
            if (cookies != null)
                handler.CookieContainer = cookies.ToCookieContainer(cookiesDomain);
            if (proxy != null)
                handler.Proxy = proxy;
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent",
                userAgent ??
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36");
            if (referer != null)
                client.DefaultRequestHeaders.Add("Referer", referer);
            return client;
        }

        public static async Task<string> UploadImageAsync(this HttpClient @this, byte[] image)
        {
            var formData = new MultipartFormDataContent(GetBoundary())
            {
                {new ByteArrayContent(image), "smfile", "image.jpg"}
            };
            var result = await @this.PostAsync("https://sm.ms/api/upload", formData);
            var json = JToken.Parse(await result.Content.ReadAsStringAsync());
            var a = json.ToString();
            return json["data"]?["url"]?.ToString();
        }
        private static string GetBoundary()
        {
            var rand = new Random();
            var sb = new StringBuilder();
            for (var i = 0; i < 28; i++) sb.Append('-');
            for (var i = 0; i < 15; i++) sb.Append((char)(rand.Next(0, 26) + 'a'));
            var boundary = sb.ToString();
            return boundary;
        }
    }
}
