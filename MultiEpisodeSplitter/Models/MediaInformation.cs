using FFMpegCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
