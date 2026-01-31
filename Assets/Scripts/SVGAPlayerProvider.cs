using System.IO;
using UnityEngine;
using Bo.SVGA;

class SVGAPlayerProvider : ISVGAPlayerProvider
{

    private MonoBehaviour _monoBehaviour;
    
    public SVGAPlayerProvider(MonoBehaviour monoBehaviour)
    {
        _monoBehaviour = monoBehaviour;
    }

    public string GetWritablePath()
    {
        return Path.Combine(Application.persistentDataPath, "svga-root");
    }

    public MonoBehaviour GetMonoBehaviour()
    {
        return _monoBehaviour;
    }

    public void Log(string msg)
    {
        Debug.Log(msg);
    }

    public void LogError(string message)
    {
        Debug.LogError(message);
    }

    public void LogWarning(string message)
    {
        Debug.LogWarning(message);
    }
}