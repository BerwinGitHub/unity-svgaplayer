using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
namespace Bo.SVGA
{
    public class SVGAPlayer : MonoBehaviour, IPointerClickHandler
    {
        #region Public

        public static MonoBehaviour GlobalMonoBehaviour { get; set; } = null;

        /// <summary>
        /// 是否正在播放
        /// </summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// 获取当前播放的帧率
        /// </summary>
        public float GetFps() => _fps;

        /// <summary>
        /// 是否循环播放
        /// </summary>
        public bool Loop = true;

        /// <summary>
        /// 当前播放的帧索引
        /// </summary>
        public int FrameIndex = 0;

        /// <summary>
        /// 播放完成回调
        /// </summary>
        public Action<SVGAPlayer, int> OnCompleted;

        public static ISVGAPlayerProvider Provider;

        #endregion

        public SVGADat Data { get; private set; }
        public RectTransform RectTransform { get; private set; }
        private float _frameTime;
        private float _acc;
        private SVGASpriteEntity[] _sprites;
        private bool _isPlaying = false;
        private SVGAFrameCache _frameCache;
        private int _completedCount = 0;
        private float _fps;

        private float _playbackRate = 1f;
        private bool _isDestroying = false;

        // private List<AudioSource> _audioSources;
        // private Dictionary<string, AudioClip> _clipCache;
        // private HashSet<string> _loadingAudio;
        private Dictionary<int, List<SVGAAudioPlayer>> _svgaAudioPlayers;

        private Dictionary<string, List<Action<string>>> _clickCallbacks =
            new Dictionary<string, List<Action<string>>>();

        public void LoadFromBytes(byte[] bytes, ISVGAPlayerProvider provider)
        {
            Provider = provider;
            SVGALog.SetProvider(provider);
            GlobalMonoBehaviour = provider.GetMonoBehaviour();
            using var stream = new MemoryStream(bytes);
            Data = new SVGADat(stream, provider);
            Build();
        }

        public void LoadFromStream(Stream stream, ISVGAPlayerProvider provider)
        {
            Provider = provider;
            SVGALog.SetProvider(provider);
            GlobalMonoBehaviour = provider.GetMonoBehaviour();
            Data = new SVGADat(stream, provider);
            Build();
        }

        private void Build()
        {
            RectTransform = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            var size = Data.Size;
            RectTransform.sizeDelta = size;
            var sprites = Data.Sprites;
            _sprites = new SVGASpriteEntity[sprites.Count];
            for (int i = 0; i < sprites.Count; i++)
            {
                var se = sprites[i];
                var type = SpriteEntityType.Image;
                if (se.Frames != null)
                {
                    bool hasVector = false;
                    for (int f = 0; f < se.Frames.Count && !hasVector; f++)
                    {
                        var fr = se.Frames[f];
                        var shapes = fr.Shapes;
                        if (shapes != null)
                        {
                            for (int s = 0; s < shapes.Count; s++)
                            {
                                var st = shapes[s].Type;
                                if (st != ShapeEntity.Types.ShapeType.Keep)
                                {
                                    hasVector = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (hasVector) type = SpriteEntityType.Shape;
                }

                _sprites[i] = SVGASpriteEntity.Create(type, Data, se, transform);
                _sprites[i].OnClick = OnPointerClick;
            }

            FrameIndex = 0;
            _acc = 0;
            _frameCache = new SVGAFrameCache();
            _frameCache.Preload(Data);
            ApplyFrame(FrameIndex);
            _isPlaying = false;
            _completedCount = 0;
            _fps = Data != null && Data.Fps > 0 ? Data.Fps : 30f;
            _frameTime = 1f / _fps;
            // preload audio
            LoadAudios();
            // 禁用点击区域
            SetRaycastTarget(false);
        }

        private void LoadAudios()
        {
            // if (!Data.FrameAudioDic.TryGetValue(frame, out var list)) return;
            // 遍历 Data.FrameAudioDic 中的所有音频
            _svgaAudioPlayers = new Dictionary<int, List<SVGAAudioPlayer>>();
            foreach (var pair in Data.FrameAudioDic)
            {
                var frame = pair.Key;
                var list = pair.Value;
                foreach (var audioEntity in list)
                {
                    var player = new SVGAAudioPlayer(audioEntity, this.gameObject, GlobalMonoBehaviour);
                    if (!_svgaAudioPlayers.TryGetValue(frame, out var players))
                    {
                        players = new List<SVGAAudioPlayer>();
                        _svgaAudioPlayers[frame] = players;
                    }

                    players.Add(player);
                }
            }
        }

        public void Update()
        {
            if (Data == null || _isDestroying || !_isPlaying) return;
            _acc += Time.deltaTime * _playbackRate;
            while (_acc >= _frameTime)
            {
                _acc -= _frameTime;
                FrameIndex++;
                if (FrameIndex >= Data.TotalFrame) // 结束
                {
                    if (!Loop) // 不循环直接结束播放
                    {
                        FrameIndex = 0;
                        _isPlaying = false;
                        ApplyFrame(Data.TotalFrame - 1);
                        _completedCount++;
                        OnCompleted?.Invoke(this, _completedCount);
                        break;
                    }

                    FrameIndex = 0;
                    _completedCount++;
                    OnCompleted?.Invoke(this, _completedCount);
                }
                if (!_isPlaying) return;
                ApplyFrame(FrameIndex);
                TryPlayAudiosForFrame(FrameIndex);
            }
        }

        private void ApplyFrame(int frame)
        {
            if (Data == null || _isDestroying) return;
            var sprites = Data.Sprites;
            if (sprites == null || _sprites == null) return;
            for (int i = 0; i < sprites.Count && i < _sprites.Length; i++)
            {
                var frames = sprites[i].Frames;
                FrameEntity fe = null;
                if (frame < frames.Count)
                {
                    fe = frames[frame];
                }

                var pre = _frameCache?.GetMeshes(i, frame);
                _sprites[i].ApplyFrame(fe, pre, i, frame, sprites[i].ImageKey);
            }
        }

        public void Seek(int frame)
        {
            if (Data == null || _isDestroying) return;
            FrameIndex = Mathf.Clamp(frame, 0, Data.TotalFrame > 0 ? Data.TotalFrame - 1 : 0);
            _acc = 0f;
            ApplyFrame(FrameIndex);
        }

        public void SetLoop(bool loop)
        {
            Loop = loop;
        }

        public void SetFps(float fps)
        {
            _fps = fps > 0f ? fps : 30f;
            _frameTime = 1f / _fps;
            _acc = 0f;
        }

        public void ResetFpsToData()
        {
            var target = Data != null && Data.Fps > 0 ? Data.Fps : 30f;
            _fps = target;
            _frameTime = 1f / _fps;
            _acc = 0f;
        }

        public void Play()
        {
            if (Data == null || _isDestroying) return;
            _isPlaying = true;
            ResumeAllAudio();
            TryPlayAudiosForFrame(FrameIndex);
        }

        /// <summary>
        /// 兼容保持上个版本 SVGA 播放接口一致性，方便业务最小改动，但是不推荐使用该方法播放最新SVGA<br/>
        /// 推荐使用 SVGAPlayer 相关接口能力直接调用，如该方法内实现一样
        /// </summary>
        /// <param name="startFrame">开始播放的帧索引，默认从 0 开始</param>
        /// <param name="loopCnt">循环播放次数，0:表示无限循环，-1 应该表示不播放?</param>
        /// <param name="complete">播放完成回调</param>
        public void Play(int startFrame, int loopCnt, Action complete)
        {
            Seek(startFrame);
            SetLoop(true);
            OnCompleted = (_, i) =>
            {
                if (loopCnt == 0) return; // 循环播放
                if (i != loopCnt) return;
                Stop();
                complete?.Invoke();
            };
            if (loopCnt != -1)
            {
                Play();
            }
        }


        public void Pause()
        {
            if (_isDestroying) return;
            _isPlaying = false;
            PauseAllAudio();
        }

        public void Stop()
        {
            if (_isDestroying) return;
            _isPlaying = false;
            // Seek(0); // 有些动画只播放一遍，需要显示到最后一帧
            _completedCount = 0;
            StopAllAudio();
        }

        public void SetRaycastTarget(bool enable)
        {
            if (_sprites == null) return;
            foreach (var sprite in _sprites)
            {
                sprite.SetRaycastTarget(enable);
            }
        }

        /// <summary>
        /// 添加点击回调，点击指定 imageKey 区域时触发，如果 imageKey 为空，则为根节点点击区域
        /// </summary>
        /// <param name="complete"></param>
        /// <param name="imageKey">需要点击的区域名称，* 为所有节点区域</param>
        public void AddClickCallback(Action<string> complete, string imageKey = "*")
        {
            if (!_clickCallbacks.TryGetValue(imageKey, out var callbacks))
            {
                callbacks = new List<Action<string>>();
                _clickCallbacks[imageKey] = callbacks;
            }

            callbacks.Add(complete);
            if (imageKey.Equals("*"))
            {
                SetRaycastTarget(true);
            }
            else if (_sprites != null) // 设置点击区域为可交互
            {
                foreach (var sprite in _sprites)
                {
                    if (sprite != null && sprite.ImageKey.Equals(imageKey))
                    {
                        sprite.SetRaycastTarget(true);
                        break;
                    }
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_clickCallbacks.Count <= 0) return;
            // 触发全局点击回调
            _clickCallbacks.TryGetValue("*", out var rootCallbacks);

            // 尝试查找点击的具体 Sprite
            var target = eventData.pointerPress;
            if (target != null && _sprites != null)
            {
                foreach (var sprite in _sprites)
                {
                    if (sprite != null && sprite.gameObject == target)
                    {
                        if (!string.IsNullOrEmpty(sprite.ImageKey))
                        {
                            if (rootCallbacks != null)
                                foreach (var callback in rootCallbacks)
                                    callback?.Invoke(sprite.ImageKey);
                            if (_clickCallbacks.TryGetValue(sprite.ImageKey, out var callbacks))
                                foreach (var callback in callbacks)
                                    callback?.Invoke(sprite.ImageKey);
                        }

                        break;
                    }
                }
            }
        }

        public void OnDestroy()
        {
            _isDestroying = true;
            SVGALog.Log($"SVGAPlayer OnDestroy");
            Stop();
            if (_svgaAudioPlayers != null)
            {
                foreach (var pair in _svgaAudioPlayers)
                {
                    var players = pair.Value;
                    for (int i = 0; i < players.Count; i++)
                    {
                        var player = players[i];
                        player?.OnDestroy();
                    }
                }
            }

            _svgaAudioPlayers?.Clear();
            // 释放绘制相关缓存数据
            _frameCache?.Destroy();
            // 释放 SVGADat 中的相关缓存数据
            Data?.Destroy();
            // 释放监听回调
            _clickCallbacks?.Clear();
        }

        public void SetPlaybackRate(float rate)
        {
            _playbackRate = rate > 0f ? rate : 0.01f;
            if (_svgaAudioPlayers != null)
            {
                foreach (var pair in _svgaAudioPlayers)
                {
                    var players = pair.Value;
                    for (int i = 0; i < players.Count; i++)
                    {
                        var player = players[i];
                        if (player != null) player.PlaybackRate = _playbackRate;
                    }
                }
            }
        }

        public float GetPlaybackRate()
        {
            return _playbackRate;
        }

        public int GetCurrentFrame()
        {
            return FrameIndex;
        }

        public void SetAudioOutputDirectory(string baseDir)
        {
            if (Data == null) return;
            Data.SetAudioOutputDirectory(baseDir);
        }

        private void TryPlayAudiosForFrame(int frame)
        {
            if (!_svgaAudioPlayers.TryGetValue(frame, out var players)) return;
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                player?.Play();
            }
        }

        private void PauseAllAudio()
        {
            if (_svgaAudioPlayers == null) return;
            foreach (var pair in _svgaAudioPlayers)
            {
                var players = pair.Value;
                for (int i = 0; i < players.Count; i++)
                {
                    var player = players[i];
                    player?.Pause();
                }
            }
        }

        private void ResumeAllAudio()
        {
            if (_svgaAudioPlayers == null) return;
            foreach (var pair in _svgaAudioPlayers)
            {
                var players = pair.Value;
                for (int i = 0; i < players.Count; i++)
                {
                    var player = players[i];
                    if (player == null) continue;
                    if (player.AudioClip == null) continue;
                    if (player.IsPaused) player.Resume();
                }
            }
        }

        private void StopAllAudio()
        {
            if (_svgaAudioPlayers == null) return;
            foreach (var pair in _svgaAudioPlayers)
            {
                var players = pair.Value;
                for (int i = 0; i < players.Count; i++)
                {
                    var player = players[i];
                    player?.Stop();
                }
            }
        }

        public Text AddText(string imageKey, string text, Font font, int fontSize, Color color, Vector3 offset)
        {
            if (string.IsNullOrEmpty(imageKey)) return null;
            if (_sprites == null) return null;
            foreach (var sprite in _sprites)
            {
                if (sprite != null && sprite.ImageKey.Equals(imageKey))
                {
                    return sprite.AddText(text, font, fontSize, color, offset);
                }
            }
            return null;
        }

        public int GetFrameCount()
        {
            return Data?.TotalFrame ?? 0;
        }
    }
}
