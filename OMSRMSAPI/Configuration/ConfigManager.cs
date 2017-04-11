using System.Configuration;

namespace OMSRMSAPI.Configuration
{
    /// <summary>
    /// Configuration for getting parameters from App.config file
    /// </summary>
    internal class ConfigManager
    {

        private const string BaseUrlKeyName = "baseUrl";
        private const string UserNameKeyName = "userName";
        private const string PasswordKeyName = "password";
        private const string AuthorizationType = "authType";
        private const string ContentType = "contentType";
        private const string Sender = "senderId";
        private const string VersionNo = "targetVersionNo";
        private const string PatchLevel = "targetPatchLevel";
        private const string MetaVersionNo = "targetMetaVersionNo";
        private const string Culture = "contentCulture";

        public string UserName => GetConfigKey(UserNameKeyName);

        public string Password => GetConfigKey(PasswordKeyName);

        public string AuthType => GetConfigKey(AuthorizationType);

        public string BaseUrl => GetConfigKey(BaseUrlKeyName);

        public string ContType => GetConfigKey(ContentType);

        public string SenderId => GetConfigKey(Sender);

        public string TargetVersionNo => GetConfigKey(VersionNo);

        public string TargetPatchLevel => GetConfigKey(PatchLevel);

        public string TargetMetaVersionNo => GetConfigKey(MetaVersionNo);

        public string ContentCulture => GetConfigKey(Culture);

        private string GetConfigKey(string name)
        {
            return ConfigurationManager.AppSettings[name];
        }
    }
}
