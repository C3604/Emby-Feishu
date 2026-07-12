using System.ComponentModel;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Validation;
using MediaBrowser.Model.Attributes;

namespace EmbyFeishu.Configuration.Groups
{
    /// <summary>
    /// 第五组：登录与用户 — 登录安全、会话和用户管理事件。
    /// </summary>
    public class LoginAndUserGroup : EditableOptionsBase
    {
        public override string EditorTitle => "登录与用户";

        // 登录安全
        [DisplayName("通知登录成功")]
        [IsAdvanced]
        public bool NotifyAuthenticationSucceeded { get; set; } = false;

        [DisplayName("通知登录失败")]
        public bool NotifyAuthenticationFailed { get; set; } = true;

        [DisplayName("通知用户被锁定")]
        public bool NotifyUserLockedOut { get; set; } = true;

        // 会话
        [DisplayName("通知会话开始")]
        [IsAdvanced]
        public bool NotifySessionStarted { get; set; } = false;

        [DisplayName("通知会话结束")]
        [IsAdvanced]
        public bool NotifySessionEnded { get; set; } = false;

        [DisplayName("通知远程控制断开")]
        [IsAdvanced]
        public bool NotifyRemoteControlDisconnected { get; set; } = false;

        [DisplayName("通知加入同步播放")]
        [IsAdvanced]
        public bool NotifyPartyJoined { get; set; } = false;

        [DisplayName("通知离开同步播放")]
        [IsAdvanced]
        public bool NotifyPartyLeft { get; set; } = false;

        // 用户管理
        [DisplayName("通知修改密码")]
        public bool NotifyUserPasswordChanged { get; set; } = true;

        [DisplayName("通知创建用户")]
        [IsAdvanced]
        public bool NotifyUserCreated { get; set; } = false;

        [DisplayName("通知删除用户")]
        [IsAdvanced]
        public bool NotifyUserDeleted { get; set; } = false;

        [DisplayName("通知更新用户")]
        [IsAdvanced]
        public bool NotifyUserUpdated { get; set; } = false;

        [DisplayName("通知更新用户策略")]
        [IsAdvanced]
        public bool NotifyUserPolicyUpdated { get; set; } = false;

        [DisplayName("通知更新用户配置")]
        [IsAdvanced]
        public bool NotifyUserConfigurationUpdated { get; set; } = false;

        // 用户过滤
        [DisplayName("用户过滤模式")]
        [Description("All=所有用户；IncludeOnly=仅通知指定用户；Exclude=排除指定用户。仅作用于播放/用户行为类事件")]
        [IsAdvanced]
        public Models.UserFilterMode UserFilterMode { get; set; } = Models.UserFilterMode.All;

        [DisplayName("用户名列表")]
        [Description("逗号、分号或换行分隔的用户名")]
        [IsAdvanced]
        [EditMultiline(3)]
        [VisibleCondition(nameof(UserFilterMode), ValueCondition.IsNotEqual, Models.UserFilterMode.All)]
        public string UserNames { get; set; } = "";
    }
}
