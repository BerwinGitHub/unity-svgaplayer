using UnityEngine;

namespace Bo.SVGA
{
    public interface ISVGAPlayerProvider
    {
        /// <summary>
        /// 获取可写路径
        /// </summary>
        /// <returns></returns>
        string GetWritablePath();

        /// <summary>
        /// 获取 MonoBehaviour
        /// </summary>
        /// <returns></returns>
        MonoBehaviour GetMonoBehaviour();


        #region 日志输出相关

        /// <summary>
        /// 日志输出
        /// </summary>
        /// <param name="message"></param>
        void Log(string message);

        /// <summary>
        /// 错误日志输出
        /// </summary>
        /// <param name="message"></param>
        void LogError(string message);

        /// <summary>
        /// 警告日志输出
        /// </summary>
        /// <param name="message"></param>  
        void LogWarning(string message);

        #endregion
    }
}
