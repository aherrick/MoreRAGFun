using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoreRAGFun.Models;

public class RawContent
{
    public string Text { get; init; }

    public ReadOnlyMemory<byte> Image { get; init; }

    public int PageNumber { get; init; }
}