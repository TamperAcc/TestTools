using TestTool.Business.Enums;

namespace TestTool.Business.Models
{
    /// <summary>
    /// 解析后的帧信息，保留原始文本和可选的业务字段。
    /// </summary>
    public class ParsedFrame
    {
        public string Raw { get; set; } = string.Empty;
        public string? Command { get; set; }
        public DevicePowerState? PowerState { get; set; }
    }
}
