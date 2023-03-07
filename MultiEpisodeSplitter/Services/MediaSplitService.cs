using MultiEpisodeSplitter.Models;
using System.IO;
using System.Text.RegularExpressions;

namespace MultiEpisodeSplitter.Services
{
    internal interface IMediaSplitService
    {
        Task SplitMedia(MediaInformation media, IEnumerable<SplitMarking> splits, CancellationToken cancellationToken);

        IEnumerable<string> CalculateFileNames(MediaInformation media, IEnumerable<SplitMarking> splits);
    }

    internal class MediaSplitService : IMediaSplitService
    {
        private Regex episodeRegex = new Regex("S(\\d\\d)E(\\d\\d)-?E(\\d\\d)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public Task SplitMedia(MediaInformation media, IEnumerable<SplitMarking> splits, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();


            //// split
            //string streamMaps = string.Join(' ', streams.Select(x => $"-map {x}"));

            //string arguments = $"-i \"{file}\" -ss {start.ToString().Replace(',', '.')} -vcodec copy -c:a copy {streamMaps} \"{output}\"";
            //if (end.HasValue)
            //    arguments = $"-i \"{file}\" -ss {start.ToString().Replace(',', '.')} -to {end.Value.ToString().Replace(',', '.')} -vcodec copy -c:a copy {streamMaps} \"{output}\"";

            //_ = StartFFMPEG(ffmpegPath, arguments);


            //// create index file for ffmpeg to merge
            //List<string> outputLines = new(files.Length);
            //foreach (string file in files)
            //    outputLines.Add($"file '{file}'");

            //File.WriteAllLines(output, outputLines);


            //// concat
            //string streamMaps = string.Join(' ', streams.Select(x => $"-map {x}"));
            //_ = StartFFMPEG(ffmpegPath, $"-f concat -safe 0 -i \"{inputList}\" -vcodec copy -c:a copy {streamMaps} \"{output}\"");
        }

        public IEnumerable<string> CalculateFileNames(MediaInformation media, IEnumerable<SplitMarking> splits)
        {
            if(splits == null || !splits.Any())
                return Array.Empty<string>();

            string fileName = Path.GetFileName(media.FullPath);
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
