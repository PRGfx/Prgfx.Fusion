using System.Collections.Generic;
using System.Linq;
using System;

namespace Prgfx.Fusion
{
    public class FusionAst : ICloneable
    {
        public object Value;

        public string EelExpression = "";

        public string ObjectType = "";

        public FusionAst Parent { get; }

        public Dictionary<string, FusionAst> Children;

        public FusionAst()
        {
            this.Children = new Dictionary<string, FusionAst>();
        }

        public FusionAst(FusionAst parent)
        {
            this.Children = new Dictionary<string, FusionAst>();
            this.Parent = parent;
        }

        public object GetValue(string[] objectPathArray)
        {
            var currentKey = objectPathArray.Last();
            objectPathArray = objectPathArray.Take(objectPathArray.Length - 1).ToArray();

            if (objectPathArray.Length == 0)
            {
                if (currentKey == "__value")
                {
                    return Value;
                }
                else if (currentKey == "__eelExpression")
                {
                    return EelExpression;
                }
                else if (currentKey == "__objectType")
                {
                    return ObjectType;
                }
                else
                {
                    if (Children.ContainsKey(currentKey))
                    {
                        return Children[currentKey];
                    }
                    return null;
                }
            }
            else if (!Children.ContainsKey(objectPathArray[0]))
            {
                return Children[objectPathArray[0]].GetValue(objectPathArray.Skip(1).ToArray());
            }
            else
            {
                return null;
            }
        }

        public void SetValue(string[] objectPathArray, object value)
        {
            if (objectPathArray.Length == 0)
            {
                this.Value = value;
            }
            else if (objectPathArray.Length == 1)
            {
                var currentKey = objectPathArray.Last();
                objectPathArray = objectPathArray.Take(objectPathArray.Length - 1).ToArray();
                if (Children.ContainsKey(currentKey) && value == null)
                {
                    Children.Remove(currentKey);
                }
                else if (Children.ContainsKey(currentKey))
                {
                    if (value is FusionAst)
                    {
                        var subTree = value as FusionAst;
                        Children[currentKey].Value = subTree.Value;
                        Children[currentKey].EelExpression = subTree.EelExpression;
                        Children[currentKey].ObjectType = subTree.ObjectType;
                        foreach (var keyValue in subTree.Children)
                        {
                            Children[currentKey].Children.Add(keyValue.Key, keyValue.Value);
                        }
                    }
                }
                else
                {
                    FusionAst newValue;
                    if (value is FusionAst)
                    {
                        newValue = value as FusionAst;
                    }
                    else
                    {
                        newValue = new FusionAst(this) { Value = value };
                    }
                    Children.Add(currentKey, newValue);
                }
            }
            else
            {
                if (!Children.ContainsKey(objectPathArray[0]))
                {
                    Children.Add(objectPathArray[0], new FusionAst(this));
                }
                Children[objectPathArray[0]].SetValue(objectPathArray.Skip(1).ToArray(), value);
            }
        }

        public override string ToString()
        {
            return this.ToString("");
        }

        public string ToString(string indentation)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(indentation).Append("__value: ").AppendLine(Value == null ? "null" : Value.ToString());
            sb.Append(indentation).Append("__eelExpression: ").AppendLine(EelExpression);
            sb.Append(indentation).Append("__objectType: ").AppendLine(ObjectType);
            foreach (var child in Children)
            {
                sb.Append(indentation).Append(child.Key).AppendLine(":");
                sb.AppendLine(child.Value.ToString(indentation + "  "));
            }
            return sb.ToString();
        }

        public object Clone()
        {
            var clone = new FusionAst();
            clone.Value = Value;
            clone.EelExpression = EelExpression;
            clone.ObjectType = ObjectType;
            foreach (var child in Children)
            {
                clone.Children.Add(child.Key, (FusionAst)child.Value.Clone());
            }
            return clone;
        }

        public FusionAst this[string childName]
        {
            get
            {
                // enable access through non-existing children for easy access chaining
                return Children.ContainsKey(childName) ? Children[childName] : new FusionAst();
            }
        }

        public override bool Equals(object other)
        {
            if (other == this)
            {
                return true;
            }
            if (other == null)
            {
                return this.Value == null && this.EelExpression.Length == 0 && this.ObjectType.Length == 0 && this.Children.Count == 0;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return 2;
        }

        public void Merge(FusionAst other)
        {
            if (Value == null)
            {
                Value = other.Value;
            }
            if (EelExpression == null || EelExpression.Length == 0)
            {
                EelExpression = other.EelExpression;
            }
            if (ObjectType == null || ObjectType.Length == 0)
            {
                ObjectType = other.ObjectType;
            }
            foreach (var keyValue in other.Children)
            {
                if (Children.ContainsKey(keyValue.Key))
                {
                    Children[keyValue.Key].Merge(keyValue.Value);
                }
                else
                {
                    Children.Add(keyValue.Key, keyValue.Value);
                }
            }
        }
    }
}