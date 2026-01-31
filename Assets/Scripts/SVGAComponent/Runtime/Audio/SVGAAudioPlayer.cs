using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.IO;

namespace Bo.SVGA
{
    public enum SVGAAudioStatus
    {
        Idle, // 刚刚进入还未开始加载
        Loading, // 正在加载
        StandBy, // 加载完成
        Play, // 正在播放
        Paused, // 暂停
        Stoped,
        PlayFinished,
        Interrupt,
    }

    public class SVGAAudioPlayer
    {
        private bool _isAudioPaused;
        private bool _isAudioPlayEnd;
        private int _loopCount;

        private float _playbackRate = 1f;

        // 标记是否需要在加载完成后自动播放
        private bool _autoPlayAfterLoad;

        private SVGAAudioEntity _svgaAudioEntity;
        private AudioClip _audioClip;

        public bool IsPaused => _isAudioPaused;
        public bool IsAudioPlayEnd => _isAudioPlayEnd;
        public int LoopCount => _loopCount;

        public bool IsAudioLoaded = false;


        /// <summary>
        /// 音频播放完成回调，如果是循环播放，单次播放后不能回调到该事件
        /// </summary>
        public Action<SVGAAudioStatus> AudioComplete;

        public AudioSource AudioSource { get; private set; }
        public AudioClip AudioClip => _audioClip;

        public float PlaybackRate
        {
            set
            {
                _playbackRate = value;
                if (AudioSource != null && AudioSource.clip != null)
                {
                    AudioSource.pitch = _playbackRate;
                }
            }
            get => _playbackRate;
        }


        public SVGAAudioPlayer(SVGAAudioEntity svgaAudioEntity, GameObject audioRoot, MonoBehaviour behaviour)
        {
            _isAudioPaused = false;
            IsAudioLoaded = false;
            _svgaAudioEntity = svgaAudioEntity;
            if (LoadAudioFromMemory())
            {
                // SVGALog.Log($"音频，同步从内存中添加成功，AudioKey:{svgaAudioEntity.AudioEntity.AudioKey}");
                IsAudioLoaded = true;
                return;
            }

            LoadAudioClipFromDisk(behaviour, clip =>
            {
                // SVGALog.Log($"音频(AudioKey:{_svgaAudioEntity.AudioEntity.AudioKey})。异步从文件中添加成功，clip={clip != null}");
                if (clip == null)
                {
                    SVGALog.LogError(
                        $"音频(AudioKey:{_svgaAudioEntity.AudioEntity.AudioKey})。从内存/文件中加载音频事变, Path:{_svgaAudioEntity.SvgaAudioData.Path}");
                    return;
                }

                // 添加 AudioSource
                InitAudioSource(audioRoot);
                IsAudioLoaded = true;

                // 如果在加载过程中请求了播放，则立即播放
                if (_autoPlayAfterLoad && !_isAudioPaused)
                {
                    SVGALog.LogWarning($"音频(AudioKey:{_svgaAudioEntity.AudioEntity.AudioKey})。音频加载完成，执行延迟播放");
                    Play();
                }
            });
        }


        /// <summary>
        /// 从内存中的二进制数据加载播放
        /// </summary>
        private bool LoadAudioFromMemory()
        {
            var bin = _svgaAudioEntity.SvgaAudioData.Binary;
            if (bin == null || bin.Length <= 0)
                return false;
            var clip = SVGAAudioUtils.TryCreateClipFromWavBytes(bin);
            if (clip == null) return false;
            _audioClip = clip;
            return true;
        }

        /// <summary>
        /// 从磁盘加载音频文件
        /// </summary>
        /// <param name="monoBehaviour"></param>
        /// <param name="complete"></param>
        private void LoadAudioClipFromDisk(MonoBehaviour monoBehaviour, Action<AudioClip> complete)
        {
            var path = _svgaAudioEntity.SvgaAudioData.Path;
            if (string.IsNullOrEmpty(path)) return;
            // 从磁盘加载音频文件
            monoBehaviour.StartCoroutine(LoadAudioClipFromDiskCoroutine(path, complete));
        }

        /// <summary>
        /// 协程从磁盘加载音频文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="complete"></param>
        /// <returns></returns>
        private IEnumerator LoadAudioClipFromDiskCoroutine(string path, Action<AudioClip> complete)
        {
            var url = path.StartsWith("http") ? path : "file://" + path;
            var req = UnityWebRequestMultimedia.GetAudioClip(url, _svgaAudioEntity.SvgaAudioData.AudioType);
            yield return req.SendWebRequest();

            if (req.isDone && req.result != UnityWebRequest.Result.ProtocolError)
            {
                var downloadHandler = (DownloadHandlerAudioClip)req.downloadHandler;
                if (downloadHandler != null)
                {
                    _audioClip = downloadHandler.audioClip;
                }

                complete?.Invoke(_audioClip);
            }
            else
            {
                complete?.Invoke(null);
            }
        }

        private void InitAudioSource(GameObject audioRoot)
        {
            if (AudioSource == null)
            {
                AudioSource = audioRoot.AddComponent<AudioSource>();
            }

            ResetAudioSource();
        }


        public void OnUpdate()
        {
            if (AudioSource == null || AudioSource.clip == null || AudioSource.loop)
                return;

            if (!AudioSource.isPlaying && !_isAudioPlayEnd && !_isAudioPaused)
            {
                _isAudioPlayEnd = true;
                AudioComplete?.Invoke(SVGAAudioStatus.PlayFinished);
            }
        }


        public void ResetAudioSource()
        {
            if (AudioSource == null || _audioClip == null) return;
            var src = AudioSource;
            src.clip = _audioClip;
            src.pitch = _playbackRate;
            src.playOnAwake = false;
            var duration = _audioClip.length;
            if (_svgaAudioEntity.AudioEntity.StartTime > 0)
            {
                var seconds = _svgaAudioEntity.AudioEntity.StartTime / 1000f;
                src.time = Mathf.Clamp(seconds, 0f, duration);
                duration = src.time;
            }
        }

        public void Play()
        {
            if (!IsAudioLoaded)
            {
                SVGALog.LogWarning($"音频(AudioKey:{_svgaAudioEntity.AudioEntity.AudioKey})。还未加载完成，标记为完成后自动播放");
                _autoPlayAfterLoad = true;
                _isAudioPaused = false;
                return;
            }

            if (AudioSource == null || AudioSource.clip == null)
                return;

            AudioSource.Play();
            _isAudioPaused = false;
            _autoPlayAfterLoad = false;
        }

        public void Pause()
        {
            _autoPlayAfterLoad = false;
            if (AudioSource == null || AudioSource.clip == null)
                return;

            AudioSource.Pause();
            _isAudioPaused = true;
        }

        public void Resume()
        {
            if (AudioSource == null || AudioSource.clip == null)
                return;

            AudioSource.UnPause();
            _isAudioPaused = false;
        }

        public void Stop()
        {
            _autoPlayAfterLoad = false;
            if (AudioSource == null || AudioSource.clip == null)
                return;

            AudioSource.Stop();
            _isAudioPaused = false;
            _isAudioPlayEnd = true;
            AudioComplete?.Invoke(SVGAAudioStatus.Interrupt);
        }

        public void OnDestroy()
        {
            if (AudioSource == null) return;
            SVGALog.Log($"音频(AudioKey:{_svgaAudioEntity.AudioEntity.AudioKey})。SVGAAudioPlayer OnDestroy");
            try
            {
                if (AudioSource.isPlaying)
                {
                    AudioSource.Stop();
                }

                AudioSource.clip = null;
            }
            catch (Exception e)
            {
                SVGALog.LogError(
                    $"音频(AudioKey:{_svgaAudioEntity.AudioEntity.AudioKey})。SVGAAudioPlayer OnDestroy Error:{e.Message}");
            }

            GameObject.Destroy(AudioSource);
            AudioSource = null;
        }
    }
}