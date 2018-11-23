using System;
using System.Collections.Generic;
using System.Text;

namespace CoreDRXLibrary
{
    public class DRXLoadException : Exception
    {
        public DRXLoadException(string message) : base(message)
        {
        }

        public DRXLoadException(string message, Exception e) : base(message)
        {
        }
    }
}
