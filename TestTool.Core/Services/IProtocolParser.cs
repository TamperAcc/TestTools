using System.Collections.Generic;
using TestTool.Business.Models;

namespace TestTool.Business.Services
{
    /// <summary>
    /// 协议解析接口：把原始文本/帧解析为业务帧集合，供上层处理。
    /// </summary>
    public interface IProtocolParser
    {
        IEnumerable<ParsedFrame> Parse(string raw);
    }
}
