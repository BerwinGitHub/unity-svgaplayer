using UnityEngine;

namespace Bo.SVGA
{
    public static class SVGAAudioUtils
    {
        /// <summary>
        /// 创建音频
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static AudioClip TryCreateClipFromWavBytes(byte[] bytes)
        {
            if (bytes.Length < 44) return null;
            if (bytes[0] != (byte)'R' || bytes[1] != (byte)'I' || bytes[2] != (byte)'F' || bytes[3] != (byte)'F') return null;
            if (bytes[8] != (byte)'W' || bytes[9] != (byte)'A' || bytes[10] != (byte)'V' || bytes[11] != (byte)'E') return null;
            int pos = 12;
            int audioFormat = 0;
            int channels = 0;
            int sampleRate = 0;
            int bitsPerSample = 0;
            int dataOffset = 0;
            int dataSize = 0;
            while (pos + 8 <= bytes.Length)
            {
                int chunkId = bytes[pos] | (bytes[pos + 1] << 8) | (bytes[pos + 2] << 16) | (bytes[pos + 3] << 24);
                int chunkSize = bytes[pos + 4] | (bytes[pos + 5] << 8) | (bytes[pos + 6] << 16) | (bytes[pos + 7] << 24);
                pos += 8;
                if (chunkId == 0x20746d66)
                {
                    audioFormat = bytes[pos] | (bytes[pos + 1] << 8);
                    channels = bytes[pos + 2] | (bytes[pos + 3] << 8);
                    sampleRate = bytes[pos + 4] | (bytes[pos + 5] << 8) | (bytes[pos + 6] << 16) | (bytes[pos + 7] << 24);
                    bitsPerSample = bytes[pos + 14] | (bytes[pos + 15] << 8);
                }
                else if (chunkId == 0x61746164)
                {
                    dataOffset = pos;
                    dataSize = chunkSize;
                    break;
                }
                pos += chunkSize;
            }
            if (audioFormat != 1) return null;
            if (bitsPerSample != 16) return null;
            if (dataOffset <= 0 || dataSize <= 0) return null;
            int sampleCount = dataSize / 2;
            var samples = new float[sampleCount];
            int si = 0;
            for (int i = 0; i < dataSize; i += 2)
            {
                short s = (short)(bytes[dataOffset + i] | (bytes[dataOffset + i + 1] << 8));
                samples[si++] = s / 32768f;
            }
            int lengthSamples = sampleCount / channels;
            var clip = AudioClip.Create("svga-wav", lengthSamples, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioType GetAudioType(byte[] audioData)
        {
            if (audioData == null || audioData.Length < 4) return AudioType.UNKNOWN;
            if (audioData.Length >= 12 &&
                audioData[0] == (byte)'R' && audioData[1] == (byte)'I' && audioData[2] == (byte)'F' && audioData[3] == (byte)'F' &&
                audioData[8] == (byte)'W' && audioData[9] == (byte)'A' && audioData[10] == (byte)'V' && audioData[11] == (byte)'E')
                return AudioType.WAV;
            if (audioData[0] == (byte)'O' && audioData[1] == (byte)'g' && audioData[2] == (byte)'g' && audioData[3] == (byte)'S')
                return AudioType.OGGVORBIS;
            if (audioData[0] == (byte)'I' && audioData[1] == (byte)'D' && audioData[2] == (byte)'3')
                return AudioType.MPEG;
            byte b0 = audioData[0];
            byte b1 = audioData[1];
            if (b0 == 0xFF && (b1 == 0xFB || b1 == 0xF3 || b1 == 0xF2))
                return AudioType.MPEG;
            if (b0 == 0xFF && ((b1 & 0xF0) == 0xF0))
                return AudioType.ACC
                    ;
            return AudioType.UNKNOWN;
        }

        public static string GetAudioTypeString(AudioType audioType)
        {
            switch (audioType)
            {
                case AudioType.WAV: return "wav";
                case AudioType.OGGVORBIS: return "oggvorbis";
                case AudioType.MPEG: return "mpeg";
                case AudioType.ACC: return "acc";
                default: return "unknown";
            }
        }
    }
}
