using FFMpegCore;
using FFMpegCore.Arguments;
using MultiEpisodeSplitter.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MultiEpisodeSplitter.Services
{
    internal interface IMediaSplitService
    {
        List<SplitMarking> AddSplitIntro(int seconds, MediaInformation media, List<SplitMarking> splits);

        List<SplitMarking> AddSplitStart(int seconds, MediaInformation media, List<SplitMarking> splits);

        List<SplitMarking> AddSplitEnd(int seconds, MediaInformation media, List<SplitMarking> splits);

        List<SplitMarking> AddSplitOutro(int seconds, MediaInformation media, List<SplitMarking> splits);

        Task SplitMedia(string output, MediaInformation media, IEnumerable<SplitMarking> splits);

        IEnumerable<string> CalculateFileNames(MediaInformation media, IEnumerable<SplitMarking> splits);
    }

    internal class MediaSplitService : IMediaSplitService
    {
        private readonly Regex episodeRegex = new("S(\\d\\d)E(\\d\\d)(-?E(\\d\\d))*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public async Task SplitMedia(string output, MediaInformation media, IEnumerable<SplitMarking> splits)
        {
            string tempFilename = Path.Combine(output, DateTime.Now.Ticks.ToString());

            var intro = splits.FirstOrDefault(x => x.IsIntro);
            if(intro != null)
            {
                var args = FFMpegArguments.FromFileInput(media.FullPath, verifyExists: true, delegate (FFMpegArgumentOptions options)
                {
                    options.Seek(TimeSpan.FromSeconds(intro.Start)).EndSeek(TimeSpan.FromSeconds(intro.End));
                }).OutputToFile(tempFilename + ".intro.mkv", overwrite: true, delegate (FFMpegArgumentOptions options)
                {
                    options.CopyChannel();
                    options.WithArgument(new CustomArgument("-map 0 -map -0:s"));
                });

                await Start(GlobalFFOptions.GetFFMpegBinaryPath(), args.Arguments);
            }

            var outro = splits.FirstOrDefault(x => x.IsOutro);
            if (outro != null)
            {
                var args = FFMpegArguments.FromFileInput(media.FullPath, verifyExists: true, delegate (FFMpegArgumentOptions options)
                {
                    options.Seek(TimeSpan.FromSeconds(outro.Start)).EndSeek(TimeSpan.FromSeconds(outro.End));
                }).OutputToFile(tempFilename + ".outro.mkv", true, options =>
                {
                    options.CopyChannel();
                    options.WithArgument(new CustomArgument("-map 0 -map -0:s"));
                });

                await Start(GlobalFFOptions.GetFFMpegBinaryPath(), args.Arguments);
            }

            var episodes = splits.Where(x => !x.IsIntro && !x.IsOutro).ToList();
            for(int i = 0; i < episodes.Count; i++)
            {
                // split the episode
                var args = FFMpegArguments.FromFileInput(media.FullPath, verifyExists: true, delegate (FFMpegArgumentOptions options)
                {
                    options.Seek(TimeSpan.FromSeconds(episodes[i].Start)).EndSeek(TimeSpan.FromSeconds(episodes[i].End));
                }).OutputToFile(tempFilename + ".ep" + (i + 1) + ".mkv", true, options =>
                {
                    options.CopyChannel();
                    options.WithArgument(new CustomArgument("-map 0 -map -0:s"));
                });

                await Start(GlobalFFOptions.GetFFMpegBinaryPath(), args.Arguments);

                // create a concat file for ffmpeg to use
                List<string> files = new();
                if (intro != null)
                    files.Add(tempFilename + ".intro.mkv");

                files.Add(tempFilename + ".ep" + (i + 1) + ".mkv");

                if (outro != null)
                    files.Add(tempFilename + ".outro.mkv");

                File.WriteAllLines(tempFilename + ".txt", files.Select(x => $"file '{x}'"));

                // and merge the episode with intro, episode and outro
                var joinArgs = FFMpegArguments.FromFileInput(tempFilename + ".txt", verifyExists: true, delegate (FFMpegArgumentOptions options)
                {
                    options.WithCustomArgument("-f concat -safe 0");
                }).OutputToFile(tempFilename + ".ep" + (i + 1) + ".joined.mkv", true, options =>
                {
                    options.CopyChannel();
                    options.WithArgument(new CustomArgument("-map 0 -map -0:s"));
                });

                await Start(GlobalFFOptions.GetFFMpegBinaryPath(), joinArgs.Arguments);
            }
        }

        private static Task Start(string filePath, string arguments)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = false
                }
            };

            proc.Start();
            return proc.WaitForExitAsync();
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
                    string episodeFilename = fileName.Replace(match.Value, $"S{match.Groups[1].Value}E{episode:D2}");
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
                    Start = seconds + 1,
                    End = (int)media.MediaData.Duration.TotalSeconds,
                });
            }
            else
            {
                result.AddRange(splits);

                var itemToSplit = splits.FirstOrDefault(x => x.Start < seconds && x.End > seconds);
                if (itemToSplit != null)
                    itemToSplit.Start = seconds + 1;

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
                    End = seconds - 1,
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
                itemToSplit.End = seconds - 1;

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
                    Start = seconds + 1,
                    End = (int)media.MediaData.Duration.TotalSeconds,
                    IsOutro = true
                });
            }
            else
            {
                result.AddRange(splits);

                var itemToSplit = splits.First(x => x.Start < seconds && x.End > seconds);

                int startSeconds = itemToSplit.Start;
                itemToSplit.Start = seconds + 1;

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
                    End = seconds - 1,
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
                    itemToSplit.End = seconds - 1;

                result.Add(new SplitMarking { Start = seconds, End = (int)media.MediaData.Duration.TotalSeconds, IsOutro = true });
            }

            return result.OrderBy(x => x.Start).ToList();
        }
    }
}
