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

                if (root.ValueKind == JsonValueKind.Array)
                {
                    Console.WriteLine("Processing direct array response");
                    foreach (var item in root.EnumerateArray())
                    {
                        var manga = ParseSingleMangaFromJson(item);
                        if (manga != null)
                        {
                            mangas.Add(manga);
                        }
                    }
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    Console.WriteLine("Processing object response, looking for data array");

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
                                }
                            }
                            break;
                        }
                    }

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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing manga list: {ex.Message}");
            }

            return mangas;
        }

        private Manga? ParseSingleMangaFromJson(JsonElement item)
        {
            try
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                string? title = null;
                string? hid = null;
                string coverUrl = "";

                // Get title
                string[] titleProps = { "title", "name", "en", "slug" };
                foreach (var prop in titleProps)
                {
                    if (item.TryGetProperty(prop, out var titleProp))
                    {
                        if (titleProp.ValueKind == JsonValueKind.String)
                        {
                            title = titleProp.GetString();
                            break;
                        }
                        else if (titleProp.ValueKind == JsonValueKind.Object && prop == "en")
                        {
                            if (titleProp.TryGetProperty("title", out var nestedTitle))
                            {
                                title = nestedTitle.GetString();
                                break;
                            }
                        }
                    }
                }

                // Get ID
                string[] idProps = { "hid", "id", "slug", "comic_id" };
                foreach (var prop in idProps)
                {
                    if (item.TryGetProperty(prop, out var idProp))
                    {
                        if (idProp.ValueKind == JsonValueKind.String)
                        {
                            hid = idProp.GetString();
                            break;
                        }
                        else if (idProp.ValueKind == JsonValueKind.Number)
                        {
                            hid = idProp.GetInt32().ToString();
                            break;
                        }
                    }
                }

                // Get cover
                string[] coverProps = { "cover_url", "cover", "thumbnail", "image", "poster" };
                foreach (var prop in coverProps)
                {
                    if (item.TryGetProperty(prop, out var coverProp) && coverProp.ValueKind == JsonValueKind.String)
                    {
                        coverUrl = coverProp.GetString() ?? "";
                        break;
                    }
                }

                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(hid))
                {
                    return null;
                }

                var manga = new Manga
                {
                    Title = title,
                    Url = $"{BASE_URL}/comic/{hid}",
                    CoverImageUrl = coverUrl,
                    Source = Name
                };

                return manga;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing single manga: {ex.Message}");
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

                using var chaptersJson = JsonDocument.Parse(chaptersResponse);
                var root = chaptersJson.RootElement;

                Console.WriteLine($"Chapters JSON ValueKind: {root.ValueKind}");

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
                        var chapterObj = ParseChapterFromJson(chapter, hid);
                        if (chapterObj != null)
                        {
                            chapters.Add(chapterObj);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No chapters array found in response");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetChaptersAsync: {ex.Message}");
            }

            Console.WriteLine($"Returning {chapters.Count} chapters");
            return chapters.OrderBy(c => c.Title).ToList();
        }

        private Chapter ParseChapterFromJson(JsonElement chapter, string mangaHid)
        {
            try
            {
                Console.WriteLine($"Parsing chapter, ValueKind: {chapter.ValueKind}");

                if (chapter.ValueKind != JsonValueKind.Object)
                {
                    return null;
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
                    Url = hid, // Store just the HID for easier processing later
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
                // The chapter.Url now contains just the HID
                var chapterHid = chapter.Url;
                Console.WriteLine($"Getting page images for chapter HID: {chapterHid}");

                // Try multiple possible endpoints for getting chapter images
                var possibleUrls = new[]
                {
                    $"{API_BASE}/chapter/{chapterHid}",
                    $"{API_BASE}/v1.0/chapter/{chapterHid}",
                    $"https://api.comick.cc/chapter/{chapterHid}"
                };

                string response = null;
                string workingUrl = null;

                foreach (var url in possibleUrls)
                {
                    try
                    {
                        Console.WriteLine($"Trying URL: {url}");
                        response = await _http.GetStringAsync(url);
                        workingUrl = url;
                        Console.WriteLine($"Success with URL: {url}, response length: {response.Length}");
                        break;
                    }
                    catch (HttpRequestException httpEx)
                    {
                        Console.WriteLine($"Failed with URL {url}: {httpEx.Message}");
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(response))
                {
                    Console.WriteLine("No valid response from any URL");
                    return pages;
                }

                Console.WriteLine($"Chapter response preview: {response.Substring(0, Math.Min(500, response.Length))}");

                using var json = JsonDocument.Parse(response);
                var root = json.RootElement;

                Console.WriteLine($"Chapter JSON ValueKind: {root.ValueKind}");

                // Debug: Print available properties
                if (root.ValueKind == JsonValueKind.Object)
                {
                    Console.WriteLine("Available properties in chapter response:");
                    foreach (var prop in root.EnumerateObject())
                    {
                        Console.WriteLine($"  - {prop.Name}: {prop.Value.ValueKind}");
                    }
                }

                // Try different structures for images
                JsonElement imagesArray = default;
                bool foundImages = false;

                // Try nested structure first: chapter -> images
                if (root.TryGetProperty("chapter", out var chapterData))
                {
                    Console.WriteLine("Found 'chapter' property");
                    if (chapterData.TryGetProperty("images", out imagesArray) && imagesArray.ValueKind == JsonValueKind.Array)
                    {
                        foundImages = true;
                        Console.WriteLine($"Found images in nested structure: {imagesArray.GetArrayLength()} images");
                    }
                    else if (chapterData.TryGetProperty("md_images", out imagesArray) && imagesArray.ValueKind == JsonValueKind.Array)
                    {
                        foundImages = true;
                        Console.WriteLine($"Found md_images in nested structure: {imagesArray.GetArrayLength()} images");
                    }
                }

                // Try direct images property
                if (!foundImages && root.TryGetProperty("images", out imagesArray) && imagesArray.ValueKind == JsonValueKind.Array)
                {
                    foundImages = true;
                    Console.WriteLine($"Found images in direct structure: {imagesArray.GetArrayLength()} images");
                }

                // Try md_images property
                if (!foundImages && root.TryGetProperty("md_images", out imagesArray) && imagesArray.ValueKind == JsonValueKind.Array)
                {
                    foundImages = true;
                    Console.WriteLine($"Found md_images in direct structure: {imagesArray.GetArrayLength()} images");
                }

                // Try pages property
                if (!foundImages && root.TryGetProperty("pages", out imagesArray) && imagesArray.ValueKind == JsonValueKind.Array)
                {
                    foundImages = true;
                    Console.WriteLine($"Found pages in direct structure: {imagesArray.GetArrayLength()} images");
                }

                if (foundImages)
                {
                    var baseUrl = "";

                    // Try to get base URL from chapter data
                    if (root.TryGetProperty("chapter", out var chapterInfo))
                    {
                        if (chapterInfo.TryGetProperty("md_images", out var mdImages) && mdImages.ValueKind == JsonValueKind.Object)
                        {
                            // This might contain server info
                            foreach (var server in mdImages.EnumerateObject())
                            {
                                if (server.Value.ValueKind == JsonValueKind.Array && server.Value.GetArrayLength() > 0)
                                {
                                    imagesArray = server.Value;
                                    baseUrl = $"https://meo.comick.pictures/{server.Name}/";
                                    Console.WriteLine($"Using server: {server.Name}, base URL: {baseUrl}");
                                    break;
                                }
                            }
                        }
                    }

                    foreach (var image in imagesArray.EnumerateArray())
                    {
                        string imageUrl = null;

                        if (image.ValueKind == JsonValueKind.String)
                        {
                            imageUrl = image.GetString();
                        }
                        else if (image.ValueKind == JsonValueKind.Object)
                        {
                            // Try different property names for image URL
                            var urlProps = new[] { "url", "src", "link", "image" };
                            foreach (var prop in urlProps)
                            {
                                if (image.TryGetProperty(prop, out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
                                {
                                    imageUrl = urlProp.GetString();
                                    break;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            // If imageUrl is relative and we have a base URL, combine them
                            if (!imageUrl.StartsWith("http") && !string.IsNullOrEmpty(baseUrl))
                            {
                                imageUrl = baseUrl + imageUrl;
                            }

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
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
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