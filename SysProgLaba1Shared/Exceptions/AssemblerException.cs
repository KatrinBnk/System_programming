using System;

namespace SysProgLaba1Shared.Exceptions
{
    public class AssemblerException : Exception
    {
        public AssemblerException()
        {
        }

        public AssemblerException(string message)
            : base(message)
        {
        }

        public AssemblerException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}

