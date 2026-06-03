using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace AutoInstall.Services
{
    /// <summary>
    /// 控制台输出辅助：文件操作用进度条，外部进程用实时日志。
    /// </summary>
    public static class ConsoleProgress
    {
        private static readonly object _lock = new object();

        // ================================================================
        //  外部进程：实时输出日志（让工具自身的进度条可见）
        // ================================================================

        /// <summary>
        /// 运行外部进程，实时输出 stdout/stderr，返回退出码。
        /// </summary>
        public static int RunWithOutput(string exePath, string arguments)
        {
            return RunWithOutput(new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
        }

        /// <summary>
        /// 通过 cmd /c 运行命令（用于 .bat 文件）
        /// </summary>
        public static int RunCmdWithOutput(string batPath, string arguments)
        {
            return RunWithOutput(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = string.Format("/c \"\"{0}\" {1}\"", batPath, arguments),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
        }

        private static int RunWithOutput(ProcessStartInfo psi)
        {
            using (var process = new Process { StartInfo = psi })
            {
                // 使用事件异步读取，避免死锁
                var outputDone = new ManualResetEvent(false);
                var errorDone = new ManualResetEvent(false);

                process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        lock (_lock) { Console.WriteLine("  {0}", e.Data); }
                    }
                    else outputDone.Set();
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        lock (_lock) { Console.WriteLine("  {0}", e.Data); }
                    }
                    else errorDone.Set();
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
                outputDone.WaitOne(2000);
                errorDone.WaitOne(2000);

                return process.ExitCode;
            }
        }

        /// <summary>
        /// 运行进程并捕获输出（用于 pyenv which 等需要返回值的场景）
        /// </summary>
        public static string RunAndCapture(string exePath, string arguments)
        {
            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            })
            {
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
        }

        public static string RunCmdAndCapture(string batPath, string arguments)
        {
            return RunAndCapture("cmd.exe",
                string.Format("/c \"\"{0}\" {1}\"", batPath, arguments));
        }

        // ================================================================
        //  步骤标题
        // ================================================================

        public static void Step(string title)
        {
            Console.WriteLine();
            Console.WriteLine("---- {0} ----", title);
        }

        // ================================================================
        //  文件操作：进度条（这些操作没有自带输出）
        // ================================================================

        /// <summary>
        /// 遍历目录并逐文件复制，显示进度条。
        /// </summary>
        public static void CopyDirectoryWithProgress(string sourceDir, string targetDir, string label)
        {
            var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
            int total = files.Length;
            if (total == 0) total = 1;
            int done = 0;
            var width = Math.Min(50, Console.WindowWidth > 0 ? Console.WindowWidth - 25 : 50);

            Console.WriteLine("{0} (共 {1} 个文件)...", label, total);
            CopyRecursive(sourceDir, targetDir, ref done, total, width);
            DrawBar(total, total, width);
            Console.WriteLine();
        }

        private static void CopyRecursive(string src, string dst,
            ref int done, int total, int width)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src))
            {
                File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
                Interlocked.Increment(ref done);
                DrawBar(done, total, width);
            }
            foreach (var d in Directory.GetDirectories(src))
                CopyRecursive(d, Path.Combine(dst, Path.GetFileName(d)), ref done, total, width);
        }

        /// <summary>
        /// ZIP 解压（旋转器 + 完成后统计文件数）
        /// </summary>
        public static void ExtractZip(string zipPath, string destDir, string label)
        {
            var busy = true;
            var t = new Thread(() =>
            {
                int i = 0; var cs = new[] { '|', '/', '-', '\\' };
                while (busy) { Console.Write("\r  {0} {1}   ", cs[i++ % 4], label); Thread.Sleep(150); }
            }) { IsBackground = true };
            t.Start();

            try { System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, destDir); }
            finally
            {
                busy = false; t.Join(500);
                var fc = Directory.GetFiles(destDir, "*", SearchOption.AllDirectories).Length;
                var dc = Directory.GetDirectories(destDir, "*", SearchOption.AllDirectories).Length;
                Console.Write("\r[OK] {0} ({1} 个文件, {2} 个目录)   \n", label, fc, dc);
            }
        }

        private static void DrawBar(int done, int total, int width)
        {
            lock (_lock)
            {
                var pct = (double)done / total;
                var f = (int)(pct * width);
                Console.Write("\r  [{0}{1}] {2,3:P0}  {3}/{4}",
                    new string('#', f), new string('-', width - f), pct, done, total);
            }
        }
    }
}
