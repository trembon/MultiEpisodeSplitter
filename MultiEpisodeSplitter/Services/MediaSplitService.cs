using MultiEpisodeSplitter.Models;
using System.Text.RegularExpressions;

namespace MultiEpisodeSplitter.Services
{
    internal interface IMediaSplitService
    {
        List<SplitMarking> AddSplitIntro(int seconds, MediaInformation media, List<SplitMarking> splits);

        List<SplitMarking> AddSplitStart(int seconds, MediaInformation media, List<SplitMarking> splits);

        List<SplitMarking> AddSplitEnd(int seconds, MediaInformation media, List<SplitMarking> splits);

        List<SplitMarking> AddSplitOutro(int seconds, MediaInformation media, List<SplitMarking> splits);

        Task SplitMedia(MediaInformation media, IEnumerable<SplitMarking> splits, CancellationToken cancellationToken);

        IEnumerable<string> CalculateFileNames(MediaInformation media, IEnumerable<SplitMarking> splits);
    }

    internal class MediaSplitService : IMediaSplitService
    {
        private Regex episodeRegex = new Regex("S(\\d\\d)E(\\d\\d)(-?E(\\d\\d))*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

            List<string> result = new();

            string fileName = Path.GetFileName(media.FullPath);
            var match = episodeRegex.Match(fileName);
            if (match.Success)
            {
                for (int i = 0; i < splits.Count(x => !x.IsIntro && !x.IsOutro); i++)
                {
                    int episode = int.Parse(match.Groups[2].Value) + i;
                    string episodeFilename = fileName.Replace(match.Value, $"S{match.Groups[1].Value}E{episode.ToString("D2")}");
                    result.Add(episodeFilename);
                }
            }
            else
            {
                for (int i = 0; i < splits.Count(x => !x.IsIntro && !x.IsOutro); i++)
                {
                    result.Add("Show.E0" + (i + 1).ToString("D2") + Path.GetExtension(fileName));
                }
            }

            return result;
        }

        public List<SplitMarking> AddSplitIntro(int seconds, MediaInformation media, List<SplitMarking> splits)
        {
            List<SplitMarking> result = new();

            if (splits == null || splits.Count == 0)
            {
                result.Add(new SplitMarking
                {
                    Start = 0,
                    End = seconds,
                    IsIntro = true
                });

                result.Add(new SplitMarking
                {
                    Start = seconds,
                    End = (int)media.MediaData.Duration.TotalSeconds,
                });
            }
            else
            {
                result.AddRange(splits);

                var itemToSplit = splits.FirstOrDefault(x => x.Start < seconds && x.End > seconds);
                if (itemToSplit != null)
                    itemToSplit.Start = seconds;

                result.Add(new SplitMarking { Start = 0, End = seconds, IsIntro = true });
            }

            return result.OrderBy(x => x.Start).ToList();
        }

        public List<SplitMarking> AddSplitStart(int seconds, MediaInformation media, List<SplitMarking> splits)
        {
            List<SplitMarking> result = new();

            if (splits == null || splits.Count == 0)
            {
                result.Add(new SplitMarking
                {
                    Start = 0,
                    End = seconds,
                    IsIntro = true
                });

                result.Add(new SplitMarking
                {
                    Start = seconds,
                    End = (int)media.MediaData.Duration.TotalSeconds,
                });
            }
            else
            {
                result.AddRange(splits);

                var itemToSplit = splits.First(x => x.Start < seconds && x.End > seconds);

                int endSeconds = itemToSplit.End;
                itemToSplit.End = seconds;

                result.Add(new SplitMarking { Start = seconds, End = endSeconds });
            }

            return result.OrderBy(x => x.Start).ToList();
        }

        public List<SplitMarking> AddSplitEnd(int seconds, MediaInformation media, List<SplitMarking> splits)
        {
            List<SplitMarking> result = new();

            if (splits == null || splits.Count == 0)
            {
                result.Add(new SplitMarking
                {
                    Start = 0,
                    End = seconds,
                });

                result.Add(new SplitMarking
                {
                    Start = seconds,
                    End = (int)media.MediaData.Duration.TotalSeconds,
                    IsOutro = true
                });
            }
            else
            {
                result.AddRange(splits);

                var itemToSplit = splits.First(x => x.Start < seconds && x.End > seconds);

                int startSeconds = itemToSplit.Start;
                itemToSplit.Start = seconds;

                result.Add(new SplitMarking { Start = startSeconds, End = seconds });
            }

            return result.OrderBy(x => x.Start).ToList();
        }

        public List<SplitMarking> AddSplitOutro(int seconds, MediaInformation media, List<SplitMarking> splits)
        {
            List<SplitMarking> result = new();

            if (splits == null || splits.Count == 0)
            {
                result.Add(new SplitMarking
                {
                    Start = 0,
                    End = seconds,
                });

                result.Add(new SplitMarking
                {
                    Start = seconds,
                    End = (int)media.MediaData.Duration.TotalSeconds,
                    IsOutro = true
                });
            }
            else
            {
                result.AddRange(splits);

                var itemToSplit = splits.FirstOrDefault(x => x.Start < seconds && x.End > seconds);
                if (itemToSplit != null)
                    itemToSplit.End = seconds;

                result.Add(new SplitMarking { Start = seconds, End = (int)media.MediaData.Duration.TotalSeconds, IsOutro = true });
            }

            return result.OrderBy(x => x.Start).ToList();
        }
    }
}
