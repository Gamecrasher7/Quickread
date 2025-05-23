using System.Collections.Generic;

namespace QuickRead.Models
{
    public class Chapter
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
        public List<string> PageImageUrls { get; set; } = new List<string>();

        public override string ToString()
        {
            return Title;
        }
    }
}