using System;
using System.Threading;

namespace EmbyFeishu.Events
{
    /// <summary>
    /// 通知发送统计。线程安全，供诊断面板展示。
    /// </summary>
    public class NotificationStatistics
    {
        private long _totalSent;
        private long _totalFailed;
        private long _totalRetried;
        private long _totalDropped;
        private volatile int _currentQueueSize;
        private volatile int _peakQueueSize;
        private long _consecutiveFailures;
        private DateTime _lastSendTime = DateTime.MinValue;
        private DateTime _lastSuccessTime = DateTime.MinValue;
        private DateTime _lastFailureTime = DateTime.MinValue;
        private DateTime _startTime = DateTime.Now;

        public long TotalSent => Interlocked.Read(ref _totalSent);
        public long TotalFailed => Interlocked.Read(ref _totalFailed);
        public long TotalRetried => Interlocked.Read(ref _totalRetried);
        public long TotalDropped => Interlocked.Read(ref _totalDropped);
        public int CurrentQueueSize => _currentQueueSize;
        public int PeakQueueSize => _peakQueueSize;
        public long ConsecutiveFailures => Interlocked.Read(ref _consecutiveFailures);
        public DateTime LastSendTime => _lastSendTime;
        public DateTime LastSuccessTime => _lastSuccessTime;
        public DateTime LastFailureTime => _lastFailureTime;

        public void RecordSent()
        {
            Interlocked.Increment(ref _totalSent);
            Interlocked.Exchange(ref _consecutiveFailures, 0);
            _lastSendTime = DateTime.Now;
            _lastSuccessTime = DateTime.Now;
        }

        public void RecordFailed()
        {
            Interlocked.Increment(ref _totalFailed);
            Interlocked.Increment(ref _consecutiveFailures);
            _lastSendTime = DateTime.Now;
            _lastFailureTime = DateTime.Now;
        }

        public void RecordRetry()
        {
            Interlocked.Increment(ref _totalRetried);
        }

        public void RecordDropped()
        {
            Interlocked.Increment(ref _totalDropped);
        }

        public void UpdateQueueSize(int size)
        {
            _currentQueueSize = size;
            if (size > _peakQueueSize)
                _peakQueueSize = size;
        }

        /// <summary>
        /// 返回 Webhook 健康状态描述。
        /// </summary>
        public string GetHealthStatus()
        {
            var consecutive = ConsecutiveFailures;
            if (TotalSent == 0 && TotalFailed == 0)
                return "未发送过";
            if (consecutive >= 5)
                return "不健康（连续 " + consecutive + " 次失败）";
            if (consecutive >= 3)
                return "异常（连续 " + consecutive + " 次失败）";
            if (consecutive > 0)
                return "偶有失败（连续 " + consecutive + " 次）";
            return "健康";
        }

        /// <summary>
        /// 生成诊断摘要文本，显示在配置页。
        /// </summary>
        public string GetDiagnosticSummary()
        {
            var uptime = DateTime.Now - _startTime;
            var uptimeStr = uptime.TotalHours >= 1
                ? string.Format("{0:F1} 小时", uptime.TotalHours)
                : string.Format("{0} 分钟", (int)uptime.TotalMinutes);

            var parts = new System.Collections.Generic.List<string>
            {
                "运行时长：" + uptimeStr,
                "成功：" + TotalSent + "，失败：" + TotalFailed + "，重试：" + TotalRetried + "，丢弃：" + TotalDropped,
                "队列：" + CurrentQueueSize + "（峰值 " + PeakQueueSize + "）",
                "连接状态：" + GetHealthStatus()
            };

            if (_lastSuccessTime > DateTime.MinValue)
                parts.Add("最后成功：" + _lastSuccessTime.ToString("HH:mm:ss"));
            if (_lastFailureTime > DateTime.MinValue)
                parts.Add("最后失败：" + _lastFailureTime.ToString("HH:mm:ss"));

            return string.Join("\n", parts);
        }
    }
}
