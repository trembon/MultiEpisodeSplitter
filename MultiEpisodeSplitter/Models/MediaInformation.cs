using FFMpegCore;

namespace MultiEpisodeSplitter.Models
{
    internal class MediaInformation
    {
        public string FullPath { get; set; }

        public string FileName { get; set; }

        public System.Drawing.Size Size { get; set; }

        public IMediaAnalysis MediaData { get; set; }
    }
}
