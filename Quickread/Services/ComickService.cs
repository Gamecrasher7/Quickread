using HtmlAgilityPack;
using QuickRead.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuickRead.Sources
{
    public class ComickService : ISourceService
    {
        public string Name => "Comick";

        private readonly HttpClient _http;

        public ComickService()
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<List<Manga>> GetLatestMangaAsync()
        {
            var mangas = new List<Manga>();

            try
            {
                var html = await _http.GetStringAsync("https://comick.io/home2");
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var nodes = doc.DocumentNode.SelectNodes("//a[contains(@href, '/comic/')]");
                if (nodes == null) return mangas;

                foreach (var node in nodes.DistinctBy(n => n.GetAttributeValue("href", "")))
                {
                    var href = node.GetAttributeValue("href", "");
                    if (string.IsNullOrEmpty(href)) continue;

                    var url = href.StartsWith("http") ? href : "https://comick.io" + href;
                    var title = node.GetAttributeValue("title", "") ?? node.InnerText.Trim();
                    var imgNode = node.SelectSingleNode(".//img");
                    var cover = imgNode?.GetAttributeValue("src", "") ?? "";

                    if (!string.IsNullOrEmpty(title))
                    {
                        mangas.Add(new Manga
                        {
                            Title = title,
                            Url = url,
                            CoverImageUrl = cover,
                            Source = Name
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error or handle appropriately
                Console.WriteLine($"Error in GetLatestMangaAsync: {ex.Message}");
            }

            return mangas;
        }

        public async Task<List<Manga>> SearchMangaAsync(string query)
        {
            var mangas = new List<Manga>();

            try
            {
                var url = $"https://comick.io/search?q={Uri.EscapeDataString(query)}";
                var html = await _http.GetStringAsync(url);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var nodes = doc.DocumentNode.SelectNodes("//a[contains(@href, '/comic/')]");
                if (nodes == null) return mangas;

                foreach (var node in nodes.DistinctBy(n => n.GetAttributeValue("href", "")))
                {
                    var href = node.GetAttributeValue("href", "");
                    if (string.IsNullOrEmpty(href)) continue;

                    var link = href.StartsWith("http") ? href : "https://comick.io" + href;
                    var title = node.GetAttributeValue("title", "") ?? node.InnerText.Trim();
                    var imgNode = node.SelectSingleNode(".//img");
                    var cover = imgNode?.GetAttributeValue("src", "") ?? "";

                    if (!string.IsNullOrEmpty(title))
                    {
                        mangas.Add(new Manga
                        {
                            Title = title,
                            Url = link,
                            CoverImageUrl = cover,
                            Source = Name
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchMangaAsync: {ex.Message}");
            }

            return mangas;
        }

        public async Task<List<Chapter>> GetChaptersAsync(Manga manga)
        {
            var chapters = new List<Chapter>();

            try
            {
                var html = await _http.GetStringAsync(manga.Url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var scriptNode = doc.DocumentNode.SelectSingleNode("//script[contains(text(), 'window.__DATA__')]");
                if (scriptNode != null)
                {
                    var scriptText = scriptNode.InnerText;
                    var startIndex = scriptText.IndexOf("window.__DATA__ = ");
                    if (startIndex >= 0)
                    {
                        startIndex += "window.__DATA__ = ".Length;
                        var endIndex = scriptText.IndexOf("};", startIndex);
                        if (endIndex > startIndex)
                        {
                            endIndex += 1;
                            var jsonString = scriptText.Substring(startIndex, endIndex - startIndex);

                            using var docJson = JsonDocument.Parse(jsonString);
                            var root = docJson.RootElement;

                            if (root.TryGetProperty("chapter", out var chapterData))
                            {
                                foreach (var item in chapterData.EnumerateArray())
                                {
                                    if (item.TryGetProperty("hid", out var hid) &&
                                        item.TryGetProperty("title", out var title))
                                    {
                                        var lang = item.TryGetProperty("lang", out var langProp) ?
                                                  langProp.GetString() ?? "en" : "en";

                                        chapters.Add(new Chapter
                                        {
                                            Title = title.GetString() ?? "No Title",
                                            Url = $"https://comick.io/chapter/{hid.GetString()}",
                                            Language = lang
                                        });
                                    }
                                }
                            }
                        }
                    }
                }

                // Fallback: try to parse chapter links directly
                if (!chapters.Any())
                {
                    var chapterLinks = doc.DocumentNode.SelectNodes("//a[contains(@href, '/chapter/')]");
                    if (chapterLinks != null)
                    {
                        foreach (var link in chapterLinks.DistinctBy(l => l.GetAttributeValue("href", "")))
                        {
                            var href = link.GetAttributeValue("href", "");
                            var title = link.InnerText.Trim();

                            if (!string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(title))
                            {
                                chapters.Add(new Chapter
                                {
                                    Title = title,
                                    Url = href.StartsWith("http") ? href : "https://comick.io" + href,
                                    Language = "en"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetChaptersAsync: {ex.Message}");
            }

            return chapters.OrderBy(c => c.Title).ToList();
        }

        public async Task<List<string>> GetPageImagesAsync(Chapter chapter)
        {
            var pages = new List<string>();

            try
            {
                var html = await _http.GetStringAsync(chapter.Url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var scriptNode = doc.DocumentNode.SelectSingleNode("//script[contains(text(), 'window.__scans__')]");
                if (scriptNode != null)
                {
                    var scriptText = scriptNode.InnerText;
                    var startIndex = scriptText.IndexOf("window.__scans__ = ");
                    if (startIndex >= 0)
                    {
                        startIndex += "window.__scans__ = ".Length;
                        var endIndex = scriptText.IndexOf("};", startIndex);
                        if (endIndex > startIndex)
                        {
                            endIndex += 1;
                            var jsonString = scriptText.Substring(startIndex, endIndex - startIndex);

                            using var json = JsonDocument.Parse(jsonString);
                            var root = json.RootElement;

                            if (root.TryGetProperty("images", out var images))
                            {
                                foreach (var page in images.EnumerateArray())
                                {
                                    if (page.TryGetProperty("url", out var url))
                                    {
                                        var imageUrl = url.GetString();
                                        if (!string.IsNullOrEmpty(imageUrl))
                                        {
                                            pages.Add(imageUrl.StartsWith("http") ? imageUrl :
                                                     "https://meo.comick.pictures" + imageUrl);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Fallback: look for img tags
                if (!pages.Any())
                {
                    var imgNodes = doc.DocumentNode.SelectNodes("//img[contains(@src, 'comick') or contains(@src, 'meo')]");
                    if (imgNodes != null)
                    {
                        foreach (var img in imgNodes)
                        {
                            var src = img.GetAttributeValue("src", "");
                            if (!string.IsNullOrEmpty(src) && (src.Contains("comick") || src.Contains("meo")))
                            {
                                pages.Add(src);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPageImagesAsync: {ex.Message}");
            }

            return pages;
        }

        public void Dispose()
        {
            _http?.Dispose();
        }
    }
}