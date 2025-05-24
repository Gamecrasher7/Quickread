using QuickRead.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace QuickRead.Sources
{
    public class MangaDexService : ISourceService
    {
        public string Name => "MangaDX";

        private readonly HttpClient _http;
        private const string API_BASE = "https://api.mangadx.org";
        private const string BASE_URL = "https://mangadx.org";
        private const string UPLOAD_BASE = "https://uploads.mangadx.org";

        public MangaDexService()
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("User-Agent", "Dalvik/2.1.0 (Linux; U; Android 14; 22081212UG Build/UKQ1.230917.001)");
            _http.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<List<Manga>> GetLatestMangaAsync()
        {
            var mangas = new List<Manga>();

            try
            {
                var url = $"{API_BASE}/manga?limit=20&offset=0&availableTranslatedLanguage[]=en&includes[]=cover_art&contentRating[]=safe&contentRating[]=suggestive&order[followedCount]=desc";
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
                var encodedQuery = HttpUtility.UrlEncode(query);
                var url = $"{API_BASE}/manga?includes[]=cover_art&offset=0&limit=20&title={encodedQuery}&availableTranslatedLanguage[]=en&contentRating[]=safe&contentRating[]=suggestive";

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

                using var json = JsonDocument.Parse(jsonResponse);
                var root = json.RootElement;

                if (root.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                {
                    Console.WriteLine($"Found {dataArray.GetArrayLength()} manga items in data array");

                    foreach (var item in dataArray.EnumerateArray())
                    {
                        var manga = ParseSingleMangaFromJson(item);
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

                var id = GetStringProperty(item, new[] { "id" });
                if (string.IsNullOrEmpty(id))
                {
                    return null;
                }

                var title = FindTitle(item);
                if (string.IsNullOrEmpty(title))
                {
                    return null;
                }

                var coverUrl = GetCoverUrl(item, id);

                var manga = new Manga
                {
                    Title = title,
                    Url = $"/manga/{id}",
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

        private string FindTitle(JsonElement data)
        {
            try
            {
                if (data.TryGetProperty("attributes", out var attributes))
                {
                    if (attributes.TryGetProperty("title", out var titles) && titles.ValueKind == JsonValueKind.Object)
                    {
                        // Try English first
                        if (titles.TryGetProperty("en", out var enTitle) && enTitle.ValueKind == JsonValueKind.String)
                        {
                            return enTitle.GetString() ?? "";
                        }

                        // Try any other title
                        foreach (var title in titles.EnumerateObject())
                        {
                            if (title.Value.ValueKind == JsonValueKind.String)
                            {
                                return title.Value.GetString() ?? "";
                            }
                        }
                    }

                    // Try alternative titles
                    if (attributes.TryGetProperty("altTitles", out var altTitles) && altTitles.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var altTitle in altTitles.EnumerateArray())
                        {
                            if (altTitle.TryGetProperty("en", out var enAlt) && enAlt.ValueKind == JsonValueKind.String)
                            {
                                return enAlt.GetString() ?? "";
                            }
                        }
                    }
                }

                return "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding title: {ex.Message}");
                return "";
            }
        }

        private string GetCoverUrl(JsonElement data, string mangaId)
        {
            try
            {
                if (data.TryGetProperty("relationships", out var relationships) && relationships.ValueKind == JsonValueKind.Array)
                {
                    foreach (var rel in relationships.EnumerateArray())
                    {
                        if (rel.TryGetProperty("type", out var type) && type.GetString() == "cover_art")
                        {
                            if (rel.TryGetProperty("attributes", out var attributes) &&
                                attributes.TryGetProperty("fileName", out var fileName) &&
                                fileName.ValueKind == JsonValueKind.String)
                            {
                                return $"{UPLOAD_BASE}/covers/{mangaId}/{fileName.GetString()}";
                            }
                        }
                    }
                }

                return "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting cover URL: {ex.Message}");
                return "";
            }
        }

        public async Task<List<Chapter>> GetChaptersAsync(Manga manga)
        {
            var chapters = new List<Chapter>();

            try
            {
                var mangaId = ExtractMangaIdFromUrl(manga.Url);
                if (string.IsNullOrEmpty(mangaId))
                {
                    Console.WriteLine("Could not extract manga ID from URL: " + manga.Url);
                    return chapters;
                }

                Console.WriteLine($"Getting chapters for manga ID: {mangaId}");

                // Get all chapters with pagination
                var offset = 0;
                var hasMoreResults = true;

                while (hasMoreResults)
                {
                    var url = $"{API_BASE}/manga/{mangaId}/feed?limit=500&offset={offset}&includes[]=user&includes[]=scanlation_group&order[volume]=desc&order[chapter]=desc&translatedLanguage[]=en&includeFuturePublishAt=0&includeEmptyPages=0&contentRating[]=safe&contentRating[]=suggestive";
                    Console.WriteLine($"Fetching chapters from: {url}");

                    var response = await _http.GetStringAsync(url);
                    Console.WriteLine($"Chapters response length: {response.Length}");

                    using var json = JsonDocument.Parse(response);
                    var root = json.RootElement;

                    var limit = 0;
                    var total = 0;

                    if (root.TryGetProperty("limit", out var limitProp) && limitProp.ValueKind == JsonValueKind.Number)
                    {
                        limit = limitProp.GetInt32();
                    }

                    if (root.TryGetProperty("total", out var totalProp) && totalProp.ValueKind == JsonValueKind.Number)
                    {
                        total = totalProp.GetInt32();
                    }

                    if (root.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        Console.WriteLine($"Found {dataArray.GetArrayLength()} chapters in batch");

                        foreach (var chapterData in dataArray.EnumerateArray())
                        {
                            var chapter = ParseChapterFromJson(chapterData);
                            if (chapter != null)
                            {
                                chapters.Add(chapter);
                            }
                        }
                    }

                    offset += limit;
                    hasMoreResults = offset < total;

                    Console.WriteLine($"Processed {offset} of {total} chapters");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetChaptersAsync: {ex.Message}");
            }

            Console.WriteLine($"Returning {chapters.Count} chapters");
            return chapters.OrderByDescending(c => ParseChapterNumber(c.Title)).ToList();
        }

        private Chapter? ParseChapterFromJson(JsonElement chapterData)
        {
            try
            {
                var id = GetStringProperty(chapterData, new[] { "id" });
                if (string.IsNullOrEmpty(id))
                {
                    return null;
                }

                if (!chapterData.TryGetProperty("attributes", out var attributes))
                {
                    return null;
                }

                var volume = GetStringProperty(attributes, new[] { "volume" });
                var chapter = GetStringProperty(attributes, new[] { "chapter" });
                var title = GetStringProperty(attributes, new[] { "title" });
                var publishAt = GetStringProperty(attributes, new[] { "publishAt" });

                // Build chapter name
                var chapName = BuildChapterName(volume, chapter, title);

                // Get scanlation group
                var scanlator = GetScanlationGroup(chapterData);

                // Parse date
                var dateUpload = "";
                if (!string.IsNullOrEmpty(publishAt) && DateTime.TryParse(publishAt, out var date))
                {
                    dateUpload = ((DateTimeOffset)date).ToUnixTimeMilliseconds().ToString();
                }

                return new Chapter
                {
                    Title = chapName,
                    Url = id, // Store the chapter ID for page fetching
                    Language = "en"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing chapter: {ex.Message}");
                return null;
            }
        }

        private string BuildChapterName(string volume, string chapter, string title)
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(volume) && volume != "null")
            {
                parts.Add($"Vol.{volume}");
            }

            if (!string.IsNullOrEmpty(chapter) && chapter != "null")
            {
                parts.Add($"Ch.{chapter}");
            }

            if (parts.Count == 0 && string.IsNullOrEmpty(title))
            {
                return "Oneshot";
            }

            var result = string.Join(" ", parts);

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

            return string.IsNullOrWhiteSpace(result) ? "Unknown Chapter" : result;
        }

        private string GetScanlationGroup(JsonElement chapterData)
        {
            try
            {
                if (chapterData.TryGetProperty("relationships", out var relationships) && relationships.ValueKind == JsonValueKind.Array)
                {
                    var groups = new List<string>();

                    foreach (var rel in relationships.EnumerateArray())
                    {
                        if (rel.TryGetProperty("type", out var type))
                        {
                            var typeStr = type.GetString();
                            if (typeStr == "scanlation_group")
                            {
                                if (rel.TryGetProperty("attributes", out var attributes) &&
                                    attributes.TryGetProperty("name", out var name) &&
                                    name.ValueKind == JsonValueKind.String)
                                {
                                    var groupName = name.GetString();
                                    if (!string.IsNullOrEmpty(groupName))
                                    {
                                        groups.Add(groupName);
                                    }
                                }
                            }
                            else if (typeStr == "user" && groups.Count == 0)
                            {
                                if (rel.TryGetProperty("attributes", out var attributes) &&
                                    attributes.TryGetProperty("username", out var username) &&
                                    username.ValueKind == JsonValueKind.String)
                                {
                                    var userName = username.GetString();
                                    if (!string.IsNullOrEmpty(userName))
                                    {
                                        return $"Uploaded by {userName}";
                                    }
                                }
                            }
                        }
                    }

                    if (groups.Count > 0)
                    {
                        return string.Join(", ", groups);
                    }
                }

                return "No Group";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting scanlation group: {ex.Message}");
                return "No Group";
            }
        }

        private double ParseChapterNumber(string chapterTitle)
        {
            try
            {
                // Extract chapter number from title for sorting
                var chMatch = System.Text.RegularExpressions.Regex.Match(chapterTitle, @"Ch\.(\d+(?:\.\d+)?)");
                if (chMatch.Success && double.TryParse(chMatch.Groups[1].Value, out var chNum))
                {
                    return chNum;
                }

                var numMatch = System.Text.RegularExpressions.Regex.Match(chapterTitle, @"(\d+(?:\.\d+)?)");
                if (numMatch.Success && double.TryParse(numMatch.Groups[1].Value, out var num))
                {
                    return num;
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private string ExtractMangaIdFromUrl(string url)
        {
            try
            {
                // URL format: /manga/{id}
                var parts = url.Split('/');
                if (parts.Length >= 3 && parts[1] == "manga")
                {
                    return parts[2];
                }

                return "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting manga ID from URL {url}: {ex.Message}");
                return "";
            }
        }

        public async Task<List<string>> GetPageImagesAsync(Chapter chapter)
        {
            var pages = new List<string>();

            try
            {
                var chapterId = chapter.Url; // This contains the chapter ID
                Console.WriteLine($"Getting page images for chapter ID: {chapterId}");

                var url = $"{API_BASE}/at-home/server/{chapterId}";
                Console.WriteLine($"Fetching pages from: {url}");

                var response = await _http.GetStringAsync(url);
                Console.WriteLine($"Page response length: {response.Length}");

                using var json = JsonDocument.Parse(response);
                var root = json.RootElement;

                var baseUrl = GetStringProperty(root, new[] { "baseUrl" });
                if (string.IsNullOrEmpty(baseUrl))
                {
                    Console.WriteLine("No baseUrl found in response");
                    return pages;
                }

                if (root.TryGetProperty("chapter", out var chapterInfo))
                {
                    var hash = GetStringProperty(chapterInfo, new[] { "hash" });
                    if (string.IsNullOrEmpty(hash))
                    {
                        Console.WriteLine("No hash found in chapter info");
                        return pages;
                    }

                    if (chapterInfo.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        Console.WriteLine($"Found {dataArray.GetArrayLength()} pages");

                        foreach (var pageFile in dataArray.EnumerateArray())
                        {
                            if (pageFile.ValueKind == JsonValueKind.String)
                            {
                                var fileName = pageFile.GetString();
                                if (!string.IsNullOrEmpty(fileName))
                                {
                                    var pageUrl = $"{baseUrl}/data/{hash}/{fileName}";
                                    pages.Add(pageUrl);
                                    Console.WriteLine($"Added page: {pageUrl}");
                                }
                            }
                        }
                    }
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

        public void Dispose()
        {
            _http?.Dispose();
        }
    }
}