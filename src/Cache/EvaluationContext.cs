using System;

namespace Prgfx.Fusion.Cache
{
    public class EvaluationContext
    {
        public string[] CacheIdentifierValues;
        public FusionObjectCacheConfiguration Configuration;

        public string FusionPath;

        public bool CacheForPathEnabled;

        public bool CacheForPathDisabled;

        public bool CurrentPathIsEntryPoint;

        public string CacheDiscriminator;
    }
}