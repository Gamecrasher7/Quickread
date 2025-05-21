using HtmlAgilityPack;
using QuickRead.Models;
using System.Net.Http;

namespace QuickRead.Sources
{
    public class MangaparkService : ISourceService
    {
        public string Name => "MangaPark";
        private readonly HttpClient _http = new();

        public async Task<List<Manga>> GetLatestMangaAsync()
        {
            var list = new List<Manga>();
            var html = await _http.GetStringAsync("https://mangapark.io");

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var items = doc.DocumentNode.SelectNodes("//div[contains(@class, 'item')]//a[contains(@href, '/title/')]");
            if (items == null) return list;

            foreach (var a in items.DistinctBy(a => a.GetAttributeValue("href", "")))
            {
                var title = a.InnerText.Trim();
                var link = "https://mangapark.io" + a.GetAttributeValue("href", "");
                list.Add(new Manga
                {
                    Title = title,
                    Url = link,
                    Source = Name
                });
            }

            return list;
        }

        public async Task<List<Manga>> SearchMangaAsync(string query)
        {
            var list = new List<Manga>();
            var html = await _http.GetStringAsync($"https://mangapark.io/search?word={Uri.EscapeDataString(query)}");

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var items = doc.DocumentNode.SelectNodes("//div[contains(@class, 'item')]//a[contains(@href, '/title/')]");
            if (items == null) return list;

            foreach (var a in items.DistinctBy(a => a.GetAttributeValue("href", "")))
            {
                var title = a.InnerText.Trim();
                var link = "https://mangapark.io" + a.GetAttributeValue("href", "");
                list.Add(new Manga
                {
                    Title = title,
                    Url = link,
                    Source = Name
                });
            }

            return list;
        }

        public async Task<List<Chapter>> GetChaptersAsync(Manga manga)
        {
            var chapters = new List<Chapter>();
            var html = await _http.GetStringAsync(manga.Url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var nodes = doc.DocumentNode.SelectNodes("//a[contains(@href, '/chapter/')]");
            if (nodes == null) return chapters;

            foreach (var node in nodes.DistinctBy(n => n.GetAttributeValue("href", "")))
            {
                chapters.Add(new Chapter
                {
                    Title = node.InnerText.Trim(),
                    Url = "https://mangapark.io" + node.GetAttributeValue("href", ""),
                    Language = "en"
                });
            }

            return chapters;
        }

        public async Task<List<string>> GetPageImagesAsync(Chapter chapter)
        {
            var pageUrls = new List<string>();
            var html = await _http.GetStringAsync(chapter.Url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var imgs = doc.DocumentNode.SelectNodes("//img[contains(@src, '/mangapark')]");
            if (imgs == null) return pageUrls;

            foreach (var img in imgs)
            {
                var src = img.GetAttributeValue("src", "");
                if (!string.IsNullOrEmpty(src))
                {
                    pageUrls.Add(src);
                }
            }

            return pageUrls;
        }
    }
}
