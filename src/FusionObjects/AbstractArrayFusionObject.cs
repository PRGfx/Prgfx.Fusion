using System.Collections.Generic;

namespace Prgfx.Fusion.FusionObjects
{
    public abstract class AbstractArrayFusionObject : AbstractFusionObject
    {
        protected string[] ignoreProperties;

        protected string[] properties;

        public AbstractArrayFusionObject(Runtime runtime, string path, string fusionObjectName) : base(runtime, path, fusionObjectName)
        {
        }

        public void SetIgnoreProperties(string[] properties)
        {
            this.ignoreProperties = properties;
        }
    }
}