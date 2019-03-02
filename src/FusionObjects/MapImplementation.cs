using System;
using System.Collections;
using System.Collections.Generic;

namespace Prgfx.Fusion.FusionObjects
{
    public class MapImplementation : AbstractFusionObject
    {

        protected int numberOfRenderedNodes = 0;

        public MapImplementation(Runtime runtime, string path, string fusionObjectName) : base(runtime, path, fusionObjectName)
        {
        }

        protected object GetItems()
        {
            return FusionValue("items");
        }

        protected string GetItemName()
        {
            return (string)FusionValue("itemName");
        }

        protected string GetItemKey()
        {
            return (string)FusionValue("itemKey");
        }

        public string GetIterationName()
        {
            return (string)FusionValue("iterationName");
        }

        public override object Evaluate()
        {
            var collection = GetItems();
            var result = new Dictionary<string, object>();
            if (collection == null)
            {
                return result;
            }
            numberOfRenderedNodes = 0;
            var itemName = GetItemName();
            if (string.IsNullOrEmpty(itemName))
            {
                throw new FusionException("The collection needs an itemName to be set.");
            }
            var itemKey = GetItemKey();
            var iterationName = GetIterationName();
            if (collection is Dictionary<string, object>)
            {
                EvaluateDictionary((Dictionary<string, object>)collection, itemName, itemKey, iterationName, out result);
            }
            else if (collection is ICollection)
            {
                EvaluateEnumerable((ICollection)collection, itemName, itemKey, iterationName, out result);
            }
            else
            {
                throw new FusionException("Cannot process collection");
            }
            return result;
        }

        private void EvaluateEnumerable(ICollection collection, string itemName, string itemKey, string iterationName, out Dictionary<string, object> result)
        {
            result = new Dictionary<string, object>();
            foreach (var item in collection){
                var context = runtime.GetCurrentContext();
                context[itemName] = item;
                if (!string.IsNullOrEmpty(itemKey)) {
                    context[itemKey] = numberOfRenderedNodes;
                }
                if (!string.IsNullOrEmpty(iterationName)) {
                    context[iterationName] = PrepareIterationInformation(collection.Count);
                }
                runtime.PushContextArray(context);
                result.Add(numberOfRenderedNodes.ToString(), runtime.Evaluate(path + "/itemRenderer"));
                runtime.PopContext();
                numberOfRenderedNodes++;
            }
        }

        private void EvaluateDictionary(Dictionary<string, object> collection, string itemName, string itemKey, string iterationName, out Dictionary<string, object> result)
        {
            result = new Dictionary<string, object>();
            foreach (var kvp in collection){
                var context = runtime.GetCurrentContext();
                context[itemName] = kvp.Value;
                if (!string.IsNullOrEmpty(itemKey)) {
                    context[itemKey] = kvp.Key;
                }
                if (!string.IsNullOrEmpty(iterationName)) {
                    context[iterationName] = PrepareIterationInformation(collection.Count);
                }
                runtime.PushContextArray(context);
                result.Add(kvp.Key, runtime.Evaluate(path + "/itemRenderer"));
                runtime.PopContext();
                numberOfRenderedNodes++;
            }
        }

        private IterationInformation PrepareIterationInformation(int count)
        {
            var cycle = numberOfRenderedNodes + 1;
            return new IterationInformation(){
                index = numberOfRenderedNodes,
                cycle = cycle,
                isFirst = numberOfRenderedNodes == 0,
                isLast = cycle == count,
                isEven = cycle % 2 == 0,
                isOdd = cycle % 2 == 1,
            };
        }
    }

    public struct IterationInformation
    {
        public int index;
        public int cycle;
        public bool isFirst;
        public bool isLast;
        public bool isEven;
        public bool isOdd;
    }
}