using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Prgfx.Fusion.FusionObjects;

namespace Prgfx.Fusion
{
    public enum EvaluationStatus
    {
        Skipped,
        Executed,
    }

    public enum FailureBehavior
    {
        Exception,
        ReturnNull,
    }

    public class Runtime
    {

        protected EvaluationStatus lastEvaluationStatus;

        protected Stack<Dictionary<string, object>> contextStack;

        protected Stack<Dictionary<string, KeyValuePair<string, object>>> applyValueStack;

        protected FusionAst configuration;

        protected Cache.RuntimeContentCache runtimeContentCache;

        protected RuntimeConfiguration settings;

        protected ExceptionHandlerFactory exceptionHandlerFactory;

        public Runtime(FusionAst configuration, RuntimeConfiguration settings = null)
        {
            this.configuration = configuration;
            var emptyContext = new Dictionary<string, object>();
            contextStack = new Stack<Dictionary<string, object>>() { };
            contextStack.Push(emptyContext);
            var emptyApplyValues = new Dictionary<string, KeyValuePair<string, object>>();
            applyValueStack = new Stack<Dictionary<string, KeyValuePair<string, object>>>();
            applyValueStack.Push(emptyApplyValues);
            this.settings = settings ?? new RuntimeConfiguration();
            exceptionHandlerFactory = new ExceptionHandlerFactory();
            this.runtimeContentCache = new Cache.RuntimeContentCache(this);
        }

        public void SetExceptionHandlerFactory(ExceptionHandlerFactory exceptionHandlerFactory)
        {
            this.exceptionHandlerFactory = exceptionHandlerFactory;
        }

        /// <summary>
        /// Add a tag to the current cache segment
        /// 
        /// During Fusion rendering the method can be used to add tag dynamically for the current cache segment.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddCacheTag(string key, string value)
        {
            if (!runtimeContentCache.EnableContentCache)
            {
                return;
            }
            runtimeContentCache.AddTag(key, value);
        }

        /// <summary>
        /// Completely replace the context dict the new contextDict.
        /// 
        /// Purely internal method, should not be called outside of this package.
        /// </summary>
        /// <param name="contextDict"></param>
        public void PushContextArray(Dictionary<string, object> contextDict)
        {
            this.contextStack.Push(contextDict);
        }

        /// <summary>
        /// Push a new context object to the rendering stack
        /// </summary>
        /// <param name="key">The key inside the context</param>
        /// <param name="context"></param>
        public void PushContext(string key, object context)
        {
            var newContext = new Dictionary<string, object>(GetCurrentContext());
            newContext.Add(key, context);
            contextStack.Push(newContext);
        }

        /// <summary>
        /// Remove hte topmost context ojbects and return them
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> PopContext()
        {
            return this.contextStack.Pop();
        }

        /// <summary>
        /// Get the current context dict
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetCurrentContext()
        {
            return this.contextStack.Peek();
        }

        public void PushApplyValues(Dictionary<string, KeyValuePair<string, object>> values)
        {
            applyValueStack.Push(values);
        }

        /// <summary>
        /// Returns the topmost "@apply" values
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, KeyValuePair<string, object>> PopApplyValues()
        {
            return applyValueStack.Pop();
        }

        public Dictionary<string, KeyValuePair<string, object>> GetCurrentApplyValues()
        {
            return applyValueStack.Peek();
        }

        /// <summary>
        /// Evaluate an absolute Fusion path and return the result
        /// </summary>
        /// <param name="fusionPath"></param>
        /// <param name="contextObject">The object available as "this" in Eel expressions. ONLY FOR INTERNAL USE!</param>
        /// <returns>The result of the evaluation can be any type</returns>
        public object Evaluate(string fusionPath, AbstractFusionObject contextObject = null)
        {
            return EvaluateInternal(fusionPath, FailureBehavior.ReturnNull, contextObject);
        }

        public EvaluationStatus GetLastEvaluationStatus()
        {
            return lastEvaluationStatus;
        }

        /// <summary>
        /// Render an absolute Fusion path and return the result
        /// 
        /// Compared to this.Evaluate, this adds some more comments helpful for debuggin.
        /// </summary>
        /// <param name="fusionPath"></param>
        /// <returns></returns>
        public string Render(string fusionPath)
        {
            string output;
            try
            {
                output = EvaluateInternal(fusionPath, FailureBehavior.Exception).ToString();
                if (settings.DebugMode)
                {
                    var contextKeys = string.Join(", ", GetCurrentContext().Keys);
                    output = string.Format($"\n<!-- Beginning to render TS path \"{fusionPath}\" (Context: {contextKeys}) -->{output}\n<!-- End to render TS path \"{fusionPath}\" (Context: {contextKeys}) -->");
                }
            }
            catch (Exception e)
            {
                output = HandleRenderingException(fusionPath, e);
            }
            return output;
        }

        /// <summary>
        /// Handle an Exception thrown while rendering Fusion according to
        /// settings specified when creating the runtime in Rendering.ExceptionHandler
        /// </summary>
        /// <param name="fusionPath"></param>
        /// <param name="e"></param>
        /// <param name="useInnerExceptionHandler"></param>
        /// <returns></returns>
        private string HandleRenderingException(string fusionPath, Exception e, bool useInnerExceptionHandler = false)
        {
            var fusionConfiguration = GetConfigurationForPath(fusionPath);
            string exceptionHandlerTypeName;
            if (fusionConfiguration["__meta"]["exceptionHandler"].Value != null)
            {
                exceptionHandlerTypeName = (string)fusionConfiguration["__meta"]["exceptionHandler"].Value;
            }
            else
            {
                if (useInnerExceptionHandler)
                {
                    exceptionHandlerTypeName = settings.Rendering.InnerExceptionHandler;
                }
                else
                {
                    exceptionHandlerTypeName = settings.Rendering.ExceptionHandler;
                }
            }
            ExceptionHandlers.AbstractExceptionHandler exceptionHandler = exceptionHandlerFactory.Get(exceptionHandlerTypeName);
            exceptionHandler.Runtime = this;
            if (!string.IsNullOrEmpty(fusionConfiguration.ObjectType))
            {
                fusionPath += $"<{fusionConfiguration.ObjectType}>";
            }
            return exceptionHandler.HandleRenderingException(fusionPath, e);
        }

        /// <summary>
        /// Determine if the given Fusion path is renderable, which means it exists and has an implementation.
        /// </summary>
        /// <param name="fusionPath"></param>
        /// <returns></returns>
        public bool CanRender(string fusionPath)
        {
            var fusionConfiguration = GetConfigurationForPath(fusionPath);
            return CanRenderWithConfiguration(fusionConfiguration);
        }

        /// <summary>
        /// Internal evaluation if given configuration is renderable
        /// </summary>
        /// <param name="fusionConfiguration"></param>
        /// <returns></returns>
        protected bool CanRenderWithConfiguration(FusionAst fusionConfiguration)
        {
            if (HasExpressionOrValue(fusionConfiguration))
            {
                return true;
            }
            if (fusionConfiguration["__meta"]["class"].Value != null && fusionConfiguration.ObjectType.Length > 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Internal evaluation method of absolute fusionPath
        /// </summary>
        /// <param name="fusionPath"></param>
        /// <param name="behaviorIfPathNotFound"></param>
        /// <param name="contextObject">The object which will be "this" in Eel expressions, if any</param>
        /// <returns></returns>
        protected object EvaluateInternal(string fusionPath, FailureBehavior behaviorIfPathNotFound, AbstractFusionObject contextObject = null)
        {
            var needToPopContext = false;
            var needToPopApply = false;
            lastEvaluationStatus = EvaluationStatus.Executed;
            var fusionConfiguration = GetConfigurationForPath(fusionPath);
            var cacheContext = runtimeContentCache.Enter(new Cache.FusionObjectCacheConfiguration(), fusionPath);

            var currentProperties = GetCurrentApplyValues();
            if (currentProperties.ContainsKey(fusionPath))
            {
                if (!EvaluateIfCondition(fusionConfiguration, fusionPath, contextObject))
                {
                    return null;
                }
                return EvaluateProcessors(currentProperties[fusionPath].Value, fusionConfiguration, fusionPath, contextObject);
            }

            if (!CanRenderWithConfiguration(fusionConfiguration))
            {
                FinalizePathEvaluation(cacheContext);
                ThrowExceptionForUnrenderablePathIfNeeded(fusionPath, fusionConfiguration, behaviorIfPathNotFound);
                lastEvaluationStatus = EvaluationStatus.Skipped;
                return null;
            }
            object output = null;

            try
            {
                if (HasExpressionOrValue(fusionConfiguration))
                {
                    return EvaluateExpressionOrValueInternal(fusionPath, fusionConfiguration, cacheContext, contextObject);
                }
                needToPopApply = PrepareApplyValuesForFusionPath(fusionPath, fusionConfiguration);
                var fusionObject = InstantiateFusionObject(fusionPath, fusionConfiguration);
                needToPopContext = PrepareContextForFusionObject(fusionObject, fusionPath, fusionConfiguration, cacheContext);
                output = EvaluateObjectOrRetrieveFromCache(fusionObject, fusionPath, fusionConfiguration, cacheContext);
            }
            catch (Exception e)
            {
                FinalizePathEvaluation(cacheContext, needToPopContext, needToPopApply);
                // todo error handling
                Console.WriteLine(e.Message);
                // Console.WriteLine(e.StackTrace);
                // throw e;
            }
            FinalizePathEvaluation(cacheContext, needToPopContext, needToPopApply);
            return output;
        }

        /// <summary>
        /// Possibly prepares a new "@apply" context for the current fusionPath and pushes it to the stack.
        /// Returns true to express that new properties were puwhed and have to be popped during finalizePathEvaluation.
        /// 
        /// Since "@apply" are not inherited every call of this method leads to a completely new "@apply" context,
        /// which is null by default
        /// </summary>
        /// <param name="fusionPath"></param>
        /// <param name="fusionConfiguration"></param>
        /// <returns></returns>
        private bool PrepareApplyValuesForFusionPath(string fusionPath, FusionAst fusionConfiguration)
        {
            var spreadValues = EvaluateApplyValues(fusionConfiguration, fusionPath);
            PushApplyValues(spreadValues);
            return true;
        }

        /// <summary>
        /// Evaluate "@apply" for the given fusion key.
        /// 
        /// If apply-definitions are found they are evaluated and the returned keys are combined.
        /// The result is returnd as dict with the following structure:
        /// 
        /// <pre>
        /// {
        ///     { "fuisonPath/key_1", { Key: "key_1", Value: "evaluated value 1"} },
        ///     { "fuisonPath/key_2", { Key: "key_2", Value: "evaluated value 2"} },
        /// }
        /// </pre>
        /// </summary>
        /// <param name="configurationWithEventualProperties"></param>
        /// <param name="fusionPath"></param>
        /// <returns></returns>
        private Dictionary<string, KeyValuePair<string, object>> EvaluateApplyValues(FusionAst configurationWithEventualProperties, string fusionPath)
        {
            var result = new Dictionary<string, KeyValuePair<string, object>>();
            if (!configurationWithEventualProperties["__meta"]["apply"].Equals(null))
            {
                var fusionObjectType = configurationWithEventualProperties.ObjectType;
                if (!Regex.IsMatch(fusionPath, @"<[^>]*>$"))
                {
                    fusionPath += $"<{fusionObjectType}>";
                }
                var propertiesConfiguration = configurationWithEventualProperties["__meta"]["apply"];
                // TODO sort
                var sortedKeys = propertiesConfiguration.Children.Keys.Select(x => x.ToString());
                foreach (var key in sortedKeys)
                {
                    if (key[0] == '_' && key[1] == '_' && Parser.ReservedKeys.Contains(key))
                    {
                        continue;
                    }
                    var singleAppyPath = fusionPath + "/__meta/apply/" + key;
                    if (!EvaluateIfCondition(propertiesConfiguration[key], singleAppyPath))
                    {
                        continue;
                    }
                    if (propertiesConfiguration.Children.ContainsKey("expression"))
                    {
                        singleAppyPath += "/expression";
                    }
                    var singleApplyValues = EvaluateInternal(singleAppyPath, FailureBehavior.Exception);
                    if (GetLastEvaluationStatus() != EvaluationStatus.Skipped && singleApplyValues is System.Collections.IDictionary)
                    {
                        foreach (KeyValuePair<string, object> kvp in (System.Collections.IDictionary)singleApplyValues)
                        {
                            if (kvp.Key[0] == '_' && kvp.Key[1] == '_' && Parser.ReservedKeys.Contains(kvp.Key))
                            {
                                continue;
                            }
                            result[fusionPath + '/' + kvp.Key] = kvp;
                        }
                    }
                }
            }
            return result;
        }

        private void ThrowExceptionForUnrenderablePathIfNeeded(string fusionPath, FusionAst fusionConfiguration, FailureBehavior behaviorIfPathNotFound)
        {
            // System.Console.WriteLine("Could not render at path " + fusionPath);
            if (!string.IsNullOrEmpty(fusionConfiguration.ObjectType))
            {
                var objectType = fusionConfiguration.ObjectType;
                throw new FusionException($"The fusion object at path \"{fusionPath}\" could not be rendered:\n\t\tThe fusion object `{objectType}` is not completely defined (missing property `@class`). Most likely you didn't inherit from a basic object.");
            }
            if (behaviorIfPathNotFound == FailureBehavior.Exception)
            {
                throw new FusionException($"No fusion object found in path \"{fusionPath}\"\n\t\tPlease make sure to define one in your Fusion configuration.");
            }
        }

        /// <summary>
        /// Possibly prepares a new context for the current FusionObject and cache context and pushes it to the stack.
        /// Returns if a new context was pushed to the stack or not.
        /// </summary>
        /// <param name="fusionObject"></param>
        /// <param name="fusionPath"></param>
        /// <param name="fusionConfiguration"></param>
        /// <param name="cacheContext"></param>
        /// <returns></returns>
        private bool PrepareContextForFusionObject(AbstractFusionObject fusionObject, string fusionPath, FusionAst fusionConfiguration, object cacheContext)
        {
            var contextArray = GetCurrentContext();
            var newContextArray = new Dictionary<string, object>(contextArray);
            if (fusionConfiguration["__meta"]["context"] != null)
            {
                foreach (var context in fusionConfiguration["__meta"]["context"].Children)
                {
                    var contextValue = EvaluateInternal(fusionPath + "/__meta/context/" + context.Key, FailureBehavior.Exception, fusionObject);
                    newContextArray.Add(context.Key, contextValue);
                }
                PushContextArray(newContextArray);
                return true;
            }
            return false;
        }

        private object EvaluateObjectOrRetrieveFromCache(AbstractFusionObject fusionObject, string fusionPath, FusionAst fusionConfiguration, Cache.EvaluationContext cacheContext)
        {
            object output = null;
            var evaluateObject = true;
            var evaluationStatus = EvaluationStatus.Skipped;
            if (runtimeContentCache.preEvaluate(cacheContext, fusionObject, out object cachedResult))
            {
                return cachedResult;
            }
            if (!EvaluateIfCondition(fusionConfiguration, fusionPath, fusionObject))
            {
                evaluateObject = false;
            }
            if (evaluateObject)
            {
                output = fusionObject.Evaluate();
                evaluationStatus = EvaluationStatus.Executed;
            }
            lastEvaluationStatus = evaluationStatus;
            if (evaluateObject)
            {
                output = EvaluateProcessors(output, fusionConfiguration, fusionPath, fusionObject);
            }
            // TODO
            // runtimeContentCache.postProcess(cacheContext, fusionObject, output);
            return output;
        }

        private object EvaluateExpressionOrValueInternal(string fusionPath, FusionAst fusionConfiguration, Cache.EvaluationContext cacheContext, AbstractFusionObject contextObject)
        {
            if (!EvaluateIfCondition(fusionConfiguration, fusionPath, contextObject))
            {
                FinalizePathEvaluation(cacheContext);
                return null;
            }
            var evaluatedExpression = EvaluateEelExpressionOrSimpleValueWithProcessor(fusionPath, fusionConfiguration, contextObject);
            FinalizePathEvaluation(cacheContext);
            return evaluatedExpression;
        }

        private object EvaluateEelExpressionOrSimpleValueWithProcessor(string fusionPath, FusionAst valueConfiguration, AbstractFusionObject contextObject)
        {
            object evaluatedValue;
            if (valueConfiguration.EelExpression.Length > 0)
            {
                evaluatedValue = EvaluateEelExpression(valueConfiguration.EelExpression, contextObject);
            }
            else
            {
                evaluatedValue = valueConfiguration.Value;
            }
            evaluatedValue = EvaluateProcessors(evaluatedValue, valueConfiguration, fusionPath, contextObject);
            return evaluatedValue;
        }

        private object EvaluateEelExpression(string eelExpression, AbstractFusionObject contextObject)
        {
            // TODO
            return eelExpression;
        }

        protected void FinalizePathEvaluation(Cache.EvaluationContext cacheContext, bool needToPopContext = false, bool needToPopApplyValues = false)
        {
            if (needToPopContext)
            {
                PopContext();
            }
            if (needToPopApplyValues)
            {
                PopApplyValues();
            }
            runtimeContentCache.leave(cacheContext);
        }

        protected FusionAst GetConfigurationForPath(string fusionPath)
        {
            var pathParts = fusionPath.Split('/');
            var configuration = this.configuration;

            var pathUntilNow = "";
            Dictionary<string, FusionAst> currentPrototypeDefinitions = new Dictionary<string, FusionAst>();
            if (configuration["__prototypes"] != null)
            {
                currentPrototypeDefinitions = ((FusionAst)configuration["__prototypes"]).Children;
            }
            foreach (var pathPart in pathParts)
            {
                pathUntilNow += "/" + pathPart;
                // cache
                configuration = MatchCurrentPathPart(pathPart, configuration, currentPrototypeDefinitions);
            }
            return configuration;
        }

        private FusionAst MatchCurrentPathPart(string pathPart, FusionAst previousConfiguration, Dictionary<string, FusionAst> currentPrototypeDefinitions)
        {
            Match matches = Regex.Match(pathPart, @"^([^<]*)(<(.*?)>)?$");
            if (!matches.Success)
            {
                throw new FusionException($"Path part `{pathPart}` not well-formed");
            }
            var currentPathSegment = matches.Groups[1].Value;
            var configuration = new FusionAst();
            if (previousConfiguration.Children.ContainsKey(currentPathSegment))
            {
                configuration = (FusionAst)previousConfiguration[currentPathSegment].Clone();
            }
            if (configuration["__prototypes"] != null)
            {
                currentPrototypeDefinitions = new Dictionary<string, FusionAst>(currentPrototypeDefinitions);
                foreach (var kv in configuration["__prototypes"].Children)
                {
                    if (!currentPrototypeDefinitions.ContainsKey(kv.Key))
                    {
                        currentPrototypeDefinitions.Add(kv.Key, kv.Value);
                    }
                    else
                    {
                        var clone = (FusionAst)currentPrototypeDefinitions[kv.Key].Clone();
                        clone.Merge(kv.Value);
                        currentPrototypeDefinitions[kv.Key] = clone;
                    }
                }
            }
            string currentPathSegmentType = null;
            if (configuration.ObjectType.Length > 0)
            {
                currentPathSegmentType = configuration.ObjectType;
            }
            if (matches.Groups[3].Value.Length > 0)
            {
                currentPathSegmentType = matches.Groups[3].Value;
            }

            if (currentPathSegmentType != null)
            {
                configuration.ObjectType = currentPathSegmentType;
                configuration = MergePrototypesWithConfigurationForPathSegment(configuration, currentPrototypeDefinitions);
            }
            if (!HasExpressionOrValue(configuration) && configuration.ObjectType.Length == 0 && configuration["__meta"]["class"] == null && configuration["__meta"]["process"] == null)
            {
                configuration.Value = "";
            }
            return configuration;
        }

        private FusionAst MergePrototypesWithConfigurationForPathSegment(FusionAst configuration, Dictionary<string, FusionAst> currentPrototypeDefinitions)
        {
            var currentPathSegmentType = configuration.ObjectType;
            if (currentPrototypeDefinitions.ContainsKey(currentPathSegmentType))
            {
                var prototypeMergingOrder = new List<string>() { currentPathSegmentType };
                if (currentPrototypeDefinitions[currentPathSegmentType]["__prototypeChain"].Value != null)
                {
                    prototypeMergingOrder.AddRange((string[])currentPrototypeDefinitions[currentPathSegmentType]["__prototypeChain"].Value);
                }
                var currentPrototypeWithInheritanceTakenIntoAccount = new FusionAst();
                foreach (var prototypeName in prototypeMergingOrder)
                {
                    if (!currentPrototypeDefinitions.ContainsKey(prototypeName))
                    {
                        throw new FusionException($"The Fusion object `{prototypeName}` which you tried to inherit from does not exist. Maybe you have a typo on the right hand side of your inheritance statement for {currentPathSegmentType}");
                    }
                    currentPrototypeWithInheritanceTakenIntoAccount.Merge(currentPrototypeDefinitions[prototypeName]);
                }
                configuration.Merge(currentPrototypeWithInheritanceTakenIntoAccount);
            }
            return configuration;
        }

        protected AbstractFusionObject InstantiateFusionObject(string fusionPath, FusionAst fusionConfiguration)
        {
            var fusionObjectType = fusionConfiguration.ObjectType;
            var fusionObjectClassName = (string)fusionConfiguration["__meta"]["class"].Value;
            if (fusionObjectClassName != null)
            {
                fusionObjectClassName = fusionObjectClassName.Replace("\\", ".");
            }
            if (!Regex.IsMatch(fusionPath, "<[^>]*>$"))
            {
                fusionPath += $"<{fusionObjectType}>";
            }

            Type fusionObjectClassType;
            try
            {
                fusionObjectClassType = Type.GetType(fusionObjectClassName);
            }
            catch (Exception)
            {
                throw new FusionException($"The implementation class `{fusionObjectClassName}` defined for Fusion object of type `{fusionObjectType}` does not exist. Maybe a typo in the @class property");
            }

            AbstractFusionObject fusionObject;
            try
            {
                fusionObject = (AbstractFusionObject)Activator.CreateInstance(fusionObjectClassType, new object[] { this, fusionPath, fusionObjectType });
            }
            catch (Exception)
            {
                throw new FusionException($"Could not invoke fusion object implementation class `{fusionObjectClassName}` defined for Fusion object of type `{fusionObjectType}`");
            }
            if (!(fusionObject is AbstractFusionObject))
            {
                throw new FusionException($"Fusion object implementation class `{fusionObjectClassName}` defined for Fusion object of type `{fusionObjectType}` does not extends AbstractFusionObject");
            }
            if (IsArrayFusionObject(fusionObject))
            {
                if (fusionConfiguration["__meta"]["ignoreProperties"] != null)
                {
                    var evaluatedIgnores = Evaluate(fusionPath + "/__meta/ignoreProperties", fusionObject);
                    ((AbstractArrayFusionObject)fusionObject).SetIgnoreProperties(evaluatedIgnores is string[] ? (string[])evaluatedIgnores : new string[] { });
                }
                SetPropertiesOnFusionObject((AbstractArrayFusionObject)fusionObject, fusionConfiguration);
            }
            return fusionObject;
        }

        /// <summary>
        /// Checks if the current fusionAst hold an Eel expression or a simple value.
        /// </summary>
        /// <param name="fusionConfiguration"></param>
        /// <returns></returns>
        private bool HasExpressionOrValue(FusionAst fusionConfiguration)
        {
            return !string.IsNullOrEmpty(fusionConfiguration.EelExpression) || fusionConfiguration.Value != null;
        }

        protected void SetPropertiesOnFusionObject(AbstractArrayFusionObject fusionObject, FusionAst fusionConfiguration)
        {
            var propertiesProperty = fusionObject
                .GetType()
                .GetField("properties", BindingFlags.Instance | BindingFlags.NonPublic);
            if (propertiesProperty != null)
            {
                propertiesProperty.SetValue(
                    fusionObject,
                    fusionConfiguration.Children.Keys
                        .Select(k => k.ToString())
                        .Where(k => !Parser.ReservedKeys.Contains(k))
                        .ToArray()
                );
            }
            else
            {
                System.Console.WriteLine("no properties property");
                System.Console.WriteLine(fusionObject.GetType());
                System.Console.WriteLine(fusionObject.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Length);
            }
            // handle apply
        }

        /// <summary>
        /// Evaluate processors on given value
        /// </summary>
        /// <param name="valueToProcess"></param>
        /// <param name="configurationWithEventualProcessors"></param>
        /// <param name="fusionPath"></param>
        /// <param name="contextObject"></param>
        /// <returns></returns>
        protected object EvaluateProcessors(object valueToProcess, FusionAst configurationWithEventualProcessors, string fusionPath, AbstractFusionObject contextObject)
        {
            if (configurationWithEventualProcessors["__meta"]["processors"] != null)
            {
                var processorConfiguration = (FusionAst)configurationWithEventualProcessors["__meta"]["processors"].Clone();
                // TODO sorting
                var sortedKeys = processorConfiguration.Children.Keys.Select(x => x.ToString());
                foreach (var key in sortedKeys)
                {
                    var processorPath = fusionPath + "/__meta/process/" + key;
                    if (!EvaluateIfCondition(processorConfiguration[key], processorPath, contextObject))
                    {
                        continue;
                    }
                    if (processorConfiguration[key].Children.ContainsKey("expression"))
                    {
                        processorPath += "/expression";
                    }
                    PushContext("value", valueToProcess);
                    var result = EvaluateInternal(processorPath, FailureBehavior.Exception, contextObject);
                    if (GetLastEvaluationStatus() != EvaluationStatus.Skipped)
                    {
                        valueToProcess = result;
                    }
                    PopContext();
                }
            }
            return valueToProcess;
        }

        /// <summary>
        /// Evaluate eventually existing meta "@if" conditionals inside the given configuration and path
        /// </summary>
        /// <param name="configurationWithEventualIf"></param>
        /// <param name="configurationPath"></param>
        /// <param name="contextObject"></param>
        /// <returns></returns>
        protected Boolean EvaluateIfCondition(FusionAst configurationWithEventualIf, string configurationPath, AbstractFusionObject contextObject = null)
        {
            if (configurationWithEventualIf["__meta"]["if"] != null)
            {
                foreach (var child in configurationWithEventualIf["__meta"]["if"].Children)
                {
                    var conditionValue = EvaluateInternal(configurationPath + "/__meta/if/" + child.Key, FailureBehavior.Exception, contextObject);
                    if (!(bool)conditionValue)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        protected bool IsArrayFusionObject(AbstractFusionObject fusionObject)
        {
            return (fusionObject is AbstractArrayFusionObject);
        }

        public void SetEnableContentCache(bool enabled)
        {
            runtimeContentCache.EnableContentCache = enabled;
        }
    }

    public class RuntimeConfiguration
    {
        public bool DebugMode;

        public RuntimeRenderingConfiguration Rendering;

        public RuntimeConfiguration()
        {
            this.Rendering = new RuntimeRenderingConfiguration();
        }

        public class RuntimeRenderingConfiguration
        {
            public string InnerExceptionHandler;
            public string ExceptionHandler;
        }
    }
}