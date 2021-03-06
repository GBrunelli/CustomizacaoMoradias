﻿using System;
using System.Runtime.Serialization;

namespace CustomizacaoMoradias
{
    class LevelNotFoundException : Exception
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

    class RoofNotDefinedException : Exception
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
