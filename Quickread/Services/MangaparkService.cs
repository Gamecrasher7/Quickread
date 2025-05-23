using HtmlAgilityPack;
using QuickRead.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace QuickRead.Sources
{
    public class MangaparkService : ISourceService
    {
        public string Name => "MangaPark";
        private readonly HttpClient _http;

        public MangaparkService()
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<List<Manga>> GetLatestMangaAsync()
        {
            var list = new List<Manga>();

            try
            {
                var html = await _http.GetStringAsync("https://mangapark.net");
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Try multiple selectors for manga items
                var items = doc.DocumentNode.SelectNodes("//div[contains(@class, 'item')]//a[contains(@href, '/title/')]") ??
                           doc.DocumentNode.SelectNodes("//a[contains(@href, '/title/')]") ??
                           doc.DocumentNode.SelectNodes("//a[contains(@href, '/manga/')]");

                if (items != null)
                {
                    foreach (var a in items.DistinctBy(a => a.GetAttributeValue("href", "")).Take(20))
                    {
                        var href = a.GetAttributeValue("href", "");
                        var title = a.GetAttributeValue("title", "") ?? a.InnerText.Trim();

                        if (!string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(title))
                        {
                            var link = href.StartsWith("http") ? href : "https://mangapark.net" + href;
                            var imgNode = a.SelectSingleNode(".//img");
                            var cover = imgNode?.GetAttributeValue("src", "") ?? "";

                            list.Add(new Manga
                            {
                                Title = title,
                                Url = link,
                                CoverImageUrl = cover,
                                Source = Name
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetLatestMangaAsync: {ex.Message}");
            }

            return list;
        }

        public async Task<List<Manga>> SearchMangaAsync(string query)
        {
            var list = new List<Manga>();

            try
            {
                var searchUrl = $"https://mangapark.net/search?word={Uri.EscapeDataString(query)}";
                var html = await _http.GetStringAsync(searchUrl);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Try multiple selectors for search results
                var items = doc.DocumentNode.SelectNodes("//div[contains(@class, 'item')]//a[contains(@href, '/title/')]") ??
                           doc.DocumentNode.SelectNodes("//a[contains(@href, '/title/')]") ??
                           doc.DocumentNode.SelectNodes("//a[contains(@href, '/manga/')]");

                if (items != null)
                {
                    foreach (var a in items.DistinctBy(a => a.GetAttributeValue("href", "")))
                    {
                        var href = a.GetAttributeValue("href", "");
                        var title = a.GetAttributeValue("title", "") ?? a.InnerText.Trim();

                        if (!string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(title))
                        {
                            var link = href.StartsWith("http") ? href : "https://mangapark.net" + href;
                            var imgNode = a.SelectSingleNode(".//img");
                            var cover = imgNode?.GetAttributeValue("src", "") ?? "";

                            list.Add(new Manga
                            {
                                Title = title,
                                Url = link,
                                CoverImageUrl = cover,
                                Source = Name
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchMangaAsync: {ex.Message}");
            }

            return list;
        }

        public async Task<List<Chapter>> GetChaptersAsync(Manga manga)
        {
            var chapters = new List<Chapter>();

            try
            {
                var html = await _http.GetStringAsync(manga.Url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Try multiple selectors for chapter links
                var nodes = doc.DocumentNode.SelectNodes("//a[contains(@href, '/chapter/')]") ??
                           doc.DocumentNode.SelectNodes("//a[contains(@href, '/read/')]");

                if (nodes != null)
                {
                    foreach (var node in nodes.DistinctBy(n => n.GetAttributeValue("href", "")))
                    {
                        var href = node.GetAttributeValue("href", "");
                        var title = node.InnerText.Trim();

                        if (!string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(title))
                        {
                            chapters.Add(new Chapter
                            {
                                Title = title,
                                Url = href.StartsWith("http") ? href : "https://mangapark.net" + href,
                                Language = "en"
                            });
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
            var pageUrls = new List<string>();

            try
            {
                var html = await _http.GetStringAsync(chapter.Url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Try multiple selectors for manga images
                var imgs = doc.DocumentNode.SelectNodes("//img[contains(@src, 'mangapark') or contains(@src, 'mpark')]") ??
                          doc.DocumentNode.SelectNodes("//img[contains(@class, 'img-fluid')]") ??
                          doc.DocumentNode.SelectNodes("//div[contains(@class, 'reader')]//img");

                if (imgs != null)
                {
                    foreach (var img in imgs)
                    {
                        var src = img.GetAttributeValue("src", "") ?? img.GetAttributeValue("data-src", "");
                        if (!string.IsNullOrEmpty(src) &&
                            (src.Contains("mangapark") || src.Contains("mpark") || src.Contains("http")))
                        {
                            pageUrls.Add(src);
                        }
                    }
                }

                // Alternative: look for script with image data
                if (!pageUrls.Any())
                {
                    var scripts = doc.DocumentNode.SelectNodes("//script[contains(text(), 'img') or contains(text(), 'image')]");
                    if (scripts != null)
                    {
                        foreach (var script in scripts)
                        {
                            var scriptText = script.InnerText;
                            // Look for image URLs in JavaScript
                            var lines = scriptText.Split('\n');
                            foreach (var line in lines)
                            {
                                if (line.Contains("http") && (line.Contains(".jpg") || line.Contains(".png") || line.Contains(".webp")))
                                {
                                    // Extract URL from the line
                                    var start = line.IndexOf("http");
                                    if (start >= 0)
                                    {
                                        var end = line.IndexOfAny(new char[] { '"', '\'', ' ', ',' }, start);
                                        if (end > start)
                                        {
                                            var url = line.Substring(start, end - start);
                                            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                                            {
                                                pageUrls.Add(url);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPageImagesAsync: {ex.Message}");
            }

            return pageUrls.Distinct().ToList();
        }

        public void Dispose()
        {
            _http?.Dispose();
        }
    }
}