using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 Iniciando varredura do sitemap index...");
        Console.WriteLine("Digite a URL do sitemap index (ex: https://api.yoursite.com/sitemap.xml):");
        string sitemapIndexUrl = Console.ReadLine()!;
        using var http = new HttpClient();

        // 1. Baixa o sitemap index
        string indexXml = await http.GetStringAsync(sitemapIndexUrl);

        // 2. Extrai todos os sub-sitemaps
        var sitemapMatches = Regex.Matches(indexXml, "<loc>(.*?)</loc>");
        var sitemapUrls = sitemapMatches
            .Select(m => System.Net.WebUtility.HtmlDecode(m.Groups[1].Value))
            .ToList();

        foreach (var sitemapUrl in sitemapUrls)

        {
            Console.WriteLine($"📂 Processando sub-sitemap: {sitemapUrl}");
            string smXml = await http.GetStringAsync(sitemapUrl);

            // 3. Extrai todas as páginas do sub-sitemap
            var pageMatches = Regex.Matches(smXml, "<loc>(.*?)</loc>");
            var pageUrls = pageMatches
                .Select(m => System.Net.WebUtility.HtmlDecode(m.Groups[1].Value))
                .ToList();

            foreach (var rawUrl in pageUrls)
            {
                try
                {
                    await TryRequest(http, rawUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Erro na primeira tentativa: {ex.Message}");
                    await TryWithEscapedUrl(http, rawUrl);
                }
            }
        }

        Console.WriteLine("🏁 Varredura concluída.");
    }

    static async Task TryRequest(HttpClient http, string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)");

        var response = await http.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"✅ OK: {url}");
        }
        else
        {
            throw new Exception($"Status {response.StatusCode}");
        }
    }

    static async Task TryWithEscapedUrl(HttpClient http, string rawUrl)
    {
        try
        {
            var uri = new Uri(rawUrl);

            // Corrige a montagem para não gerar barra extra
            var safeSegments = uri.Segments
                .Select(s => Uri.EscapeDataString(s.Trim('/')))
                .Where(s => !string.IsNullOrEmpty(s));

            string safePath = string.Join("/", safeSegments);
            string safeUrl = $"{uri.Scheme}://{uri.Host}/{safePath}";

            var request = new HttpRequestMessage(HttpMethod.Get, safeUrl);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)");

            var response = await http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"🔄 OK (com EscapeDataString): {safeUrl}");
            }
            else
            {
                Console.WriteLine($"❌ Falha {response.StatusCode} mesmo com EscapeDataString: {safeUrl}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Erro ao acessar com EscapeDataString: {ex.Message}");
        }
    }
}

