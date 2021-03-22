using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace TIAGroupCopyCLI.AppExceptions
{
    [Serializable]
    public class ParameterException : Exception
    {
        public ParameterException() : base() { }
        public ParameterException(string message): base(message) { }
        public ParameterException(string message, Exception inner) : base( message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client.
        protected ParameterException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class HaendlerException : Exception
    {
        public HaendlerException() : base() { }
        public HaendlerException(string message) : base(message) { }
        public HaendlerException(string message, Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client.
        protected HaendlerException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class GroupCopyException : Exception
    {
        public GroupCopyException() : base() { }
        public GroupCopyException(string message) : base(message) { }
        public GroupCopyException(string message, Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client.
        protected GroupCopyException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    public class ProgrammingException : Exception
    {
        public ProgrammingException() : base() { }
        public ProgrammingException(string message) : base(message) { }
        public ProgrammingException(string message, Exception inner) : base(message, inner) { }
    }
}
