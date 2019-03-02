using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace Prgfx.Fusion
{
    public class Parser
    {
        protected Regex SCAN_PATTERN_COMMENT = new Regex(@"
            ^\s*                       # beginning of line; with numerous whitespace
            (
                \#                     # this can be a comment char
                |\/\/                  # or two slashes
                |\/\*                  # or slash followed by star
            )
        ", RegexOptions.IgnorePatternWhitespace);

        protected Regex SCAN_PATTERN_OPENINGCONFINEMENT = new Regex(@"
            ^\s*                      # beginning of line; with numerous whitespace
            (?:                       # first part of a TS path
                @?[a-zA-Z0-9:_\-]+              # Unquoted key
                |""(?:\\\""|[^""])+""           # Double quoted key, supporting more characters like underscore and at sign
                |\'(?:\\\\\'|[^\'])+\'          # Single quoted key, supporting more characters like underscore and at sign
                |prototype\([a-zA-Z0-9.:]+\)    # Prototype definition
            )
            (?:                                 # followed by multiple .<tsPathPart> sections:
                \.
                (?:
                    @?[a-zA-Z0-9:_\-]+            # Unquoted key
                    |""(?:\\\""|[^""])+""         # Double quoted key, supporting more characters like underscore and at sign
                    |\'(?:\\\\\'|[^\'])+'          # Single quoted key, supporting more characters like underscore and at sign
                    |prototype\([a-zA-Z0-9.:]+\)  # Prototype definition
                )
            )*
            \s*                       # followed by multiple whitespace
            \{                        # followed by opening {
            \s*$                      # followed by multiple whitespace (possibly) and nothing else.
        ", RegexOptions.IgnorePatternWhitespace);

        protected Regex SCAN_PATTERN_CLOSINGCONFINEMENT = new Regex(@"
            ^\s*                      # beginning of line; with numerous whitespace
            \}                        # closing confinement
            \s*$                      # followed by multiple whitespace (possibly) and nothing else.
        ", RegexOptions.IgnorePatternWhitespace);

        protected Regex SCAN_PATTERN_OBJECTDEFINITION = new Regex(@"
        	^\s*                             # beginning of line; with numerous whitespace
            (?:
                [a-zA-Z0-9.():@_\-]+         # Unquoted key
                |""(?:\\\""|[^""])+""            # Double quoted key, supporting more characters like underscore and at sign
                |\'(?:\\\\\'|[^\'])+\'       # Single quoted key, supporting more characters like underscore and at sign
            )+
            \s*
            (=|<|>)
        ", RegexOptions.IgnorePatternWhitespace);

        protected Regex SPLIT_PATTERN_OBJECTPATH = new Regex(@"
            \.                         # we split at dot characters...
            (?!                        # which are not inside prototype(...). Thus, the dot does NOT match IF it is followed by:
                [^(]*                  # - any character except (
                \)                     # - the character )
            )
        ", RegexOptions.IgnorePatternWhitespace);

        protected Regex SCAN_PATTERN_OBJECTPATH = new Regex(@"
        	^
			\.?
			(?:
				@?[a-zA-Z0-9:_\-]+              # Unquoted key
				|""(?:\\\""|[^""])+""               # Double quoted key, supporting more characters like underscore and at sign
				|\'(?:\\\\\'|[^\'])+\'          # Single quoted key, supporting more characters like underscore and at sign
				|prototype\([a-zA-Z0-9.:]+\)    # Prototype definition
			)
			(?:
				\.
				(?:
					@?[a-zA-Z0-9:_\-]+              # Unquoted key
					|""(?:\\\""|[^""])+""               # Double quoted key, supporting more characters like underscore and at sign
					|\'(?:\\\\\'|[^\'])+\'          # Single quoted key, supporting more characters like underscore and at sign
					|prototype\([a-zA-Z0-9.:]+\)    # Prototype definition
				)
			)*
		$
        ", RegexOptions.IgnorePatternWhitespace);

        protected Regex SCAN_PATTERN_OBJECTPATHSEGMENT_IS_PROTOTYPE = new Regex(@"^prototype\([a-zA-Z0-9:.]+\)$");

        protected Regex SPLIT_PATTERN_COMMENTTYPE = new Regex(@".*?(#|\/\/|\/\*|\*\/).*");

        protected Regex SPLIT_PATTERN_DECLARATION = new Regex(@"(?<declarationType>[a-zA-Z]+[a-zA-Z0-9]*)\s*:\s*([""']{0,1})(?<declaration>.*)\2");

        protected Regex SPLIT_PATTERN_OBJECTDEFINITION = new Regex(@"
        	^\s*                      # beginning of line; with numerous whitespace
            (?<ObjectPath>           # begin ObjectPath
                \.?
                (?:
                    @?[a-zA-Z0-9:_\-]+              # Unquoted key
                    |""(?:\\\""|[^""])+""               # Double quoted key, supporting more characters like underscore and at sign
                    |\'(?:\\\\\'|[^\'])+\'          # Single quoted key, supporting more characters like underscore and at sign
                    |prototype\([a-zA-Z0-9.:]+\)    # Prototype definition
                )
                (?:
                    \.
                    (?:
                        @?[a-zA-Z0-9:_\-]+              # Unquoted key
                        |""(?:\\\""|[^""])+""               # Double quoted key, supporting more characters like underscore and at sign
                        |\'(?:\\\\\'|[^\'])+\'          # Single quoted key, supporting more characters like underscore and at sign
                        |prototype\([a-zA-Z0-9.:]+\)    # Prototype definition
                    )
                )*
            )
            \s*
            (?<Operator>             # the operators which are supported
                =|<|>
            )
            \s*
            (?<Value>                # the remaining line inside the value
                .*?
            )
            \s*
            (?<OpeningConfinement>
                (?<![${])\{           # optionally followed by an opening confinement
            )?
            \s*$
        ", RegexOptions.IgnorePatternWhitespace);

        protected Regex SPLIT_PATTERN_VALUENUMBER = new Regex(@"^\s*-?\d+\s*$");

        protected Regex SPLIT_PATTERN_VALUEFLOATNUMBER = new Regex(@"^\s*-?\d+(\.\d+)?\s*$");

        protected Regex SPLIT_PATTERN_VALUELITERAL = new Regex(@"^""([^""\\\\]*(?>\\\\.[^""\\\\]*)*)""|'([^'\\\\]*(?>\\\\.[^'\\\\]*)*)\'$");
        protected Regex SPLIT_PATTERN_VALUEMULTILNELITERAL = new Regex(@"
            ^(
			(?<DoubleQuoteChar>"")
			(?<DoubleQuoteValue>
				(?:\\\\.
				|
				[^\\""])*
			)
			|
			(?<SingleQuoteChar>')
			(?<SingleQuoteValue>
				(?:\\\\.
				|
				[^\\'])*
			)
            )
        ", RegexOptions.IgnorePatternWhitespace);

        protected Regex SPLIT_PATTERN_VALUEBOOLEAN = new Regex(@"^\s*(TRUE|FALSE|true|false)\s*$");

        protected Regex SPLIT_PATTERN_VALUENULL = new Regex(@"^\s*(NULL|null)\s*$");

        protected Regex SCAN_PATTERN_VALUE_OBJECTTYPE = new Regex(@"
        	^\s*                      # beginning of line; with numerous whitespace
            (?:                       # non-capturing submatch containing the namespace followed by "":"" (optional)
                (?<namespace>
                    [a-zA-Z0-9.]+     # namespace alias (cms, …) or fully qualified namespace (Neos.Neos, …)
                )
                :                     # : as delimiter
            )?
            (?<unqualifiedType>
                [a-zA-Z0-9.]+         # the unqualified type
            )
            \s*$
        ", RegexOptions.IgnorePatternWhitespace);

        protected Regex SCAN_PATTERN_DSL_EXPRESSION_START = new Regex(@"^[a-zA-Z0-9\.]+`");
        protected Regex SPLIT_PATTERN_DSL_EXPRESSION = new Regex(@"^(?<identifier>[a-zA-Z0-9\.]+)`(?<code>[^`]*)`$");

        public static string[] ReservedKeys = { "__meta", "__prototypes", "__prototypeObjectName", "__prototypeChain", "__value", "__objectType", "__eelExpression" };

        protected int currentLineNumber;

        protected string[] currentSourceCodeLines;

        protected bool currentBlockCommentState = false;

        protected Stack<string> currentObjectPathStack;

        protected FusionAst objectTree;

        protected DslFactory dslFactory;

        public void SetDslFactory(DslFactory dslFactory)
        {
            this.dslFactory = dslFactory;
        }

        public FusionAst Parse(string sourceCode, bool buildPrototypeHierarchy = true)
        {
            this.Initialize();
            sourceCode = sourceCode.Replace("\r\n", "\n");
            this.currentSourceCodeLines = sourceCode.Split('\n');
            string fusionLine;
            while ((fusionLine = getNextFusionLine()) != null)
            {
                ParseFusionLine(fusionLine);
            }
            if (buildPrototypeHierarchy)
            {
                BuildPrototypeHierarchy();
            }
            return objectTree;
        }

        protected void Initialize()
        {
            currentLineNumber = 0;
            currentBlockCommentState = false;
            currentObjectPathStack = new Stack<string>();
            objectTree = new FusionAst();
        }

        protected string getNextFusionLine()
        {
            if (currentSourceCodeLines.Length > currentLineNumber)
            {
                return currentSourceCodeLines[currentLineNumber++];
            }
            return null;
        }

        protected void ParseFusionLine(string fusionLine)
        {
            fusionLine = fusionLine.Trim();
            if (currentBlockCommentState)
            {
                ParseComment(fusionLine);
            }
            else
            {
                if (fusionLine.Length == 0)
                {
                    return;
                }
                else if (SCAN_PATTERN_COMMENT.IsMatch(fusionLine))
                {
                    ParseComment(fusionLine);
                }
                else if (SCAN_PATTERN_OPENINGCONFINEMENT.IsMatch(fusionLine))
                {
                    ParseConfinementBlock(fusionLine, true);
                }
                else if (SCAN_PATTERN_CLOSINGCONFINEMENT.IsMatch(fusionLine))
                {
                    ParseConfinementBlock(fusionLine, false);
                }
                else if (SCAN_PATTERN_OBJECTDEFINITION.IsMatch(fusionLine))
                {
                    ParseObjectDefinition(fusionLine);
                }
                else
                {
                    throw new ParserException($"Syntax error in line {currentLineNumber + 1}. ({fusionLine})");
                }
            }
        }

        protected void ParseComment(string fusionLine)
        {
            Match matches = SPLIT_PATTERN_COMMENTTYPE.Match(fusionLine);
            if (matches.Success)
            {
                switch (matches.Groups[1].Value)
                {
                    case "/*":
                        currentBlockCommentState = true;
                        break;
                    case "*/":
                        if (!currentBlockCommentState)
                        {
                            throw new ParserException("Unexpected closing block comment without matching opening block comment.");
                        }
                        currentBlockCommentState = true;
                        ParseFusionLine(fusionLine.Substring(fusionLine.IndexOf(matches.Groups[1].Value) + 2));
                        break;
                    case "#":
                    case "//":
                    default:
                        break;
                }
            }
            else if (!currentBlockCommentState)
            {
                throw new ParserException($"No comment type matched although the comment scan regex matched the Fusion line ({fusionLine}).");
            }
        }

        protected void ParseConfinementBlock(string fusionLine, bool isOpeningConfinement)
        {
            if (isOpeningConfinement)
            {
                var result = fusionLine.Trim().TrimEnd('{').TrimEnd();
                currentObjectPathStack.Push(GetCurrentObjectPathPrefix() + result);
            }
            else
            {
                if (currentObjectPathStack.Count < 1)
                {
                    throw new ParserException("Unexpected closing confinement without matching opening confinement. Check the number of your curly braces.");
                }
                currentObjectPathStack.Pop();
            }
        }

        protected void ParseObjectDefinition(string fusionLine)
        {
            Match match = SPLIT_PATTERN_OBJECTDEFINITION.Match(fusionLine);
            if (!match.Success)
            {
                throw new ParserException($@"Invalid object definition ""{fusionLine}""");
            }
            var objectPath = GetCurrentObjectPathPrefix() + match.Groups["ObjectPath"].Value;
            switch (match.Groups["Operator"].Value)
            {
                case "=":
                    ParseValueAssignment(objectPath, match.Groups["Value"].Value);
                    break;
                case ">":
                    ParseValueUnAssignment(objectPath);
                    break;
                case "<":
                    ParseValueCopy(match.Groups["Value"].Value, objectPath);
                    break;
            }
            if (match.Groups["OpeningConfinement"].Value.Length > 0)
            {
                ParseConfinementBlock(match.Groups["ObjectPath"].Value, true);
            }
        }

        protected void ParseValueAssignment(string objectPath, string value)
        {
            var processedValue = GetProcessedValue(value);
            SetValueInObjectTree(GetParsedObjectPath(objectPath), processedValue);
        }

        protected void ParseValueUnAssignment(string objectPath)
        {
            SetValueInObjectTree(GetParsedObjectPath(objectPath), null);
        }

        protected void ParseValueCopy(string sourceObjectPath, string targetObjectPath)
        {
            var sourceObjectPathArray = GetParsedObjectPath(sourceObjectPath);
            var targetObjectPathArray = GetParsedObjectPath(targetObjectPath);

            System.Func<string[], bool> isPrototypeDefinition = pathArray => (pathArray.Length >= 2 && pathArray[pathArray.Length - 2] == "__prototypes");
            var sourceIsPrototypeDefinition = isPrototypeDefinition(sourceObjectPathArray);
            var targetIsPrototypeDefinition = isPrototypeDefinition(targetObjectPathArray);

            if (sourceIsPrototypeDefinition || targetIsPrototypeDefinition)
            {
                if (sourceIsPrototypeDefinition && targetIsPrototypeDefinition && sourceObjectPathArray.Length == 2 && targetObjectPathArray.Length == 2)
                {
                    var newTargetPath = new string[targetObjectPathArray.Length + 1];
                    targetObjectPathArray.CopyTo(newTargetPath, 0);
                    newTargetPath[targetObjectPathArray.Length] = "__prototypeObjectName";
                    SetValueInObjectTree(newTargetPath, sourceObjectPathArray.Last());
                }
                else if (sourceIsPrototypeDefinition && targetIsPrototypeDefinition)
                {
                    // at least one is a nested prototype which is not supported
                    throw new ParserException($@"Tried to parse ""{targetObjectPath}"" < ""{sourceObjectPath}"", however  one of the sides is nested (e.g. foo.prototype(Bar)). Setting up prototype inheritance is only supported at the top level: prototype(Foo) < prototype(Bar)");
                }
                else
                {
                    throw new ParserException($@"Tried to parse ""{targetObjectPath}"" < ""{sourceObjectPath}"", however one of the sides is no prototype definition of the form prototype(Foo). It is only allowed to build inheritance chains with prototype objects.");
                }
            }
            else
            {
                var originalValue = GetValueFromObjectTree(sourceObjectPathArray);
                SetValueInObjectTree(targetObjectPathArray, originalValue);
            }
        }

        protected object GetProcessedValue(string value)
        {
            if (SPLIT_PATTERN_VALUENUMBER.IsMatch(value))
            {
                return int.Parse(value);
            }
            else if (SPLIT_PATTERN_VALUEFLOATNUMBER.IsMatch(value))
            {
                return float.Parse(value);
            }
            else if (SPLIT_PATTERN_VALUELITERAL.IsMatch(value))
            {
                Match match = SPLIT_PATTERN_VALUELITERAL.Match(value);
                return match.Groups[2].Value.Length > 0 ? match.Groups[2].Value : match.Groups[1].Value;
            }
            else if (SPLIT_PATTERN_VALUEMULTILNELITERAL.IsMatch(value))
            {
                Match match = SPLIT_PATTERN_VALUEMULTILNELITERAL.Match(value);
                value = match.Groups["SingleQuoteValue"].Value.Length > 0 ? match.Groups["SingleQuoteValue"].Value : match.Groups["DoubleQuoteValue"].Value;
                var closingQuoteChar = match.Groups["SingleQuoteChar"].Value.Length > 0 ? match.Groups["SingleQuoteChar"].Value : match.Groups["DoubleQuoteChar"].Value;
                var regex = new Regex($@"(?<Value>(?:\\{closingQuoteChar}])*)(?<QuoteChar>{closingQuoteChar}?)");
                string fusionLine;
                while ((fusionLine = getNextFusionLine()) != null)
                {
                    Match m = regex.Match(fusionLine);
                    value += m.Groups["Value"].Value;
                    if (m.Groups["QuoteChar"].Value.Length > 0)
                    {
                        break;
                    }
                }
            }
            else if (SPLIT_PATTERN_VALUEBOOLEAN.IsMatch(value))
            {
                return value.ToLower() == "true";
            }
            else if (SPLIT_PATTERN_VALUENULL.IsMatch(value))
            {
                return null;
            }
            else if (SCAN_PATTERN_VALUE_OBJECTTYPE.IsMatch(value))
            {
                Match match = SCAN_PATTERN_VALUE_OBJECTTYPE.Match(value);
                string objectTypeNamespace;
                if (match.Groups["namespace"].Value.Length == 0)
                {
                    // TODO
                    objectTypeNamespace = "Neos.Fusion";
                }
                else
                {
                    objectTypeNamespace = match.Groups["namespace"].Value;
                }
                var unqualifiedType = match.Groups["unqualifiedType"].Value;
                return new FusionAst()
                {
                    ObjectType = $"{objectTypeNamespace}:{unqualifiedType}".Trim()
                };
            }
            else if (value[0] == '$')
            {
                var eelExpressionSoFar = new System.Text.StringBuilder(value);
                string line = value;
                do
                {
                    if (line.Last() == '}')
                    {
                        var eelExpression = eelExpressionSoFar.ToString();
                        if (IsValidEelExpression(eelExpression))
                        {
                            return new FusionAst()
                            {
                                EelExpression = eelExpression.Replace("\n", "")
                            };
                        }
                    }
                    eelExpressionSoFar.Append('\n').Append(line);
                } while ((line = getNextFusionLine()) != null);
                throw new ParserException("Not a valid eel expression");
            }
            else if (SCAN_PATTERN_DSL_EXPRESSION_START.IsMatch(value))
            {
                var dslExpressionSoFar = new System.Text.StringBuilder(value);
                string line;
                while (true)
                {
                    var expressionSoFar = dslExpressionSoFar.ToString();
                    if (expressionSoFar.Last() == '`')
                    {
                        Match m = SPLIT_PATTERN_DSL_EXPRESSION.Match(expressionSoFar);
                        if (m.Success)
                        {
                            return InvokeAndParseDsl(m.Groups["identifier"].Value, m.Groups["code"].Value);
                        }
                    }
                    line = getNextFusionLine();
                    if (line == null)
                    {
                        throw new ParserException($"Syntax error: A multi-line dsl expression starting with \"{value}\" was not closed");
                    }
                    dslExpressionSoFar.Append('\n').Append(line);
                }
            }
            return value;
        }

        protected FusionAst InvokeAndParseDsl(string identifier, string code)
        {
            IFusionDsl dsl = null;
            if (this.dslFactory == null || (dsl = dslFactory.create(identifier)) == null)
            {
                throw new DslException("Unable to create DSL - unintialized DSL-factory?");
            }
            var transpiledFusion = dsl.Transpile(code);
            var parser = new Parser();
            parser.SetDslFactory(dslFactory);
            var temporaryAst = parser.Parse("value = " + transpiledFusion + "\n");
            var processedValue = temporaryAst.Children["value"];
            return processedValue;
        }

        protected bool IsValidEelExpression(string expression)
        {
            return true;
        }

        protected void SetValueInObjectTree(string[] path, object value)
        {
            objectTree.SetValue(path, value);
        }

        protected object GetValueFromObjectTree(string[] path)
        {
            return objectTree.GetValue(path);
        }

        protected string[] GetParsedObjectPath(string objectPath)
        {
            if (!SCAN_PATTERN_OBJECTPATH.IsMatch(objectPath))
            {
                throw new ParserException($@"Syntax error: Invalid object path ""{objectPath}"".");
            }
            if (objectPath[0] == '.')
            {
                objectPath = GetCurrentObjectPathPrefix() + objectPath.Substring(1);
            }
            var objectPathArray = new List<string>();
            foreach (var objectPathSegment in SPLIT_PATTERN_OBJECTPATH.Split(objectPath))
            {
                if (objectPathSegment[0] == '@')
                {
                    objectPathArray.Add("__meta");
                    objectPathArray.Add(objectPathSegment.Substring(1));
                }
                else if (SCAN_PATTERN_OBJECTPATHSEGMENT_IS_PROTOTYPE.IsMatch(objectPathSegment))
                {
                    objectPathArray.Add("__prototypes");
                    objectPathArray.Add(objectPathSegment.Substring(10, objectPathSegment.Length - 11));
                }
                else
                {
                    var key = objectPathSegment;
                    // TODO check reserved keys
                    objectPathArray.Add(UnquoteString(key));
                }
            }
            return objectPathArray.ToArray();
        }

        protected string UnquoteString(string input)
        {
            var value = input;
            switch (input[0])
            {
                case '"':
                    value = Regex.Replace(input, @"(^""|""$)", "").Replace("\\\"", "\"");
                    break;
                case '\'':
                    value = Regex.Replace(input, @"(^'|'$)", "").Replace("\\'", "'");
                    break;
            }
            return value.Replace(@"\\", @"\");
        }

        protected string GetCurrentObjectPathPrefix()
        {
            return currentObjectPathStack.TryPeek(out string lastElementOfStack) ? (lastElementOfStack + ".") : string.Empty;
        }

        protected void BuildPrototypeHierarchy()
        {
            if (objectTree["__prototypes"] == null)
            {
                return;
            }
            foreach (var item in objectTree["__prototypes"].Children)
            {
                List<string> prototypeHierarchy = new List<string>();
                var currentPrototypeName = item.Key;
                while (objectTree["__prototypes"][currentPrototypeName]["__prototypeObjectName"].Value != null)
                {
                    currentPrototypeName = (string)objectTree["__prototypes"][currentPrototypeName]["__prototypeObjectName"].Value;
                    prototypeHierarchy.Add(currentPrototypeName);
                    if (currentPrototypeName == item.Key)
                    {
                        throw new ParserException($"Recursive ineritance found for prototype `{item.Key}`. Prototype chain: " + string.Join(" < ", prototypeHierarchy));
                    }
                }
                if (prototypeHierarchy.Count > 0)
                {
                    prototypeHierarchy.Reverse();
                    objectTree.SetValue(new string[] { "__prototypes", item.Key, "__prototypeChain" }, prototypeHierarchy.ToArray());
                }
            }
        }
    }
}