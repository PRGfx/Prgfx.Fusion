using System.Collections.Generic;

namespace Prgfx.Fusion.FusionObjects
{
    public abstract class AbstractFusionObject
    {
        protected Runtime runtime;

        protected string path;

        protected string fusionObjectName;

        protected Dictionary<string, object> fusionValueCache;

        public AbstractFusionObject(Runtime runtime, string path, string fusionObjectName)
        {
            this.runtime = runtime;
            this.path = path;
            this.fusionObjectName = fusionObjectName;
            this.fusionValueCache = new Dictionary<string, object>();
        }

        abstract public object Evaluate();

        public object GetRuntime()
        {
            return this.runtime;
        }

        protected object FusionValue(string path)
        {
            var fullPath = this.path + "/" + path;
            if (!fusionValueCache.ContainsKey(fullPath)) {
                fusionValueCache.Add(fullPath, runtime.Evaluate(fullPath, this));
            }
            return fusionValueCache[fullPath];
        }
    }
}