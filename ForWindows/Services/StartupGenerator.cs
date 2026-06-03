using System;
using System.IO;

namespace AutoInstall.Services
{
    /// <summary>
    /// 生成一键启动.bat 启动脚本
    /// </summary>
    public static class StartupGenerator
    {
        /// <summary>
        /// 根据模板（或默认内容）生成启动批处理文件。
        /// </summary>
        public static void Generate(string batDir, string venvName, string venvPath,
            string venvPython, string defaultExec)
        {
            var startupBat = Path.Combine(batDir, "一键启动.bat");

            // 在 Config 目录下递归查找第一个 .txt 模板文件
            string templateFile = null;
            var configDir = Path.Combine(batDir, "Config");
            if (Directory.Exists(configDir))
            {
                foreach (var txt in Directory.GetFiles(configDir, "*.txt", SearchOption.AllDirectories))
                {
                    templateFile = txt;
                    break;
                }
            }

            if (templateFile == null)
            {
                templateFile = Path.Combine(configDir, "启动虚拟环境.txt");
            }

            if (!File.Exists(templateFile))
            {
                // 无模板文件，使用默认内容
                using (var writer = new StreamWriter(startupBat, false, System.Text.Encoding.Default))
                {
                    writer.WriteLine("call %~dp0{0}\\Scripts\\activate.bat", venvName);
                    if (!string.IsNullOrWhiteSpace(defaultExec))
                        writer.WriteLine(defaultExec);
                }
            }
            else
            {
                Console.WriteLine("正在根据 {0} 生成 {1} ...", templateFile, startupBat);

                if (File.Exists(startupBat))
                    File.Delete(startupBat);

                using (var writer = new StreamWriter(startupBat, false, System.Text.Encoding.Default))
                {
                    foreach (var line in File.ReadAllLines(templateFile))
                    {
                        var processed = line
                            .Replace("{BAT_DIR}", batDir)
                            .Replace("{VENV_NAME}", venvName)
                            .Replace("{VENV_PATH}", venvPath)
                            .Replace("{VENV_PYTHON}", venvPython);

                        if (string.IsNullOrEmpty(line))
                            writer.WriteLine();
                        else
                            writer.WriteLine(processed);
                    }
                }
            }
        }
    }
}
