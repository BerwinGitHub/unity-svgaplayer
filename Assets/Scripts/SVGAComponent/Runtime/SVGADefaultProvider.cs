using UnityEngine;

namespace Bo.SVGA
{
    public class SVGADefaultProvider : ISVGAPlayerProvider
    {
        public string GetWritablePath()
        {
            return Application.persistentDataPath;
        }

        public MonoBehaviour GetMonoBehaviour()
        {
            return null;
        }


        public void Log(string message)
        {
            Debug.Log(message);
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
}