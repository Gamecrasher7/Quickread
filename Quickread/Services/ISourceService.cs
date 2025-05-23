using QuickRead.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuickRead.Sources
{
    public interface ISourceService : IDisposable
    {
        string Name { get; }
        Task<List<Manga>> GetLatestMangaAsync();
        Task<List<Manga>> SearchMangaAsync(string query);
        Task<List<Chapter>> GetChaptersAsync(Manga manga);
        Task<List<string>> GetPageImagesAsync(Chapter chapter);
    }
}