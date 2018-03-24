using System;
using System.Collections.Generic;
using System.Text;

namespace Reyno.AspNetCore.CommandR
{
    public class CommandROptions
    {
        public string Path { get; set; } = "_commandr";
        public string RequestNamespace { get; set; }
    }
}
