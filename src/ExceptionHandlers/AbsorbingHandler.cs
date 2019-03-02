using System;

namespace Prgfx.Fusion.ExceptionHandlers
{
    public class AbsorbingHandler : AbstractExceptionHandler
    {
        protected override string Handle(string fusionPath, Exception exception, int referenceCode)
        {
            // this package has no logging
            return string.Empty;
        }

        protected new bool ExceptionDisablesCache(string fusionPath, Exception exception) => false;
    }
}