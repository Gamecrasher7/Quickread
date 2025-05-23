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
        private const string API_BASE = "https://api.comick.fun";
        private const string BASE_URL = "https://comick.io";

        public ComickService()
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _http.DefaultRequestHeaders.Add("Referer", $"{BASE_URL}/");
            _http.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<List<Manga>> GetLatestMangaAsync()
        {
            var mangas = new List<Manga>();

            try
            {
                var url = $"{API_BASE}/v1.0/search?sort=uploaded&page=1&limit=20";
                Console.WriteLine($"Fetching latest manga from: {url}");

                var response = await _http.GetStringAsync(url);
                Console.WriteLine($"Response length: {response.Length}");

                // Debug: Print first 500 characters of response
                Console.WriteLine($"Response preview: {response.Substring(0, Math.Min(500, response.Length))}");

                mangas = ParseMangaListFromJson(response);
                Console.WriteLine($"Parsed {mangas.Count} manga items");
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Error in GetLatestMangaAsync: {httpEx.Message}");
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON Error in GetLatestMangaAsync: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error in GetLatestMangaAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return mangas;
        }

        public async Task<List<Manga>> SearchMangaAsync(string query)
        {
            var mangas = new List<Manga>();

            try
            {
                var encodedQuery = Uri.EscapeDataString(query);
                var url = $"{API_BASE}/v1.0/search?q={encodedQuery}&limit=20";

                Console.WriteLine($"Searching with URL: {url}");

                var response = await _http.GetStringAsync(url);
                Console.WriteLine($"Search response length: {response.Length}");

                // Debug: Print response structure
                Console.WriteLine($"Search response preview: {response.Substring(0, Math.Min(300, response.Length))}");

                mangas = ParseMangaListFromJson(response);
                Console.WriteLine($"Found {mangas.Count} mangas");
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Error in SearchMangaAsync: {httpEx.Message}");
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON Error in SearchMangaAsync: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error in SearchMangaAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return mangas;
        }

        private List<Manga> ParseMangaListFromJson(string jsonResponse)
        {
            var mangas = new List<Manga>();

            try
            {
                if (string.IsNullOrWhiteSpace(jsonResponse))
                {
                    Console.WriteLine("Empty JSON response received");
                    return mangas;
                }

                var options = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                };

                using var json = JsonDocument.Parse(jsonResponse, options);
                var root = json.RootElement;

                Console.WriteLine($"JSON ValueKind: {root.ValueKind}");

                // Handle different response structures
                if (root.ValueKind == JsonValueKind.Array)
                {
                    Console.WriteLine("Processing direct array response");
                    foreach (var item in root.EnumerateArray())
                    {
                        var manga = ParseSingleMangaFromJson(item);
                        if (manga != null)
                        {
                            mangas.Add(manga);
                            Console.WriteLine($"Added manga: {manga.Title}");
                        }
                    }
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    Console.WriteLine("Processing object response, looking for data array");

                    // Try different possible property names
                    string[] possibleArrayProps = { "data", "results", "comics", "manga" };

                    foreach (var propName in possibleArrayProps)
                    {
                        if (root.TryGetProperty(propName, out var arrayProp) && arrayProp.ValueKind == JsonValueKind.Array)
                        {
                            Console.WriteLine($"Found array in property: {propName}");
                            foreach (var item in arrayProp.EnumerateArray())
                            {
                                var manga = ParseSingleMangaFromJson(item);
                                if (manga != null)
                                {
                                    mangas.Add(manga);
                                    Console.WriteLine($"Added manga: {manga.Title}");
                                }
                            }
                            break;
                        }
                    }

                    // If no array found, check if the root object itself is a manga
                    if (mangas.Count == 0)
                    {
                        Console.WriteLine("Trying to parse root object as single manga");
                        var manga = ParseSingleMangaFromJson(root);
                        if (manga != null)
                        {
                            mangas.Add(manga);
                        }
                    }
                }

                Console.WriteLine($"Successfully parsed {mangas.Count} manga items");
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON parsing error: {jsonEx.Message}");
                Console.WriteLine($"JSON Response: {jsonResponse.Substring(0, Math.Min(1000, jsonResponse.Length))}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing manga list: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return mangas;
        }

        private Manga ParseSingleMangaFromJson(JsonElement item)
        {
            try
            {
                Console.WriteLine($"Parsing manga item, ValueKind: {item.ValueKind}");

                if (item.ValueKind != JsonValueKind.Object)
                {
                    Console.WriteLine("Item is not an object, skipping");
                    return null;
                }

                // Debug: Print available properties
                Console.WriteLine("Available properties:");
                foreach (var prop in item.EnumerateObject())
                {
                    Console.WriteLine($"  - {prop.Name}: {prop.Value.ValueKind}");
                }

                // Try different possible property names for title
                string title = null;
                string[] titleProps = { "title", "name", "en", "slug" };
                foreach (var prop in titleProps)
                {
                    if (item.TryGetProperty(prop, out var titleProp))
                    {
                        if (titleProp.ValueKind == JsonValueKind.String)
                        {
                            title = titleProp.GetString();
                            Console.WriteLine($"Found title in '{prop}': {title}");
                            break;
                        }
                        else if (titleProp.ValueKind == JsonValueKind.Object && prop == "en")
                        {
                            // Handle nested title structure
                            if (titleProp.TryGetProperty("title", out var nestedTitle))
                            {
                                title = nestedTitle.GetString();
                                Console.WriteLine($"Found nested title: {title}");
                                break;
                            }
                        }
                    }
                }

                // Try different possible property names for ID/HID
                string hid = null;
                string[] idProps = { "hid", "id", "slug", "comic_id" };
                foreach (var prop in idProps)
                {
                    if (item.TryGetProperty(prop, out var idProp))
                    {
                        if (idProp.ValueKind == JsonValueKind.String)
                        {
                            hid = idProp.GetString();
                            Console.WriteLine($"Found ID in '{prop}': {hid}");
                            break;
                        }
                        else if (idProp.ValueKind == JsonValueKind.Number)
                        {
                            hid = idProp.GetInt32().ToString();
                            Console.WriteLine($"Found numeric ID in '{prop}': {hid}");
                            break;
                        }
                    }
                }

                // Try different possible property names for cover
                string coverUrl = "";
                string[] coverProps = { "cover_url", "cover", "thumbnail", "image", "poster" };
                foreach (var prop in coverProps)
                {
                    if (item.TryGetProperty(prop, out var coverProp) && coverProp.ValueKind == JsonValueKind.String)
                    {
                        coverUrl = coverProp.GetString() ?? "";
                        Console.WriteLine($"Found cover in '{prop}': {coverUrl}");
                        break;
                    }
                }

                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(hid))
                {
                    Console.WriteLine($"Missing required data - Title: '{title}', HID: '{hid}'");
                    return null;
                }

                var manga = new Manga
                {
                    Title = title,
                    Url = $"{BASE_URL}/comic/{hid}",
                    CoverImageUrl = coverUrl,
                    Source = Name
                };

                Console.WriteLine($"Successfully created manga: {manga.Title}");
                return manga;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing single manga: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<List<Chapter>> GetChaptersAsync(Manga manga)
        {
            var chapters = new List<Chapter>();

            try
            {
                var hid = ExtractHidFromUrl(manga.Url);
                if (string.IsNullOrEmpty(hid))
                {
                    Console.WriteLine("Could not extract HID from URL: " + manga.Url);
                    return chapters;
                }

                Console.WriteLine($"Getting chapters for HID: {hid}");

                var chaptersUrl = $"{API_BASE}/comic/{hid}/chapters?page=1&limit=100";
                Console.WriteLine($"Fetching chapters from: {chaptersUrl}");

                var chaptersResponse = await _http.GetStringAsync(chaptersUrl);
                Console.WriteLine($"Chapters response length: {chaptersResponse.Length}");

                // Debug: Print response structure
                Console.WriteLine($"Chapters response preview: {chaptersResponse.Substring(0, Math.Min(500, chaptersResponse.Length))}");

                using var chaptersJson = JsonDocument.Parse(chaptersResponse);
                var root = chaptersJson.RootElement;

                Console.WriteLine($"Chapters JSON ValueKind: {root.ValueKind}");

                // Debug: Print available properties
                if (root.ValueKind == JsonValueKind.Object)
                {
                    Console.WriteLine("Available properties in chapters response:");
                    foreach (var prop in root.EnumerateObject())
                    {
                        Console.WriteLine($"  - {prop.Name}: {prop.Value.ValueKind}");
                    }
                }

                // Try different possible property names for chapters array
                string[] chapterProps = { "chapters", "data", "results", "chapter_list" };
                JsonElement chaptersArray = default;
                bool foundArray = false;

                foreach (var propName in chapterProps)
                {
                    if (root.TryGetProperty(propName, out chaptersArray) && chaptersArray.ValueKind == JsonValueKind.Array)
                    {
                        Console.WriteLine($"Found chapters array in property: {propName} with {chaptersArray.GetArrayLength()} items");
                        foundArray = true;
                        break;
                    }
                }

                if (!foundArray && root.ValueKind == JsonValueKind.Array)
                {
                    Console.WriteLine("Root is array, using as chapters");
                    chaptersArray = root;
                    foundArray = true;
                }

                if (foundArray)
                {
                    foreach (var chapter in chaptersArray.EnumerateArray())
                    {
                        var chapterObj = ParseChapterFromJson(chapter);
                        if (chapterObj != null)
                        {
                            chapters.Add(chapterObj);
                            Console.WriteLine($"Added chapter: {chapterObj.Title}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No chapters array found in response");
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Error in GetChaptersAsync: {httpEx.Message}");
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON Error in GetChaptersAsync: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error in GetChaptersAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine($"Returning {chapters.Count} chapters");
            return chapters.OrderBy(c => c.Title).ToList();
        }

        private Chapter ParseChapterFromJson(JsonElement chapter)
        {
            try
            {
                Console.WriteLine($"Parsing chapter, ValueKind: {chapter.ValueKind}");

                if (chapter.ValueKind != JsonValueKind.Object)
                {
                    Console.WriteLine("Chapter is not an object, skipping");
                    return null;
                }

                // Debug: Print available properties
                Console.WriteLine("Available chapter properties:");
                foreach (var prop in chapter.EnumerateObject())
                {
                    Console.WriteLine($"  - {prop.Name}: {prop.Value.ValueKind}");
                }

                string chap = GetStringProperty(chapter, new[] { "chap", "chapter", "number" });
                string vol = GetStringProperty(chapter, new[] { "vol", "volume" });
                string title = GetStringProperty(chapter, new[] { "title", "name" });
                string hid = GetStringProperty(chapter, new[] { "hid", "id" });

                Console.WriteLine($"Chapter data - chap: '{chap}', vol: '{vol}', title: '{title}', hid: '{hid}'");

                if (string.IsNullOrEmpty(hid))
                {
                    Console.WriteLine("No HID found for chapter, skipping");
                    return null;
                }

                var chapterTitle = BeautifyChapterName(vol, chap, title);

                return new Chapter
                {
                    Title = chapterTitle,
                    Url = $"{API_BASE}/chapter/{hid}",
                    Language = "en"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing chapter: {ex.Message}");
                return null;
            }
        }

        private string GetStringProperty(JsonElement element, string[] possibleNames)
        {
            foreach (var name in possibleNames)
            {
                if (element.TryGetProperty(name, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.String)
                    {
                        return prop.GetString() ?? "";
                    }
                    else if (prop.ValueKind == JsonValueKind.Number)
                    {
                        return prop.GetDouble().ToString();
                    }
                }
            }
            return "";
        }

        private string BeautifyChapterName(string vol, string chap, string title)
        {
            var result = "";

            if (!string.IsNullOrEmpty(vol) && vol != "null")
            {
                if (string.IsNullOrEmpty(chap) || chap == "null")
                {
                    result += $"Volume {vol} ";
                }
                else
                {
                    result += $"Vol. {vol} ";
                }
            }

            if (!string.IsNullOrEmpty(chap) && chap != "null")
            {
                if (string.IsNullOrEmpty(vol) || vol == "null")
                {
                    result += $"Chapter {chap}";
                }
                else
                {
                    result += $"Ch. {chap}";
                }
            }

            if (!string.IsNullOrEmpty(title) && title != "null")
            {
                if (!string.IsNullOrEmpty(result))
                {
                    result += $" - {title}";
                }
                else
                {
                    result = title;
                }
            }

            return string.IsNullOrWhiteSpace(result) ? "Unknown Chapter" : result.Trim();
        }

        private string ExtractHidFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var segments = uri.Segments;

                for (int i = 0; i < segments.Length - 1; i++)
                {
                    if (segments[i].TrimEnd('/') == "comic")
                    {
                        var hid = segments[i + 1].TrimEnd('/');
                        Console.WriteLine($"Extracted HID: {hid} from URL: {url}");
                        return hid;
                    }
                }

                var lastSegment = segments[segments.Length - 1].TrimEnd('/');
                Console.WriteLine($"Fallback HID: {lastSegment} from URL: {url}");
                return lastSegment;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting HID from URL {url}: {ex.Message}");
                return "";
            }
        }

        public async Task<List<string>> GetPageImagesAsync(Chapter chapter)
        {
            var pages = new List<string>();

            try
            {
                var url = chapter.Url;
                Console.WriteLine($"Fetching page images from: {url}");

                var response = await _http.GetStringAsync(url);
                Console.WriteLine($"Page images response length: {response.Length}");

                using var json = JsonDocument.Parse(response);
                var root = json.RootElement;

                // Try different structures for images
                JsonElement imagesArray = default;
                bool foundImages = false;

                // Try nested structure first
                if (root.TryGetProperty("chapter", out var chapterData) &&
                    chapterData.TryGetProperty("images", out imagesArray) &&
                    imagesArray.ValueKind == JsonValueKind.Array)
                {
                    foundImages = true;
                    Console.WriteLine($"Found images in nested structure: {imagesArray.GetArrayLength()} images");
                }
                // Try direct images property
                else if (root.TryGetProperty("images", out imagesArray) && imagesArray.ValueKind == JsonValueKind.Array)
                {
                    foundImages = true;
                    Console.WriteLine($"Found images in direct structure: {imagesArray.GetArrayLength()} images");
                }

                if (foundImages)
                {
                    foreach (var image in imagesArray.EnumerateArray())
                    {
                        string imageUrl = null;

                        if (image.ValueKind == JsonValueKind.String)
                        {
                            imageUrl = image.GetString();
                        }
                        else if (image.ValueKind == JsonValueKind.Object)
                        {
                            if (image.TryGetProperty("url", out var urlProp))
                            {
                                imageUrl = urlProp.GetString();
                            }
                        }

                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            pages.Add(imageUrl);
                            Console.WriteLine($"Added page image: {imageUrl}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No images array found in response");
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Error in GetPageImagesAsync: {httpEx.Message}");
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON Error in GetPageImagesAsync: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error in GetPageImagesAsync: {ex.Message}");
            }

            Console.WriteLine($"Returning {pages.Count} page images");
            return pages;
        }

        public void Dispose()
        {
            _http?.Dispose();
        }
    }
}