using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Assembler
{
    public enum Segments
    {
        [Description("LCL")]
        Local,
        [Description("ARG")]
        Argument,
        [Description("THIS")]
        This,
        [Description("THAT")]
        That,
        [Description("Static")]
        Static,
        [Description("SP")]
        Conatant,
        [Description("Pointer")]
        Pointer,
        [Description("TEMP")]
        Temp,
    }
}
