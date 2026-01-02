using System.Collections.Generic;
using TestTool.Core.Models;

namespace TestTool.Core.Services
{
    /// <summary>
    /// 协议解析接口：把原始文本/帧解析为业务帧集合，供上层处理。
    /// </summary>
    public interface IProtocolParser
    {
        IEnumerable<ParsedFrame> Parse(string raw);
    }
}
