using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ConsoleApp
{
    internal class Program
    {
        private const string _url = "https://academia-library.ru";
        private static HttpClient _client;

        public static async Task Main()
        {
            var handler = new HttpClientHandler()
            {
                CookieContainer = new CookieContainer()
            };

            _client = new HttpClient(handler)
            {
                BaseAddress = new Uri(_url)
            };

            var request = new HttpRequestMessage(HttpMethod.Post, @"/inet_order/profile/auth.php?login=yes&backurl=%2Finet_order%2F");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {{"AUTH_FORM", "Y"},
            {"TYPE", "AUTH"},
            {"backurl", @"/inet_order/profile/auth.php?backurl=%2Finet_order%2F"},
            {"USER_LOGIN","???"},
            {"USER_PASSWORD", "???"},
            {"Login", "Войти"}});

            var response = await _client.SendAsync(request);

            for (int i = 1; i <= 251; i++)
            {  
                var imagePath = await GetImagePathAsync("/reader/?id=143477&page="+i.ToString());
                await DownloadImageAsync(imagePath, i.ToString()+".png");
            }
        }

        private static async Task DownloadImageAsync(string imageUrl, string image)
        {
            var response = await _client.GetAsync(imageUrl);
            var fs = new FileStream(image, FileMode.Create);
            await response.Content.CopyToAsync(fs);
        }

        private static async Task<string> GetImagePathAsync(string url)
        {
            var response = await _client.GetAsync(url);
            var html = await response.Content.ReadAsStringAsync();
            var imageElement = new HtmlParser().ParseDocument(html).GetElementById("img_place");
            return imageElement.GetAttribute("src");
        }
    }
}