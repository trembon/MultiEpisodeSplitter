using FFMpegCore;
using FFMpegCore.Pipes;
using MultiEpisodeSplitter.Models;
using System.Drawing;

namespace MultiEpisodeSplitter.Services
{
    internal interface IMediaInformationService
    {
        Task<MediaInformation> LoadMedia(string filePath);

        Task<string> GetStillFromMediaAsBase64(MediaInformation media, int atSecond);
    }

    internal class MediaInformationService : IMediaInformationService
    {
        public async Task<string> GetStillFromMediaAsBase64(MediaInformation media, int atSecond)
        {
            var args = SnapshotArgumentBuilder.BuildSnapshotArguments(media.FullPath, media.MediaData, media.Size, TimeSpan.FromSeconds(atSecond));
            using var ms = new MemoryStream();

            await args.Item1
                .OutputToPipe(new StreamPipeSink(ms), options => args.outputOptions(options.ForceFormat("rawvideo")))
                .ProcessAsynchronously();

            ms.Position = 0;
            using var bitmap = new Bitmap(ms);
            bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), bitmap.PixelFormat);

            return string.Format("data:image/jpeg;base64,{0}", Convert.ToBase64String(ms.GetBuffer()));
        }

        public async Task<MediaInformation> LoadMedia(string filePath)
        {
            var mediaInfo = await FFProbe.AnalyseAsync(filePath);
            var size = new System.Drawing.Size(mediaInfo.PrimaryVideoStream.Width, mediaInfo.PrimaryVideoStream.Height);

            return new MediaInformation
            {
                FullPath = filePath,
                FileName = Path.GetFileName(filePath),
                Size = size,
                MediaData = mediaInfo
            };
        }
    }
}
