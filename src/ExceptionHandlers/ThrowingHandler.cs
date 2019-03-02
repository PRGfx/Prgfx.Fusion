using System;

namespace Prgfx.Fusion.ExceptionHandlers
{
    public class ThrowingHandler : AbstractExceptionHandler
    {
        protected override string Handle(string fusionPath, Exception exception, int referenceCode)
        {
            return string.Empty;
        }

        public new string HandleRenderingException(string fusionPath, Exception exception)
        {
            throw exception;
        }
    }
}