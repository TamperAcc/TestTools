using TestTool.Business.Enums;

namespace TestTool.Business.Models
{
    /// <summary>
    /// 协议解析后的帧信息，包含原始文本和可选的业务含义。
    /// </summary>
    public class ParsedFrame
    {
        /// <summary>原始串口文本/帧。</summary>
        public string Raw { get; set; } = string.Empty;
        /// <summary>解析出的命令文本（如果有）。</summary>
        public string? Command { get; set; }
        /// <summary>检测到的电源状态（如果能推断）。</summary>
        public DevicePowerState? PowerState { get; set; }
    }
}
