using QuickRead.Models;

namespace QuickRead.Sources
{
    public interface ISourceService
    {
        string Name { get; }
        Task<List<Manga>> GetLatestMangaAsync();
        Task<List<Manga>> SearchMangaAsync(string query);
        Task<List<Chapter>> GetChaptersAsync(Manga manga);
        Task<List<string>> GetPageImagesAsync(Chapter chapter);
    }
}
