using System.Collections.Generic;
using System.Linq;

namespace Prgfx.Fusion.FusionObjects
{
    public class LoopImplementation : MapImplementation
    {
        public LoopImplementation(Runtime runtime, string path, string fusionObjectName) : base(runtime, path, fusionObjectName)
        {
        }

        public string GetGlue()
        {
            return (string)FusionValue("__meta/glue") ?? "";
        }

        public override object Evaluate()
        {
            var glue = GetGlue();
            var collection = (Dictionary<string, object>)base.Evaluate();
            return string.Join(glue, collection.Select(x => x.Value.ToString()));
        }
    }
}