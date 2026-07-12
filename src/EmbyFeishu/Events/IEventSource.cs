using System;

namespace EmbyFeishu.Events
{
    /// <summary>
    /// 事件源：负责订阅一类 Emby 事件并转换为统一通知事件。
    /// Start 必须幂等；Stop/Dispose 必须完整解除订阅。
    /// </summary>
    public interface IEventSource : IDisposable
    {
        /// <summary>事件源名称（用于日志）</summary>
        string Name { get; }

        /// <summary>开始订阅（幂等）</summary>
        void Start();

        /// <summary>停止订阅</summary>
        void Stop();
    }
}
