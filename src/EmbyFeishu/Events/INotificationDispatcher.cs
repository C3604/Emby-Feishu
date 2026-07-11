using System;
using EmbyFeishu.Models;

namespace EmbyFeishu.Events
{
    /// <summary>
    /// 通知调度器接口
    /// </summary>
    public interface INotificationDispatcher : IDisposable
    {
        /// <summary>
        /// 将通知事件入队
        /// </summary>
        void Enqueue(PlaybackNotificationEvent evt);

        /// <summary>
        /// 启动后台处理循环
        /// </summary>
        void Start();

        /// <summary>
        /// 停止后台处理并释放资源
        /// </summary>
        void Stop();
    }
}
