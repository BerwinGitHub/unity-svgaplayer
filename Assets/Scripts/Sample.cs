using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Bo.SVGA;

public class Sample : MonoBehaviour
{

    public Button btnPlay;
    public Button btnPause;
    public Button btnStop;
    public Button btnSeekAdd;
    public Button btnSeekReduce;
    public Button btnSeek;
    public Button btnRate;
    public Text txtTitle;
    public InputField ifFrameIdx;
    public InputField ifPlaybackRate;
    public Toggle tglLoop;

    #region ScrollView

    public ScrollRect scrollRect;
    public GameObject itemPrefab;

    #endregion

    private SVGAPlayer svgPlayer;
    private SVGAPlayerProvider svgPlayerProvider;

    private void Awake()
    {
        svgPlayerProvider = new SVGAPlayerProvider(this);
        LoadSvgaFiles();
    }

    /// <summary>
    /// 加载SVGA文件
    /// 从 StreamingAssetsPath/svga-files 中加载所有 svga 文件并显示到 scrollRect 中
    /// </summary>
    private void LoadSvgaFiles()
    {
        var svgaFiles = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, "svga-files"), "*.svga");
        var i = 0;
        foreach (var svgaFile in svgaFiles)
        {
            CreateListItem(i++, svgaFile);
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (svgPlayer != null && svgPlayer.IsPlaying)
        {
            var idx = svgPlayer.GetCurrentFrame();
            ifFrameIdx.text = idx + "";
        }

    }

    public void SVGAPlay()
    {
        if (svgPlayer == null) return;
        svgPlayer?.Play();
        RefreshOptionsUI();
    }

    public void SVGAPause()
    {
        if (svgPlayer == null) return;
        svgPlayer?.Pause();
        RefreshOptionsUI();
    }

    public void SVGAStop()
    {
        if (svgPlayer == null) return;
        svgPlayer?.Stop();
        RefreshOptionsUI();
    }

    public void SVGASeek()
    {
        if (svgPlayer == null) return;
        svgPlayer?.Seek(int.Parse(ifFrameIdx.text));
    }

    public void SVGASeekNext(float time)
    {
        if (svgPlayer == null) return;
        var idx = svgPlayer.GetCurrentFrame();
        idx = ++idx >= svgPlayer.Data.TotalFrame ? 0 : idx;
        svgPlayer.Seek(idx);
    }

    public void SVGASeekPrev(float time)
    {
        if (svgPlayer == null) return;
        var idx = svgPlayer.GetCurrentFrame();
        idx = --idx < 0 ? svgPlayer.Data.TotalFrame - 1 : idx;
        svgPlayer.Seek(idx);
    }

    public void SVGARate()
    {
        if (svgPlayer == null) return;
        svgPlayer?.SetPlaybackRate(float.Parse(ifPlaybackRate.text));
    }

    public void SVGALoop(bool value)
    {
        if (svgPlayer == null) return;
        svgPlayer?.SetLoop(tglLoop.isOn);
    }

    private void RefreshOptionsUI()
    {
        btnPlay.interactable = svgPlayer != null && !svgPlayer.IsPlaying;
        btnStop.interactable = svgPlayer != null && svgPlayer.IsPlaying;
        btnPause.interactable = svgPlayer != null && svgPlayer.IsPlaying;
        btnSeek.interactable = svgPlayer != null && !svgPlayer.IsPlaying;
        ifFrameIdx.interactable = svgPlayer != null && !svgPlayer.IsPlaying;
        btnSeekAdd.interactable = svgPlayer != null && !svgPlayer.IsPlaying;
        btnSeekReduce.interactable = svgPlayer != null && !svgPlayer.IsPlaying;
    }

    private GameObject CreateListItem(int i, string fullPath)
    {
        var foldername = System.IO.Path.GetDirectoryName(fullPath);
        var filename = System.IO.Path.GetFileName(fullPath);

        var item = Instantiate(itemPrefab, scrollRect.content);
        item.transform.SetParent(scrollRect.content, false);
        item.SetActive(true);
        item.transform.Find("txtFolder").GetComponent<Text>().text = foldername;
        item.transform.Find("txtName").GetComponent<Text>().text = $"#{i + 1} - {filename}";
        var btnLoad = item.transform.Find("Options/btnLoad").GetComponent<Button>();
        btnLoad.onClick.AddListener(() =>
        {
            var _i = i;
            var _fullPath = fullPath;
            if (!File.Exists(_fullPath))
            {
                Debug.LogError("文件不存在：" + _fullPath);
                return;
            }

            LoadAndPlaySvga(_i, _fullPath);
        });
        return item;
    }

    private void LoadAndPlaySvga(int index, string fullPath)
    {
        txtTitle.text = $"#{index + 1} - {Path.GetFileName(fullPath)}";
        if (svgPlayer != null)
        {
            DestroyImmediate(svgPlayer.gameObject);
        }

        if (!File.Exists(fullPath)) return;
        var go = new GameObject("SVGAPlayer");
        var parent = transform.Find("SVGAContainer").transform;
        go.transform.SetParent(parent, false);
        go.transform.SetAsFirstSibling();
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        svgPlayer = go.AddComponent<SVGAPlayer>();
        using var fs = File.OpenRead(fullPath);
        svgPlayer.LoadFromStream(fs, svgPlayerProvider);
        svgPlayer.Seek(0);
        svgPlayer.AddClickCallback((imageKey) =>
        {
            Debug.Log($"Click: {imageKey}");
        });
        // svgPlayer.AddClickCallback((imageKey) =>
        // {
        //     Debug.Log($"Click: {imageKey}");
        // }, "chetou");
        // 替换为字体
        if (fullPath.Contains("4_correct_double"))
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            svgPlayer.AddText("img_519", "3", font, 80, Color.white, new Vector3(-100, 0, 0));
        }
        ifPlaybackRate.text = (svgPlayer != null ? svgPlayer.GetPlaybackRate() : 0) + "";
        // svgPlayer.SetFps(60);
        svgPlayer.OnCompleted += (player, completedCount) =>
        {
            Debug.Log($"OnCompleted: {completedCount}");
            if (!player.Loop)
            {
                ResetUIControls();
            }
        };
        ResetUIControls();
    }



    private void ResetUIControls()
    {
        bool playing = svgPlayer != null && svgPlayer.IsPlaying;
        if (btnPlay != null) btnPlay.interactable = !playing;
        if (btnStop != null) btnStop.interactable = playing;
        if (btnPause != null) btnPause.interactable = playing;
        if (btnSeek != null) btnSeek.interactable = !playing;
        btnSeekAdd.interactable = !playing;
        btnSeekReduce.interactable = !playing;
        if (ifFrameIdx != null)
        {
            ifFrameIdx.interactable = !playing;
            ifFrameIdx.text = "0";
        }
    }
}