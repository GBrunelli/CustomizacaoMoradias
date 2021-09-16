using System;
using System.Runtime.Serialization;

namespace CustomizacaoMoradias.Source.Exceptions
{
    internal class LevelNotFoundException : Exception
    {
        public LevelNotFoundException()
        {
        }

        public LevelNotFoundException(string message) : base(message)
        {
        }

        public LevelNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected LevelNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    internal class RoofNotDefinedException : Exception
    {
        public RoofNotDefinedException()
        {
        }

        public RoofNotDefinedException(string message) : base(message)
        {
        }

        public RoofNotDefinedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RoofNotDefinedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
