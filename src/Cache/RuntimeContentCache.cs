using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Prgfx.Fusion.FusionObjects;

namespace Prgfx.Fusion.Cache
{
    public class RuntimeContentCache
    {
        protected Runtime runtime;

        public bool EnableContentCache = false;

        protected bool addCacheSegmentMarkersToPlaceholders = false;
        private bool inCacheEntryPoint;

        protected Dictionary<string, bool> tags;

        public RuntimeContentCache(Runtime runtime)
        {
            this.runtime = runtime;
            this.tags = new Dictionary<string, bool>();
        }

        /// <summary>
        /// Adds a tag built from the given key and value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddTag(string key, string value)
        {
            key = key.Trim();
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("Tag Key must not be empty");
            }
            value = value.Trim();
            if (string.IsNullOrEmpty(value))
            {
                throw new Exception("Tag Value must not be empty");
            }
            var tag = key[0].ToString().ToUpper() + key.Substring(1) + "DynamicTag_" + value;
            tags[tag] = true;
        }

        /// <summary>
        /// Resets the assigned tags, returning the previously set tags.
        /// </summary>
        /// <returns></returns>
        public string[] FlushTags()
        {
            var tags = this.tags.Keys.Select(k => k.ToString()).ToArray();
            this.tags.Clear();
            return tags;
        }

        /// <summary>
        /// Enter an evaluation
        /// 
        /// Needs to be called right before evaluation of a path start to check the cache mode and set
        /// internal state like the cache entry point.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="fusionPath"></param>
        /// <returns></returns>
        public EvaluationContext Enter(FusionObjectCacheConfiguration configuration, string fusionPath)
        {
            var cacheForPathEnabled = configuration.Mode == FusionObjectCacheConfiguration.CacheMode.Cached || configuration.Mode == FusionObjectCacheConfiguration.CacheMode.Dynamic;
            var cacheForPathDisabled = configuration.Mode == FusionObjectCacheConfiguration.CacheMode.Uncached || configuration.Mode == FusionObjectCacheConfiguration.CacheMode.Dynamic;

            if (cacheForPathDisabled && configuration.Context.Count == 0)
            {
                throw new Exception($"Missing @cache.context configuration for path \"{fusionPath}\". An uncached segment must have one or more context variable names configured.");
            }

            var currentPathIsEntryPoint = false;
            if (EnableContentCache || cacheForPathEnabled)
            {
                if (!inCacheEntryPoint)
                {
                    inCacheEntryPoint = true;
                    currentPathIsEntryPoint = true;
                }
            }

            return new EvaluationContext()
            {
                Configuration = configuration,
                FusionPath = fusionPath,
                CacheForPathEnabled = cacheForPathEnabled,
                CacheForPathDisabled = cacheForPathDisabled,
                CurrentPathIsEntryPoint = currentPathIsEntryPoint,
            };
        }

        public bool preEvaluate(EvaluationContext evaluationContext, AbstractFusionObject fusionObject, out object cacheValue)
        {
            /* TODO
            if (EnableContentCache)
            {
                if (evaluationContext.CacheForPathEnabled && evaluationContext.CacheForPathDisabled)
                {
                    evaluationContext.CacheDiscriminator = (string)runtime.Evaluate(evaluationContext.FusionPath + "/__meta/cache/entryDiscriminator");
                }
                if (evaluationContext.CacheForPathEnabled)
                {
                    evaluationContext.CacheIdentifierValues = BuildCacheIdentifierValues(evaluationContext.Configuration, evaluationContext.FusionPath, fusionObject);
                }
            } */
            cacheValue = null;
            return false;
        }

        public object postProcess(EvaluationContext evaluationContext, AbstractFusionObject fusionObject, object output)
        {
            return output;
        }

        /// <summary>
        /// Leave the evaluation of a path
        /// 
        /// Has to be called in the same function calling Enter() for every return path.
        /// </summary>
        /// <param name="evaluationContext"></param>
        public void leave(EvaluationContext evaluationContext)
        {
            if (evaluationContext.CurrentPathIsEntryPoint)
            {
                this.inCacheEntryPoint = false;
            }
        }

        /// <summary>
        /// Evaluate a Fusion path with a given context without content caching
        /// 
        /// This is used to render uncached segmens "out of band" in GetCachedSegment of ContentCache
        /// </summary>
        /// <param name="path"></param>
        /// <param name="contextDict"></param>
        /// <returns></returns>
        public object evaluateUncached(string path, Dictionary<string, object> contextDict)
        {
            var previousEnableContentcache = EnableContentCache;
            EnableContentCache = false;
            runtime.PushContextArray(contextDict);
            var result = runtime.Evaluate(path);
            runtime.PopContext();
            EnableContentCache = previousEnableContentcache;
            return result;
        }

        /// <summary>
        /// Builds an array of additional key/values which must go into the calculation of the cache entry identifier
        /// for a cached content segment
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="fusionPath"></param>
        /// <param name="fusionObject"></param>
        /// <returns></returns>
        protected string[] BuildCacheIdentifierValues(FusionObjectCacheConfiguration configuration, string fusionPath, AbstractFusionObject fusionObject)
        {
            var objectType = "<Neos.Fusion:GlobalCacheIdentifiers>";
            if (!string.IsNullOrEmpty(configuration.EntryIdentifier.ObjectType))
            {
                objectType = $"<{configuration.EntryIdentifier.ObjectType}>";
            }
            try
            {
                var entryIdentifiers = (System.Collections.IEnumerable)runtime.Evaluate(fusionPath + "/__meta/cache/entryIdentifier" + objectType, fusionObject);
                var result = new List<string>();
                foreach (var entryIdentifier in entryIdentifiers)
                {
                    result.Add((string)entryIdentifier);
                }
                return result.ToArray();
            }
            catch (Exception)
            {
                return new string[] { };
            }
        }

        /// <summary>
        /// Builds an array of strings which must be used as tags for the cache entry identifier of a specific cached content segment.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="fusionPath"></param>
        /// <param name="fusionObject"></param>
        /// <returns></returns>
        protected string[] BuildCachetags(FusionObjectCacheConfiguration configuration, string fusionPath, AbstractFusionObject fusionObject)
        {
            var cacheTags = new List<string>();
            if (configuration.EntryTags.Count > 0)
            {
                foreach (var kvp in configuration.EntryTags)
                {
                    var tagValue = runtime.Evaluate(fusionPath + "/__meta/cache/entryTags/" + kvp.Key, fusionObject);
                    if (tagValue is IEnumerable)
                    {
                        foreach (var item in (IEnumerable)tagValue)
                        {
                            cacheTags.Add((string)item);
                        }
                    }
                    else if (tagValue is string)
                    {
                        cacheTags.Add((string)tagValue);
                    }
                }
                cacheTags.AddRange(FlushTags());
            }
            else
            {
                cacheTags.Add("Everything");
            }
            return cacheTags.Distinct().ToArray();
        }
    }
}