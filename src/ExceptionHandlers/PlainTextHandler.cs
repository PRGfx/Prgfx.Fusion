using System;

namespace Prgfx.Fusion.ExceptionHandlers
{
    public class PlainTextHandler : AbstractExceptionHandler
    {
        protected override string Handle(string fusionPath, Exception exception, int referenceCode)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("Exception while rendering ");
            sb.Append(FormatScriptPath(fusionPath, "\n\t"));
            sb.Append(": ");
            sb.Append(System.Text.RegularExpressions.Regex.Replace(exception.Message, "<[^>]*>", ""));
            if (referenceCode != 0)
            {
                sb.Append($" ({referenceCode})");
            }
            return sb.ToString();
        }

        protected new bool ExceptionDisablesCache(string fusionPath, Exception exception) => false;
    }
}