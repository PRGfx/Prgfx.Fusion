using System.Collections.Generic;

namespace Prgfx.Fusion.FusionObjects
{
    public class AttributesImplementation : AbstractArrayFusionObject
    {
        public AttributesImplementation(Runtime runtime, string path, string fusionObjectName) : base(runtime, path, fusionObjectName)
        {
        }

        public override object Evaluate()
        {
            var allowEmpty = (bool)FusionValue("__meta/allowEmpty");
            var sb = new System.Text.StringBuilder();
            foreach (var attributeName in properties)
            {
                if (System.Array.IndexOf(ignoreProperties, attributeName) >= 0)
                {
                    continue;
                }
                var attributeValue = FusionValue(attributeName);
                if (attributeValue == null) {
                    continue;
                }
                else if (attributeValue is bool && (bool)attributeValue == true && allowEmpty)
                {
                    sb.Append(' ').Append(attributeName);
                }
                else if (attributeValue is bool && (bool)attributeValue == false) {
                    continue;
                }
                else
                {
                    sb.Append(' ').Append(attributeName).Append("=\"").Append(attributeValue.ToString()).Append("\"");
                }
            }
            return sb.ToString();
        }
    }
}