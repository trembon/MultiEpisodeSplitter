using MultiEpisodeSplitter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MultiEpisodeSplitter.Services
{
    internal interface IMediaSplitService
    {
        Task SplitMedia(MediaInformation media, IEnumerable<SplitMarking> splits, CancellationToken cancellationToken);
    }

    internal class MediaSplitService : IMediaSplitService
    {
        private Regex episodeRegex = new Regex("S(\\d\\d)E(\\d\\d)-?E(\\d\\d)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public Task SplitMedia(MediaInformation media, IEnumerable<SplitMarking> splits, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<string> CalculateFileNames(string fullPath, IEnumerable<SplitMarking> splits)
        {
            string fileName = Path.GetFileName(fullPath);
            var match = episodeRegex.Match(fileName);
            if (match.Groups.Count == 4)
            {
                string rel1 = fileName.Replace(match.Value, $"S{match.Groups[1].Value}E{match.Groups[2].Value}");
                string rel2 = fileName.Replace(match.Value, $"S{match.Groups[1].Value}E{match.Groups[3].Value}");
            }
            else
            {
                string tmp1 = "Show.E01" + Path.GetExtension(fileName);
                string tmp2 = "Show.E02" + Path.GetExtension(fileName);
            }

            return null;
        }
    }
}
