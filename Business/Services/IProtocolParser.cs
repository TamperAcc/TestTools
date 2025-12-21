using System.Collections.Generic;
using TestTool.Business.Models;

namespace TestTool.Business.Services
{
    /// <summary>
    /// 协议解析器接口：负责把串口收到的原始文本/帧转换成可消费的业务帧。
    /// 典型用例：串口服务收到字符串后调用 Parse，得到若干解析结果交给控制器处理。
    /// </summary>
    public interface IProtocolParser
    {
        /// <summary>
        /// 解析一条串口原始文本（或帧）。
        /// - 输入：未经处理的原始字符串（可能包含多行）。
        /// - 输出：零个或多个 <see cref="ParsedFrame"/>，每个帧包含原文及可选的命令/状态。
        /// </summary>
        /// <param name="raw">串口读到的原始字符串（未加工，可含换行）。</param>
        /// <returns>解析后的帧集合，调用方可逐个遍历并驱动业务逻辑。</returns>
        IEnumerable<ParsedFrame> Parse(string raw);
    }
}
