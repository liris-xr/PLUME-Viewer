using System;

namespace Runtime
{
    public abstract class MalformedStreamException : Exception
    {
        private MalformedStreamException(string message) : base(message)
        {
        }

        public class MalformedVarInt : MalformedStreamException
        {
            public MalformedVarInt(string message) : base(message)
            {
            }
        }
    }
}