namespace Prgfx.Fusion.FusionObjects
{
    public class RendererImplementation : AbstractFusionObject
    {
        public RendererImplementation(Runtime runtime, string path, string fusionObjectName) : base(runtime, path, fusionObjectName)
        {
        }

        public string GetObjectType()
        {
            return (string)FusionValue("type");
        }

        public string GetRenderPath()
        {
            return (string)FusionValue("renderPath");
        }

        public override object Evaluate()
        {
            var rendererPath = path + "/renderer";
            var canRenderWithRenderer = runtime.CanRender(rendererPath);
            var renderPath = GetRenderPath();

            object renderedElement;
            if (canRenderWithRenderer) {
                renderedElement = runtime.Evaluate(rendererPath, this);
            } else if (!string.IsNullOrEmpty(renderPath)) {
                if (renderPath[0] == '/') {
                    renderedElement = runtime.Render(renderPath.Substring(1));
                } else {
                    renderedElement = runtime.Render(path + '/' + renderPath.Replace('.', '/'));
                }
            } else {
                var objectType = GetObjectType();
                renderedElement = runtime.Render($"{path}/element<{objectType}>");
            }
            return renderedElement;
        }
    }
}