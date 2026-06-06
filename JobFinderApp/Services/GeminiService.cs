using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JobFinderApp.Services
{
    public class GeminiService
    {
        private readonly string apiKey = "AIzaSyCoEX6qCIOPSHJiSWNHY-FBvUErvfA0QXM";

        public async Task<string> Generate(string prompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=AIzaSyCoEX6qCIOPSHJiSWNHY-FBvUErvfA0QXM";

            var body = new
            {
                contents = new[]
                {
            new
            {
                parts = new[]
                {
                    new { text = prompt }
                }
            }
        }
            };

            using (var client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                
                var response = await client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return error; // 🔥 return error to debug
                }

                return await response.Content.ReadAsStringAsync();
             
            }
        }
    }
}