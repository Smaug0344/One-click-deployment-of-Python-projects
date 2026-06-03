using System;
using System.Collections.Generic;
using System.IO;

namespace AutoInstall.Services
{
    /// <summary>
    /// 读取并解析 Config\config.ini 配置文件
    /// </summary>
    public class ConfigData
    {
        public string Version { get; set; }
        public string VenvName { get; set; }
        public string DefaultExecution { get; set; }
        public string UpgradePip { get; set; }
        public string AutoInstallNodeJs { get; set; }
        public string NodeVersion { get; set; }

        public bool IsValid
        {
            get { return !string.IsNullOrWhiteSpace(Version) && !string.IsNullOrWhiteSpace(VenvName); }
        }
    }

    public static class ConfigReader
    {
        public static ConfigData Read(string batDir)
        {
            var config = new ConfigData();
            var configDir = Path.Combine(batDir, "Config");

            if (!Directory.Exists(configDir))
                throw new DeployException(string.Format("未找到 Config 文件夹，请确认 {0} 存在。", configDir));

            var configFile = Path.Combine(configDir, "config.ini");
            if (!File.Exists(configFile))
                throw new DeployException("未找到 config.ini 文件。");

            foreach (var line in File.ReadAllLines(configFile))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith(";"))
                    continue;

                var eqIndex = trimmed.IndexOf('=');
                if (eqIndex < 0)
                    continue;

                var key = trimmed.Substring(0, eqIndex).Trim();
                var value = trimmed.Substring(eqIndex + 1).Trim();

                if (value.Length >= 2 && value.StartsWith("\"") && value.EndsWith("\""))
                    value = value.Substring(1, value.Length - 2);

                switch (key)
                {
                    case "VERSION":              config.Version = value; break;
                    case "VENV_NAME":            config.VenvName = value; break;
                    case "DefaultExecution":     config.DefaultExecution = value; break;
                    case "UPGRADE_PIP":           config.UpgradePip = value; break;
                    case "AUTO_INSTALL_NodeJs":  config.AutoInstallNodeJs = value; break;
                    case "NODE_VERSION":         config.NodeVersion = value; break;
                }
            }

            if (string.IsNullOrWhiteSpace(config.Version))
                throw new DeployException("config.ini 中未找到 VERSION 配置项。");
            if (string.IsNullOrWhiteSpace(config.VenvName))
                throw new DeployException("config.ini 中未找到 VENV_NAME 配置项。");

            return config;
        }
    }
}
