using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Program;

namespace DevApps.Samples
{
    internal static class CodeGen
    {
        internal static void Create()
        {
            DevObject.Create("insocket", "System socket buffer in")
                    .SetLoopMethod(@"")
                    .SetCode(@"");

            DevObject.Create("outsocket", "System socket buffer out")
                .SetLoopMethod(@"")
                .SetCode(@"");

            DevObject.Create("socket", "System socket")
                .AddPointer("IN", "insocket")
                .AddPointer("OUT", "outsocket")
                .AddPointer("RECEIVE", "buffer")
                .AddFunction(@"recv", @"
                    buffer.name = ""Kiki""
                    buffer.data.append(5)
                    buffer.data.append(21)
                    buffer.data.append(0x14)
                    buffer.data.append(5)
                ")
                .AddFunction(@"send", @"

                ")
                .SetCode(@"");

            DevObject.Create("buffer", "Receive Buffer")
                .SetLoopMethod(@"")
                .SetInitMethod(@"
                import array

                class Buffer:
                    name = ""Ignored""
                    data = bytearray()

                buffer = Buffer()
                ")
                .AddFunction(@"control", @"
                    name != ""Ignored""
                ")
                .SetBuildMethod(@"
                    import binascii

                    print(binascii.hexlify(buffer.data))
                ")
                .AddProperty(@"isvalid", @"len(buffer.data) > 0")
                .AddProperty(@"value", @"buffer.name");

            DevObject.Create("cstemplate", "Template")
                .SetInitMethod(@"
                class CsTemplate:
                    base = ""{namespace}\n\n{class def}\n\n{footer}""

                cstemplate = CsTemplate()
                ")
                .AddProperty(@"base", @"cstemplate.base");

            var oCode = DevObject.Create("cscode", "CSharp Code generator")
                .SetInitMethod(@"
                    out = """"
                    print('hello world!')
                ")
                .SetBuildMethod(@"
                    print('using namespace System;')
                    print()
                    print('public class { ... }')
                ");

        }
    }
}
