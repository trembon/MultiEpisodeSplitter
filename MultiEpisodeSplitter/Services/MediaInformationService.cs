using FFMpegCore.Enums;
using FFMpegCore;
using MultiEpisodeSplitter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var bitmap = await FFMpeg.SnapshotAsync(media.FullPath, media.Size, TimeSpan.FromSeconds(atSecond));

            using var ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

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
