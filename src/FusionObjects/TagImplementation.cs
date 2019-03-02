using System.Collections.Generic;

namespace Prgfx.Fusion.FusionObjects
{
    public class TagImplementation : AbstractFusionObject
    {
        public TagImplementation(Runtime runtime, string path, string fusionObjectName) : base(runtime, path, fusionObjectName)
        {
        }

        public override object Evaluate()
        {
            var tagName = (string)FusionValue("tagName");
            if (tagName == null)
            {
                throw new FusionException("Missing property `tagName`");
            }
            var selfClosing = (bool)FusionValue("selfClosingTag");
            var omitClosingTag = (bool)FusionValue("omitClosingTag");
            var content = (string)FusionValue("content");
            var sb = new System.Text.StringBuilder("<");
            sb.Append(tagName);
            var attributes = (string)FusionValue("attributes");
            if (attributes != null && attributes.Length > 0)
            {
                sb.Append(attributes);
            }
            var closed = false;
            if (selfClosing && content != null && content.Length > 0)
            {
                if (!omitClosingTag) {
                    sb.Append("/");
                }
                sb.Append(">");
                closed = true;
            }
            else
            {
                sb.Append(">");
            }
            if (content != null && content.Length > 0)
            {
                sb.Append(content);
            }
            if (!closed && !omitClosingTag)
            {
                sb.Append("</").Append(tagName).Append(">");
            }
            return sb.ToString();
        }
    }
}