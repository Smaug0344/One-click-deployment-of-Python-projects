using System;

namespace AutoInstall.Services
{
    /// <summary>
    /// 部署过程中的可恢复错误，抛出后由 Main 统一捕获并暂停。
    /// </summary>
    public class DeployException : Exception
    {
        public DeployException(string message) : base(message) { }
    }
}
