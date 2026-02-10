using System;

namespace LifeSim
{
    public class DiaryEntry
    {
        public DateTime Created { get; set; }
        public string Summary { get; set; } = "";
        public string Content { get; set; } = "";

        public DiaryEntry() { }

        public DiaryEntry(string summary, string content)
        {
            Created = DateTime.Now;
            Summary = summary;
            Content = content;
        }
    }
}
