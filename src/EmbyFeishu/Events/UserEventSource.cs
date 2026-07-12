using System;
using EmbyFeishu.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Events;

namespace EmbyFeishu.Events
{
    /// <summary>
    /// 用户管理与安全事件源。密码修改仅提示发生变化，绝不含密码值。
    /// </summary>
    public class UserEventSource : IEventSource
    {
        private readonly IUserManager _userManager;
        private readonly NotificationContext _ctx;
        private bool _started;

        public string Name => "User";

        public UserEventSource(IUserManager userManager, NotificationContext ctx)
        {
            _userManager = userManager;
            _ctx = ctx;
        }

        public void Start()
        {
            if (_started) return;
            _started = true;
            _userManager.UserCreated += OnUserCreated;
            _userManager.UserDeleted += OnUserDeleted;
            _userManager.UserUpdated += OnUserUpdated;
            _userManager.UserLockedOut += OnUserLockedOut;
            _userManager.UserPasswordChanged += OnUserPasswordChanged;
            _userManager.UserPolicyUpdated += OnUserPolicyUpdated;
            _userManager.UserConfigurationUpdated += OnUserConfigurationUpdated;
        }

        public void Stop()
        {
            if (!_started) return;
            _started = false;
            _userManager.UserCreated -= OnUserCreated;
            _userManager.UserDeleted -= OnUserDeleted;
            _userManager.UserUpdated -= OnUserUpdated;
            _userManager.UserLockedOut -= OnUserLockedOut;
            _userManager.UserPasswordChanged -= OnUserPasswordChanged;
            _userManager.UserPolicyUpdated -= OnUserPolicyUpdated;
            _userManager.UserConfigurationUpdated -= OnUserConfigurationUpdated;
        }

        public void Dispose() => Stop();

        private void OnUserCreated(object sender, GenericEventArgs<User> e)
            => Publish(e, NotificationEventType.UserCreated, "创建用户", "➕", NotificationSeverity.Information, o => o.NotifyUserCreated, null, 0);

        private void OnUserDeleted(object sender, GenericEventArgs<User> e)
            => Publish(e, NotificationEventType.UserDeleted, "删除用户", "➖", NotificationSeverity.Warning, o => o.NotifyUserDeleted, null, 0);

        private void OnUserUpdated(object sender, GenericEventArgs<User> e)
            => Publish(e, NotificationEventType.UserUpdated, "更新用户", "✏️", NotificationSeverity.Information, o => o.NotifyUserUpdated, "userchg", 2);

        private void OnUserLockedOut(object sender, GenericEventArgs<User> e)
            => Publish(e, NotificationEventType.UserLockedOut, "用户被锁定", "🔒", NotificationSeverity.Security, o => o.NotifyUserLockedOut, "lockout", 10);

        private void OnUserPasswordChanged(object sender, GenericEventArgs<User> e)
            => Publish(e, NotificationEventType.UserPasswordChanged, "修改密码", "🔑", NotificationSeverity.Warning, o => o.NotifyUserPasswordChanged, null, 0);

        private void OnUserPolicyUpdated(object sender, GenericEventArgs<User> e)
            => Publish(e, NotificationEventType.UserPolicyUpdated, "更新用户策略", "🛡️", NotificationSeverity.Information, o => o.NotifyUserPolicyUpdated, "userchg", 2);

        private void OnUserConfigurationUpdated(object sender, GenericEventArgs<User> e)
            => Publish(e, NotificationEventType.UserConfigurationUpdated, "更新用户配置", "⚙️", NotificationSeverity.Information, o => o.NotifyUserConfigurationUpdated, "userchg", 2);

        /// <summary>
        /// 统一构建用户事件。dedupTag 非空时，同一用户 dedupWindow 秒内共享去重键，
        /// 避免用户更新/策略/配置在短时间内重复推送。
        /// </summary>
        private void Publish(GenericEventArgs<User> e, NotificationEventType type, string title, string emoji,
            NotificationSeverity severity, Func<PluginOptions, bool> enabled, string dedupTag, int dedupWindow)
        {
            try
            {
                var options = _ctx.GetEnabledOptions();
                if (options == null || !enabled(options)) return;

                var user = e?.Argument;
                if (user == null) return;

                var evt = new NotificationEvent
                {
                    EventType = type,
                    Category = NotificationCategory.UserManagement,
                    Severity = severity,
                    Emoji = emoji,
                    Title = title,
                    UserName = user.Name
                };
                evt.AddField("用户", user.Name, MessageDetailLevel.Simple);

                if (type == NotificationEventType.UserPasswordChanged)
                    evt.Summary = "该用户的密码已被修改";
                if (type == NotificationEventType.UserLockedOut)
                    evt.Summary = "该用户因多次登录失败被锁定";

                if (!string.IsNullOrEmpty(dedupTag) && dedupWindow > 0)
                {
                    evt.DeduplicationKey = dedupTag + "|" + user.Id.ToString("N");
                    evt.DedupWindowSeconds = dedupWindow;
                }

                _ctx.Publish(evt);
            }
            catch (Exception ex) { _ctx.Logger.Error("[EmbyFeishu] 处理用户事件异常: {0}", ex.Message); }
        }
    }
}
