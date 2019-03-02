using System;
using System.Collections.Generic;
using Prgfx.Fusion.ExceptionHandlers;

namespace Prgfx.Fusion
{
    public class ExceptionHandlerFactory
    {
        protected Dictionary<Type, AbstractExceptionHandler> handlers;

        public ExceptionHandlerFactory()
        {
            this.handlers = new Dictionary<Type, AbstractExceptionHandler>();
            // register internal handlers
            Register(new AbsorbingHandler());
            Register(new PlainTextHandler());
            Register(new ThrowingHandler());
        }

        public void Register<T>() where T : AbstractExceptionHandler
        {
            var newInstance = (AbstractExceptionHandler)Activator.CreateInstance(typeof(T));
            if (!handlers.TryAdd(typeof(T), newInstance))
            {
                handlers[typeof(T)] = newInstance;
            }
        }

        public void Register(AbstractExceptionHandler handler)
        {
            if (!handlers.TryAdd(handler.GetType(), handler))
            {
                handlers[handler.GetType()] = handler;
            }
        }

        public AbstractExceptionHandler Get(string typeName)
        {
            return Get(Type.GetType(typeName));
        }

        private AbstractExceptionHandler Get(Type type)
        {
            if (this.handlers.ContainsKey(type))
            {
                return this.handlers[type];
            }
            throw new KeyNotFoundException("Unknown Exception-Handler type");
        }
    }
}