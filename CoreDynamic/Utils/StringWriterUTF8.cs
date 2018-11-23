using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CoreDynamic.Utils
{
    public class StringWriterUTF8 : StringWriter
    {
        Encoding encoding;
        public override Encoding Encoding => encoding;

        public StringWriterUTF8(StringBuilder builder) : base(builder)
        {
            this.encoding = System.Text.Encoding.UTF8;
        }
    }
}
