using System.Collections.Generic;

namespace Prgfx.Fusion.FusionObjects
{
    public class ArrayImplementation : AbstractArrayFusionObject
    {
        public ArrayImplementation(Runtime runtime, string path, string fusionObjectName) : base(runtime, path, fusionObjectName)
        {
        }

        public override object Evaluate()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var property in properties) {
                if (System.Array.IndexOf(ignoreProperties, property) >= 0) {
                    continue;
                }
                sb.Append(runtime.Evaluate(this.path + "/" + property));
            }
            return sb.ToString();
        }
    }
}