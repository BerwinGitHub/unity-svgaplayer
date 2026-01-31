using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Collections;
using UnityEngine;

namespace Bo.SVGA
{
    /// <summary>
    /// SVGA 数据实体.
    /// </summary>
    public class SVGADat
    {
        /// <summary>
        /// SVGA 文件原始二进制.
        /// </summary>
        private byte[] InflatedBytes { get; set; }

        /// <summary>
        /// SVGA 文件解析后的 MovieEntity.
        /// </summary>
        public MovieEntity MovieEntity { get; set; }

        #region 基础解析数据

        /// <summary>
        /// SVGA 文件版本.
        /// @bind version
        /// </summary>
        public string Version => MovieEntity?.Version;

        /// <summary>
        /// SVGA 配置参数.
        /// @bind params = { viewBoxWidth: 1080, viewBoxHeight: 1920, fps: 30, frames: 100 }
        /// </summary>
        public MovieParams Params => MovieEntity?.Params;

        /// <summary>
        /// 资源，包含图片和音频。如果是 SVG 资源那么这个字段是没有的
        /// 键名为图片名称, 键值为 PNG 的二进制数据.
        /// @bind images = { "xxxx" : "iiao92kkx0bolk2..."}
        /// </summary>
        public MapField<string, ByteString> Images => MovieEntity?.Images;

        /// <summary>
        /// SVGA Sprite 对象，可以理解为有哪些骨骼，里面包含了每个骨骼的动画（资源、位置、缩放、旋转、透明度）帧数据.
        /// @bind sprites = [
        ///     { imageKey: "xxxx1", frames: [{ alpha: 1, layout:{ width: 100, height: 100 }, transform: { a: 0, d: 0, c:0, d:0, tx: 0, ty: 0 } }] }
        ///     { imageKey: "xxxx2", frames: [{}, {}] } // 可能 frames 里面有连续为空对象的情况
        /// ]
        /// </summary>
        public RepeatedField<SpriteEntity> Sprites => MovieEntity?.Sprites;


        /// <summary>
        /// SVGA Audio 对象.
        /// @bind audios = [
        ///     { audioKey: "xxxx", endFrame: 177, totalFrame: 5904 }
        /// ]
        /// </summary>
        public RepeatedField<AudioEntity> AudioEntities => MovieEntity?.Audios;

        #endregion


        public ISVGAPlayerProvider Provider { get; private set; }

        /// <summary>
        /// SVGA 图片二进制数据.
        /// </summary>
        public Dictionary<string, byte[]> ImagesBinaries;

        /// <summary>
        /// SVGA 音频二进制数据.
        /// </summary>
        private Dictionary<string, byte[]> AudioBinaries;

        #region 转码之后的数据

        /// <summary>
        /// 从 MovieEntity 中获取 SVGA 音频.
        /// MovieEntity 的 Audio 属性下保存了 SVGA 的音频文件, 为一个可枚举的 AudioEntity 集合,
        /// 每个 AudioEntity 包含了音频的名称, 开始时间, 结束时间, 音量等信息.
        /// </summary>
        // public Dictionary<string, string> AudioPaths;
        public Dictionary<int, List<SVGAAudioEntity>> FrameAudioDic;

        #endregion

        #region 供外部获取数据

        /// <summary>
        /// 动画总帧数.
        /// </summary>
        public int TotalFrame => Params?.Frames ?? 0;

        /// <summary>
        /// 动画帧率.
        /// </summary>
        public int Fps => Params?.Fps ?? 0;

        /// <summary>
        /// SVGA 动画大小.
        /// </summary>
        public Vector2 Size => Params == null ? Vector2.zero : new Vector2(Params.ViewBoxWidth, Params.ViewBoxHeight);

        #endregion

        public SVGADat(Stream svgaStream, ISVGAPlayerProvider provider)
        {
            Provider = provider;
            InflateSvgaFile(svgaStream);
            ParseMovieEntity();
            ParseImageOrAudio();
            PrepareAudioResources();
        }


        #region 解析数据方法

        public void InflateSvgaFile(Stream svgaFileBuffer)
        {
            // SVGALog.Log("InflateSVGAFile !!!!!!!!!!");
            byte[] inflatedBytes;

            // 微软自带的 DeflateStream 不认识文件头两个字节，SVGA 的这两字节为 78 9C，是 Deflate 的默认压缩表示字段.
            // 关于此问题请看 https://stackoverflow.com/questions/17212964/net-zlib-inflate-with-net-4-5.
            // Zlib 文件头请看 https://stackoverflow.com/questions/9050260/what-does-a-zlib-header-look-like.
            svgaFileBuffer.Seek(2, SeekOrigin.Begin);

            using (var deflatedStream = new DeflateStream(svgaFileBuffer, CompressionMode.Decompress))
            {
                using (var stream = new MemoryStream())
                {
                    deflatedStream.CopyTo(stream);
                    inflatedBytes = stream.ToArray();
                    // SVGALog.Log("长度：" + inflatedBytes.Length);
                }
            }

            InflatedBytes = inflatedBytes;
        }

        public void ParseMovieEntity()
        {
            if (InflatedBytes == null)
            {
                SVGALog.LogError("InflatedBytes 为空, 无法解析 MovieEntity");
                return;
            }

            MovieEntity = MovieEntity.Parser.ParseFrom(InflatedBytes);
        }

        public void ParseImageOrAudio()
        {
            if (Images == null)
            {
                SVGALog.LogError("MovieEntity Images 为空，可能是SVG资源");
                return;
            }

            // SVGALog.Log("Sprites 数量：" + Images.Count);
            ImagesBinaries = new Dictionary<string, byte[]>();
            AudioBinaries = new Dictionary<string, byte[]>();
            foreach (var image in Images)
            {
                var buffer = image.Value.ToByteArray();
                if (buffer == null || buffer.Length < 4)
                    continue;

                // fix: 音频文件的前 3 个字节为 ID3 或 -1 -5 -108，分别为 MP3 文件的标识.
                // fix：判断 key 是否在 Audios 中，如果在表示为音频
                if (AudioEntities.Any(audio => audio.AudioKey == image.Key))
                {
                    AudioBinaries[image.Key] = buffer;
                    continue;
                }


                // if (buffer[0] == 73 && buffer[1] == 68 && buffer[2] == 51) //ID3
                // {
                //     AudioBinaries[image.Key] = buffer;
                //     continue;
                // }
                // else if (buffer[0] == -1 && buffer[1] == -5 && buffer[2] == -108)
                // {
                //     AudioBinaries[image.Key] = buffer;
                //     continue;
                // }

                //位图
                ImagesBinaries.Add(image.Key, buffer);
            }
        }

        private string _audioOutputDir;

        public void SetAudioOutputDirectory(string baseDir)
        {
            _audioOutputDir = baseDir;
            PrepareAudioResources();
        }

        private void PrepareAudioResources()
        {
            var audioKey4Data = new Dictionary<string, SVGAAudioData>();
            FrameAudioDic = new Dictionary<int, List<SVGAAudioEntity>>();

            // 处理音频源文件
            if (AudioBinaries != null)
            {
                foreach (var kv in AudioBinaries)
                {
                    audioKey4Data[kv.Key] = new SVGAAudioData(kv.Key, kv.Value, Provider);
                }
            }

            if (AudioEntities != null)
            {
                for (int i = 0; i < AudioEntities.Count; i++)
                {
                    var ae = AudioEntities[i];
                    if (!FrameAudioDic.TryGetValue(ae.StartFrame, out var list))
                    {
                        list = new List<SVGAAudioEntity>();
                        FrameAudioDic[ae.StartFrame] = list;
                    }

                    var audioKey = ae.AudioKey;
                    if (audioKey4Data.TryGetValue(audioKey, out var data))
                    {
                        list.Add(new SVGAAudioEntity(ae, data));
                    }
                    else
                    {
                        Provider.LogWarning($"当前帧({ae.StartFrame})未找到音频数据，AudioKey:{ae.AudioKey}");
                    }
                }
            }
            audioKey4Data.Clear();
        }

        #endregion

        public byte[] GetImageBinary(string imageKey)
        {
            if (ImagesBinaries.TryGetValue(imageKey, out var binary))
            {
                return binary;
            }

            return null;
        }

        public byte[] GetAudioBinary(string audioKey)
        {
            if (AudioBinaries.TryGetValue(audioKey, out var binary))
            {
                return binary;
            }

            return null;
        }


        /// <summary>
        /// 创建 SVGADat 实例.
        /// </summary>
        /// <param name="inflatedBytes"> SVGA 文件二进制 </param>    
        /// <param name="writablePath"> 可写路径. </param>
        /// <returns></returns>
        public static SVGADat Create(byte[] inflatedBytes, string writablePath, ISVGAPlayerProvider provider)
        {
            if (inflatedBytes == null || inflatedBytes.Length == 0)
            {
                SVGALog.LogError("inflatedBytes 为空, 无法创建 SVGADat");
                return null;
            }

            using var stream = new MemoryStream(inflatedBytes, false);
            var svgaDat = new SVGADat(stream, provider);
            if (!string.IsNullOrEmpty(writablePath)) svgaDat.SetAudioOutputDirectory(writablePath);
            return svgaDat;
        }

        public void Destroy()
        {
            InflatedBytes = null;
            ImagesBinaries?.Clear();
            AudioBinaries?.Clear();
            FrameAudioDic?.Clear();
            MovieEntity?.Sprites?.Clear();
            MovieEntity?.Audios?.Clear();
            MovieEntity?.Images?.Clear();
            MovieEntity = null;
        }
    }
}