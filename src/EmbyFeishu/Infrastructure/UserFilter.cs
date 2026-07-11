using System;
using System.Collections.Generic;
using System.Linq;
using EmbyFeishu.Models;

namespace EmbyFeishu.Infrastructure
{
    /// <summary>
    /// 用户过滤器
    /// </summary>
    public static class UserFilter
    {
        /// <summary>
        /// 判断指定用户是否应该发送通知
        /// </summary>
        public static bool ShouldNotify(string userName, UserFilterMode mode, string userNamesList)
        {
            if (mode == UserFilterMode.All)
                return true;

            var isUnknown = string.IsNullOrWhiteSpace(userName);
            var normalizedUser = isUnknown ? "" : userName.Trim();

            var configuredNames = ParseUserNames(userNamesList);

            if (mode == UserFilterMode.IncludeOnly)
            {
                if (isUnknown) return false;
                return configuredNames.Contains(normalizedUser, StringComparer.OrdinalIgnoreCase);
            }

            if (mode == UserFilterMode.Exclude)
            {
                if (isUnknown) return true;
                return !configuredNames.Contains(normalizedUser, StringComparer.OrdinalIgnoreCase);
            }

            return true;
        }

        private static List<string> ParseUserNames(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new List<string>();

            return raw.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.Trim())
                .Where(n => n.Length > 0)
                .ToList();
        }
    }
}
