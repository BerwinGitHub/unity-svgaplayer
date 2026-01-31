using System;
using System.IO;
using UnityEngine;

namespace Bo.SVGA
{
    /// <summary>
    ///     SVGA 音频数据
    /// </summary>
    public class SVGAAudioData
    {
        public ISVGAPlayerProvider Provider { get; set; }

        public string AudioKey { get; set; }

        public AudioType AudioType { get; set; }

        public byte[] Binary { get; set; }

        /// <summary>
        /// 音频本地路径
        /// </summary>
        public string Path { get; set; } = string.Empty;

        public SVGAAudioData(string audioKey, byte[] audioData, ISVGAPlayerProvider provider)
        {
            AudioKey = audioKey;
            Provider = provider;
            Binary = audioData;
            var path = Provider.GetWritablePath() + "/svga-audios";
            SaveAudioDataToFile(audioKey, audioData, path);
        }

        private void SaveAudioDataToFile(string fileName, byte[] audioData, string rootPath)
        {
            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }

            AudioType = SVGAAudioUtils.GetAudioType(audioData);
            Path = System.IO.Path.Combine(rootPath, fileName + "." + SVGAAudioUtils.GetAudioTypeString(AudioType));
            using (var fileStream = new FileStream(Path, FileMode.Create, FileAccess.Write))
            {
                try
                {
                    fileStream.Write(audioData, 0, audioData.Length);
                }
                catch (Exception e)
                {
                    Provider.LogError($"保存音频文件保存, file:{Path} error:{e}");
                }
            }
        }
    }
}