namespace QuickRead.Models
{
    public class Manga
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;

        public override string ToString()
        {
            return Title;
        }
    }
}