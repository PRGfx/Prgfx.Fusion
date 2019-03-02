namespace Prgfx.Fusion.ExceptionHandlers
{
    public abstract class AbstractExceptionHandler
    {
        public Runtime Runtime { set; protected get; }

        /// <summary>
        /// Handle an Exception thrown while rendering Fusion
        /// </summary>
        /// <param name="fusionPath"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        public string HandleRenderingException(string fusionPath, System.Exception exception)
        {
            if (ExceptionDisablesCache(fusionPath, exception))
            {
                Runtime.SetEnableContentCache(false);
            }
            return Handle(fusionPath, exception, 0);
        }

        /// <summary>
        /// Handles an Exception thrown while rendering Fusion
        /// </summary>
        /// <param name="fusionPath"></param>
        /// <param name="exception"></param>
        /// <param name="referenceCode"></param>
        /// <returns></returns>
        protected abstract string Handle(string fusionPath, System.Exception exception, int referenceCode);

        /// <summary>
        /// breaks the given path to multiple lines to allow a nicer formatted logging
        /// 
        /// example:
        /// <pre>
        /// FormatScriptPath("page<Page>/body<Template>/content/main<ContentCollection>", ""):
        /// page<Page>/body<Template>/content/main<ContentCollection>
        /// </pre>
        /// <pre>
        /// FormatScriptPath("page<Page>/body<Template>/content/main<ContentCollection>", "\n\t\t"):
        /// page<Page>/
        ///         body<Template>/
        ///         content/
        ///         main<ContentCollection>
        /// </pre>
        /// </summary>
        /// <param name="fusionPath"></param>
        /// <param name="delimiter"></param>
        /// <param name="escapeHtml"></param>
        /// <returns></returns>
        protected string FormatScriptPath(string fusionPath, string delimiter, bool escapeHtml = true)
        {
            if (escapeHtml)
            {
                fusionPath = System.Web.HttpUtility.HtmlEncode(escapeHtml);
            }
            var elements = fusionPath.Split('/');
            return string.Join("/" + delimiter, elements);
        }

        /// <summary>
        /// Can be used to determine if handling the exception should disable the cache or not.
        /// </summary>
        /// <param name="fusionPath"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        protected bool ExceptionDisablesCache(string fusionPath, System.Exception exception) => true;

    }
}