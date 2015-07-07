using System;
using System.Collections.Generic;
using System.Text;

namespace FlacBox.CdromUtils
{
    /// <summary>
    /// Base exception for CdromUtils library.
    /// </summary>
    public class CdromUtilsException : Exception
    {
        public CdromUtilsException(string message)
            : base(message)
        {
        }
    }
}
