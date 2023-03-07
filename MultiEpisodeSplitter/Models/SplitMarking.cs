namespace MultiEpisodeSplitter.Models
{
    internal class SplitMarking
    {
        public int Start { get; set; }
        public int End { get; set; }

        public bool IsIntro { get; set; }
        public bool IsAfterTexts { get; set; }
    }
}
