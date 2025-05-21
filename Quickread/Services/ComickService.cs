using HtmlAgilityPack;
using QuickRead.Models;
using System.Net.Http;
using System.Text.Json;

namespace QuickRead.Sources
{
    public class ComickService : ISourceService
    {
        public string Name => "Comick";

        private readonly HttpClient _http = new();

        public async Task<List<Manga>> GetLatestMangaAsync()
        {
            var mangas = new List<Manga>();
            var html = await _http.GetStringAsync("https://comick.io/home2");

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var nodes = doc.DocumentNode.SelectNodes("//a[contains(@href, '/comic/')]");
            if (nodes == null) return mangas;

            foreach (var node in nodes.DistinctBy(n => n.GetAttributeValue("href", "")))
            {
                var url = "https://comick.io" + node.GetAttributeValue("href", "");
                var title = node.GetAttributeValue("title", "Unknown");
                var imgNode = node.SelectSingleNode(".//img");
                var cover = imgNode?.GetAttributeValue("src", "");

                mangas.Add(new Manga
                {
                    Title = title,
                    Url = url,
                    CoverImageUrl = cover,
                    Source = Name
                });
            }

            return mangas;
        }

        public async Task<List<Manga>> SearchMangaAsync(string query)
        {
            var mangas = new List<Manga>();
            var url = $"https://comick.io/search?q={Uri.EscapeDataString(query)}";
            var html = await _http.GetStringAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var nodes = doc.DocumentNode.SelectNodes("//a[contains(@href, '/comic/')]");
            if (nodes == null) return mangas;

            foreach (var node in nodes.DistinctBy(n => n.GetAttributeValue("href", "")))
            {
                var link = "https://comick.io" + node.GetAttributeValue("href", "");
                var title = node.GetAttributeValue("title", "Unknown");
                var imgNode = node.SelectSingleNode(".//img");
                var cover = imgNode?.GetAttributeValue("src", "");

                mangas.Add(new Manga
                {
                    Title = title,
                    Url = link,
                    CoverImageUrl = cover,
                    Source = Name
                });
            }

            return mangas;
        }

        public async Task<List<Chapter>> GetChaptersAsync(Manga manga)
        {
            var chapters = new List<Chapter>();

            var html = await _http.GetStringAsync(manga.Url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var scriptNode = doc.DocumentNode.SelectSingleNode("//script[contains(text(), 'window.__DATA__')]");
            if (scriptNode == null) return chapters;

            var scriptText = scriptNode.InnerText;
            var startIndex = scriptText.IndexOf("window.__DATA__ = ") + "window.__DATA__ = ".Length;
            var endIndex = scriptText.IndexOf("};", startIndex) + 1;
            var jsonString = scriptText.Substring(startIndex, endIndex - startIndex);

            using var docJson = JsonDocument.Parse(jsonString);
            var root = docJson.RootElement;

            if (root.TryGetProperty("chapter", out var chapterData))
            {
                foreach (var item in chapterData.EnumerateArray())
                {
                    if (item.TryGetProperty("hid", out var hid) &&
                        item.TryGetProperty("title", out var title) &&
                        item.TryGetProperty("lang", out var lang))
                    {
                        chapters.Add(new Chapter
                        {
                            Title = title.GetString() ?? "No Title",
                            Url = $"https://comick.io/chapter/{hid.GetString()}",
                            Language = lang.GetString() ?? "unknown"
                        });
                    }
                }
            }

            return chapters;
        }

        public async Task<List<string>> GetPageImagesAsync(Chapter chapter)
        {
            var html = await _http.GetStringAsync(chapter.Url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var scriptNode = doc.DocumentNode.SelectSingleNode("//script[contains(text(), 'window.__scans__')]");
            if (scriptNode == null) return new List<string>();

            var scriptText = scriptNode.InnerText;
            var startIndex = scriptText.IndexOf("window.__scans__ = ") + "window.__scans__ = ".Length;
            var endIndex = scriptText.IndexOf("};", startIndex) + 1;
            var jsonString = scriptText.Substring(startIndex, endIndex - startIndex);

            using var json = JsonDocument.Parse(jsonString);
            var root = json.RootElement;

            var pages = new List<string>();

            if (root.TryGetProperty("images", out var images))
            {
                foreach (var page in images.EnumerateArray())
                {
                    if (page.TryGetProperty("url", out var url))
                        pages.Add("https://meo.comick.pictures" + url.GetString());
                }
            }

            return pages;
        }
    }
}
