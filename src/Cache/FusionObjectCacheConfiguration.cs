using System;

namespace Prgfx.Fusion.Cache
{
    public class FusionObjectCacheConfiguration
    {

        public enum CacheMode
        {
            Cached,
            Uncached,
            Dynamic,
            Embed,
        }
        public CacheMode Mode;

        public int MaximumLifeTime;

        public System.Collections.Generic.Dictionary<string, object> Context;

        public FusionAst EntryIdentifier;

        public string EntryDiscriminator;

        public System.Collections.Generic.Dictionary<string, object> EntryTags;

        public FusionObjectCacheConfiguration()
        {
            Context = new System.Collections.Generic.Dictionary<string, object>();
            EntryTags = new System.Collections.Generic.Dictionary<string, object>();
            EntryIdentifier = new FusionAst();
        }

        public FusionObjectCacheConfiguration(FusionAst ast)
        {
            Context = new System.Collections.Generic.Dictionary<string, object>();
            EntryTags = new System.Collections.Generic.Dictionary<string, object>();
            Mode = GetCacheModeFromName(ast.GetValue(new string[] { "mode" }));
            var maxLifeTimeValue = ast.GetValue(new string[] { "maximumLifetime" });
            MaximumLifeTime = maxLifeTimeValue != null && (maxLifeTimeValue is int) ? (int)maxLifeTimeValue : 0;
            EntryIdentifier = ast["entryIdentifier"];
        }

        private CacheMode GetCacheModeFromName(object cacheModeName)
        {
            if (cacheModeName == null || !(cacheModeName is string))
            {
                return CacheMode.Embed;
            }
            switch ((string)cacheModeName)
            {
                case "cached":
                    return CacheMode.Cached;
                case "uncached":
                    return CacheMode.Uncached;
                case "dynamic":
                    return CacheMode.Dynamic;
            }
            return CacheMode.Embed;
        }
    }
}