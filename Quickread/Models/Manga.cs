namespace QuickRead.Models
{
    public class Manga
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string CoverImageUrl { get; set; }
        public string Description { get; set; }
        public string Source { get; set; }

        public override string ToString()
        {
            return Title;
        }
    }
}
