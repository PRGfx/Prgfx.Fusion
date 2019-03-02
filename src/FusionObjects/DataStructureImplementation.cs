using System.Collections.Generic;

namespace Prgfx.Fusion.FusionObjects
{
    public class DataStructureImplementation : AbstractArrayFusionObject
    {
        public DataStructureImplementation(Runtime runtime, string path, string fusionObjectName) : base(runtime, path, fusionObjectName)
        {
        }

        public override object Evaluate()
        {
            var result = new Dictionary<string, object>();
            foreach (var property in properties) {
                if (System.Array.IndexOf(ignoreProperties, property) >= 0) {
                    continue;
                }
                result.Add(property, runtime.Evaluate(this.path + "/" + property));
            }
            return result;
        }
    }
}