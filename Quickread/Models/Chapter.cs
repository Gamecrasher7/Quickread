namespace QuickRead.Models
{
    public class Chapter
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Language { get; set; }
        public List<string> PageImageUrls { get; set; } = new List<string>();

        public override string ToString()
        {
            return Title;
        }
    }
}
