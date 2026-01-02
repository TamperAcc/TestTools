using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TestTool.Core.Enums;
using TestTool.Core.Models;
using TestTool.Core.Services;

namespace TestTool.Business.Services
{
    /// <summary>
    /// 简单协议解析器：解析简单文本 ON/OFF/STATUS 信息，可根据需要扩展
    /// </summary>
    public class SimpleProtocolParser : IProtocolParser
    {
        public IEnumerable<ParsedFrame> Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                yield break;

            var lines = raw.Replace("\r", "\n").Split('\n');
            foreach (var line in lines)
            {
                var text = line.Trim();
                if (string.IsNullOrEmpty(text))
                    continue;

                var frame = new ParsedFrame { Raw = text };
                var upper = text.ToUpperInvariant();

                if (upper.Contains("ON"))
                {
                    frame.Command = "ON";
                    frame.PowerState = DevicePowerState.On;
                }
                else if (upper.Contains("OFF"))
                {
                    frame.Command = "OFF";
                    frame.PowerState = DevicePowerState.Off;
                }
                else if (upper.Contains("STATUS"))
                {
                    frame.Command = "STATUS";
                    if (upper.Contains("ON")) frame.PowerState = DevicePowerState.On;
                    else if (upper.Contains("OFF")) frame.PowerState = DevicePowerState.Off;
                }

                yield return frame;
            }
        }
    }
}
