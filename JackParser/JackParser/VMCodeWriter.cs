using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace JackParser
{
    class VMCodeWriter
    {
        #region Data members
        static string _vmCode;
        public static string VmCode
        {
            get { return _vmCode; }
            set { _vmCode = value; }
        }


        static string _className;
        public static string ClassName
        {
            get { return _className; }
            set { _className = value; }
        }

        static BiDictionary<string, int> _staticVariables;
        public static BiDictionary<string, int> StaticVariables
        {
            get { return _staticVariables; }
            set { _staticVariables = value; }
        }

        static BiDictionary<string, int> _localVariables;
        public static BiDictionary<string, int> LocalVariables
        {
            get { return _localVariables; }
            set { _localVariables = value; }
        }

        static BiDictionary<string, int> _arguments;
        public static BiDictionary<string, int> Arguments
        {
            get { return _arguments; }
            set { _arguments = value; }
        }

        static BiDictionary<string, int> _classVaraibles;
        public static BiDictionary<string, int> ClassVaraibles
        {
            get { return _classVaraibles; }
            set { _classVaraibles = value; }
        }

        static Dictionary<string, FunctionType> _functionTypeByName;

        #endregion

        #region C'tors
        static VMCodeWriter()
        {
            VMCodeWriter._arguments = new BiDictionary<string, int>();
            VMCodeWriter._classVaraibles = new BiDictionary<string, int>();
            VMCodeWriter._functionTypeByName = new Dictionary<string, FunctionType>();
            VMCodeWriter._localVariables = new BiDictionary<string, int>();
            VMCodeWriter._staticVariables = new BiDictionary<string, int>();
            VMCodeWriter._vmCode = String.Empty;
            VMCodeWriter.ClassName = String.Empty;
        }
        #endregion

        #region Internal methods
        /// <summary>
        /// Adds the variablbe.
        /// </summary>
        /// <param name="variableModifier">The variable modifier.</param>
        /// <param name="varName">Name of the variable.</param>
        /// <exception cref="System.Exception">Got unknown modifier for variable: +variableModifier</exception>
        internal static void AddVariablbe(string variableModifier, string varName)
        {
            BiDictionary<string, int> varDic;
            if (String.Equals(variableModifier, "field", StringComparison.OrdinalIgnoreCase))
            {
                varDic = VMCodeWriter._classVaraibles;
            }
            else if (String.Equals(variableModifier, "static", StringComparison.OrdinalIgnoreCase))
            {
                varDic = VMCodeWriter._staticVariables;
            }
            else if (String.Equals(variableModifier, "var", StringComparison.OrdinalIgnoreCase))
            {
                varDic = VMCodeWriter._localVariables;
            }
            else
            {
                throw new Exception("Got unknown modifier for variable: " + variableModifier);
            }
            int varIdx = varDic.Count;
            varDic.Add(varName, varIdx);

        }

        /// <summary>
        /// Adds a function declaration to the VM Code.
        /// </summary>
        /// <param name="fModifier">The f modifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="argCount">The number of arguments passed to function.</param>
        /// <exception cref="System.Exception">Got unkown function modifier</exception>
        internal static void AddFunction(string fModifier, string name, int argCount)
        {
            FunctionType fType;
            bool isConstructor = String.Equals(fModifier, "constructor");
            bool isFunction = String.Equals(fModifier, "function");
            bool isMethod = String.Equals(fModifier, "method");
            if (isConstructor || isFunction)
            {
                fType = FunctionType.Function;
            }
            else if (isMethod)
            {
                fType = FunctionType.Method;
            }
            else
            {
                throw new ArgumentException("Got unkown function modifier: " + fModifier);
            }
            WriteFunctionDeclaration(fType, name, argCount);
            if (isConstructor)
            {
                VMCodeWriter.WriteClassMemoryAllocation();
            }

        }

        /// <summary>
        /// Adds the let statement.
        /// </summary>
        /// <param name="statementNode">The node containg statement.</param>
        internal static void AddLetStatement(XmlNode statementNode)
        {
            VMCodeWriter.GetVmCodeFromStatementsExpression(statementNode);
            
            XmlNode identifier = statementNode.SelectSingleNode("letStatement/identifier");
            string varName = identifier.InnerText;

            VMCodeWriter.WritePopStatement(varName);      
           
        }

        

        /// <summary>
        /// Gets the segment and index by variable by it's name.
        /// </summary>
        /// <param name="varName">Name of the variable.</param>
        /// <param name="segment">The segment.</param>
        /// <param name="index">The index.</param>
        /// <exception cref="System.Exception">failed to get variable's index</exception>
        private static void GetSegmentAndIndexByVariableName(string varName, out Segments segment, out int index)
        {
            BiDictionary<string, int> dic = VMCodeWriter.GetDictionaryByVariableName(varName);
            segment = VMCodeWriter.GetSegmentByDictionary(dic);

            if (!dic.TryGetByFirst(varName, out index))
            {
                throw new Exception("failed to get variable's index");
            }
        }

      

        /// <summary>
        /// Adds if statement.
        /// </summary>
        /// <param name="statementNode">The node containg statement</param>        
        internal static void AddIfStatement(XmlNode statementNode)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds the while statement.
        /// </summary>
        /// <param name="statementNode">The node containg statement</param>
        /// <exception cref="System."></exception>
        internal static void AddWhileStatement(XmlNode statementNode)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds the do statement.
        /// </summary>
        /// <param name="statementNode">The node containg statement.</param>
        internal static void AddDoStatement(XmlNode statementNode)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds the return statement.
        /// </summary>
        /// <param name="statementNode">The node containg statement.</param>
        internal static void AddReturnStatement(XmlNode statementNode)
        {
            throw new NotImplementedException();
        } 
        #endregion

        /// <summary>
        /// Gets the segment by dictionary.
        /// </summary>
        /// <param name="dic">The dictionary.</param>
        /// <returns>the segment that the dictionary represents</returns>
        /// <exception cref="System.Exception">failed to get segment by dictionary</exception>
        private static Segments GetSegmentByDictionary(BiDictionary<string, int> dic)
        {
            if (dic.Equals(VMCodeWriter._arguments))
            {
                return Segments.argument;
            }
            if (dic.Equals(VMCodeWriter._classVaraibles))
            {
                return Segments.@this;
            }
            if (dic.Equals(VMCodeWriter._localVariables))
            {
                return Segments.local;
            }
            if (dic.Equals(VMCodeWriter._staticVariables))
            {
                return Segments.@static;
            }

            throw new Exception("failed to get segment by dictionary");
        }


        /// <summary>
        /// Gets the dictionary that the specified variable belongs to.
        /// </summary>
        /// <param name="varName">Name of the variable.</param>
        /// <returns>the dictionary that the specified variable belongs to</returns>
        private static BiDictionary<string, int> GetDictionaryByVariableName(string varName)
        {
            BiDictionary<string, int> dic = null;

            List<BiDictionary<string, int>> potentialDics = new List<BiDictionary<string, int>>
            {
                /*Order matters!*/
                VMCodeWriter._arguments,//must be first
                VMCodeWriter._classVaraibles,
                VMCodeWriter._localVariables,
                VMCodeWriter._staticVariables,

            };
            for (int i = 0; i < potentialDics.Count && dic == null; i++)
            {
                var currDic = potentialDics[i];
                if (currDic.ContainsKey(varName))
                {
                    dic = currDic;
                }
            }

           
            return dic;
        }


        


        /// <summary>
        /// Writes the VM push stetment.
        /// </summary>
        /// <param name="arg">The argument to push from.</param>
        private static void WritePushStetment(StackArgumentObject arg)
        {
            VMCodeWriter.VmCode += String.Format("push {0} {1}", arg.segment, arg.index) + Environment.NewLine;
        }
        /// <summary>
        /// Writes the VM push stetment.
        /// </summary>
        /// <param name="arg">The argument to push from.</param>
        private static void WritePushStetment(string varName)
        {
            Segments segment;
            int index;
            GetSegmentAndIndexByVariableName(varName, out segment, out index);

            VMCodeWriter.WritePushStetment(segment, index);
        }

        /// <summary>
        /// Writes the VM push stetment.
        /// </summary>
        /// <param name="segment">The segments.</param>
        /// <param name="index">The index of variable to push from.</param>
        private static void WritePushStetment(Segments segment, int index)
        {
            VMCodeWriter.WritePushStetment(new StackArgumentObject(segment, index));
        }
        /// <summary>
        /// Writes the pop statement.
        /// </summary>
        /// <param name="arg">The argument to push to.</param>
        private static void WritePopStatement(StackArgumentObject arg)
        {
            VMCodeWriter.VmCode += String.Format("pop {0} {1}", arg.segment, arg.index) + Environment.NewLine;
        }

        /// <summary>
        /// Writes the pop statement.
        /// </summary>
        /// <param name="arg">The variable to pop to.</param>
        private static void WritePopStatement(string varName)
        {
            Segments segment;
            int index;
            GetSegmentAndIndexByVariableName(varName, out segment, out index);

            VMCodeWriter.WritePopStatement(segment, index);
        }
        /// <summary>
        /// Writes the pop statement.
        /// </summary>
        /// <param name="segment">The segment.</param>
        /// <param name="index">The index of variable to pop to.</param>
        private static void WritePopStatement(Segments segment, int index)
        {
            VMCodeWriter.WritePopStatement(new StackArgumentObject(segment, index));
        }
        /// <summary>
        /// Writes the the VM code that allocates memory for class .
        /// </summary>
        private static void WriteClassMemoryAllocation()
        {
            List<StackArgumentObject> argumets = new List<StackArgumentObject>();
            argumets.Add(new StackArgumentObject(Segments.constant,VMCodeWriter._classVaraibles.Count));
           
            VMCodeWriter.WriteCallFunction("Memory.alloc",argumets);
            VMCodeWriter.WritePopStatement(new StackArgumentObject(Segments.pointer,0));
        }

        /// <summary>
        /// Writes the VM code that calls a function
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="argumets">The argumets count passed to function</param>
        private static void WriteCallFunction(string functionName, int argumentCount)
        {
            VMCodeWriter.VmCode += String.Format("call {0} {1}", functionName, argumentCount) + Environment.NewLine;
        }

        /// <summary>
        /// Writes the VM code that calls a function
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="argumets">The argumets to put in stack for function.</param>
        private static void WriteCallFunction(string functionName, List<StackArgumentObject> argumets)
        {
            foreach (StackArgumentObject arg in argumets)
            {
                VMCodeWriter.WritePushStetment(arg);
            }
            VMCodeWriter.WriteCallFunction(functionName, argumets.Count);
        }
        /// <summary>
        /// Adds a function declaration to the VM Code.
        /// </summary>
        /// <param name="fType">Type of the function.</param>
        /// <param name="name">The name of function.</param>
        /// <param name="argCount">The number of arguments passed to function.</param>
        private static void WriteFunctionDeclaration(FunctionType fType, string name, int argCount)
        {
            VMCodeWriter._functionTypeByName.Add(name, fType);
            VMCodeWriter.VmCode += String.Format("function CircleMaker.{0} {1}", name, argCount)+Environment.NewLine;
        }
        /// <summary>
        /// Gets the dictionary by sprcified segment.
        /// </summary>
        /// <param name="segment">The segment.</param>
        /// <returns>the variable dictionary that matches specified segment</returns>
        private static BiDictionary<string, int> GetDictionaryBySegment(Segments segment)
        {
            BiDictionary<string, int> dic = null;
            switch (segment)
            {
                case Segments.constant:
                    dic = null;
                    break;
                case Segments.local:
                    dic = VMCodeWriter._localVariables;
                    break;
                case Segments.argument:
                    dic = VMCodeWriter._arguments;
                    break;
                default:
                    dic = null;
                    break;
            }
            return dic;
        }
        /// <summary>
        /// Gets the vm code from statement's expression node.
        /// </summary>
        /// <param name="statementNode">The statement node.</param>
        /// <returns>the vm code from statement's expression</returns>
        private static void GetVmCodeFromStatementsExpression(XmlNode statementNode)
        {
            XmlNode expressionNode = VMCodeWriter.GetExpressionNodeFromStatement(statementNode);
            VMCodeWriter.ExpressionNodeToVmCode(expressionNode);
            
        }

        /// <summary>
        /// converts an Expressions node to vm code.
        /// </summary>
        /// <param name="expressionNode">The expression node.</param>
        /// <returns>the expression VM code</returns>
        private static void ExpressionNodeToVmCode(XmlNode expressionNode)
        {
            List<string> tokens = VMCodeWriter.GetAllExpressionTokens(expressionNode);
            //VMCodeWriter.ExpressionTokensToVmCode(tokens);

            //return;
            //splittedExpressions.ToString();
            string retString = String.Empty;
            
            string currString = expressionNode.InnerText;
            //string rp = ReversePolishHandler.ToReversePolish(tokens);
            WriteExpressionVM(currString);
            
            


        }

        private static void WriteExpressionVM(string currExpression)
        {
            int expInt;
            if (int.TryParse(currExpression, out expInt)) //expression is a number
            {
                VMCodeWriter.WritePushStetment(Segments.constant, expInt);
            }
            else
            {
                var varDic = VMCodeWriter.GetDictionaryByVariableName(currExpression); 
                bool isVariable = varDic != null;
                if (isVariable)//expression is a variable
                {
                    VMCodeWriter.WritePushStetment(currExpression);
                }
                else if (currExpression.Any(c => SymbolClassifications._oparations.Contains(c.ToString()))) //expression is in form of (exp1 OP exp2) OR (UNARYOP exp)
                {
                    var emuStrings=
                        currExpression.Select<char,string>(c => SymbolClassifications._oparations.Contains(c.ToString()) ? "@" + c + "@" : c.ToString());
                    string revisedExpression = String.Join(String.Empty, emuStrings);
                    string[] splittedExpressions = revisedExpression.Split(new string[] { "@" }, 3, StringSplitOptions.RemoveEmptyEntries);
                    if (splittedExpressions.Length == 2)//(UNARYOP exp)
                    {
                        string exp1 = splittedExpressions[1];
                        string @operator = splittedExpressions[0];

                        VMCodeWriter.WriteExpressionVM(exp1);

                        VMCodeWriter.WriteUnaryOperation(@operator, String.Empty);
                    }
                    else // (exp1 OP exp2)
                    {
                        string exp1 = splittedExpressions[0];
                        string exp2 = splittedExpressions[2];
                        string @operator = splittedExpressions[1];

                        VMCodeWriter.WriteExpressionVM(exp1);
                        VMCodeWriter.WriteExpressionVM(exp2);

                        VMCodeWriter.WriteBinaryOperation(@operator, String.Empty);
                    }

                }
                else if (Regex.IsMatch(currExpression,"^[a-zA-Z_][a-z*A-Z*_*0-9*]*[(]",RegexOptions.None))//function call
                {
                    int openBrackIdx = currExpression.IndexOf("(");
                    int closeBrackIdx = currExpression.IndexOf("(");
                    List<string> expressions = 
                        currExpression.Substring(openBrackIdx+1, currExpression.Length - 2- closeBrackIdx).Split(new char[]{
                       ','}).ToList();
                    //writing parameter expressions
                    for (int i = 0; i < expressions.Count; i++)
                    {
                        //recursion
                        VMCodeWriter.WriteExpressionVM(expressions[i]);
                    }
                    throw new NotImplementedException("Stas, this where i got so far...");
                    //string functionName =Str;
                    //VMCodeWriter.WriteCallFunction(functionName,expressions.Count);
                }
                else
                {
                    throw new Exception("Failed to recognize expression structure: "+currExpression);
                }
            }
        }

        

        private static void WriteBinaryOperation(string @operator, string destinationVarName)
        {
            Dictionary<string, string> operationsNames = new Dictionary<string, string>()
            {
                {"+","add"},
                {"-","sub"},
                {"=","eq"},
                {">","gt"},
                {"<","lt"},
                {"&","and"},
                {"|","or"},

            };

            string operation = operationsNames[@operator];
            VMCodeWriter._vmCode += operation + Environment.NewLine;
            
            if (!String.IsNullOrWhiteSpace(destinationVarName))
            {
                VMCodeWriter.WritePopStatement(destinationVarName);
            }
        }

        private static void WriteUnaryOperation(string @operator, string destinationVarName)
        {
            Dictionary<string, string> operationsNames = new Dictionary<string, string>()
            {
                {"~","not"},
                {"-","neg"},

            };

            string operation = operationsNames[@operator];
            VMCodeWriter._vmCode += operation + Environment.NewLine;

            if (!String.IsNullOrWhiteSpace(destinationVarName))
            {
                VMCodeWriter.WritePopStatement(destinationVarName);
            }
        }

        private static void ExpressionTokensToVmCode(List<string> tokens)
        {
            List<List<string>> splittedExpressions = VMCodeWriter.GetSubExpressions(tokens);
            for (int i = 0; i < splittedExpressions.Count; i++)
            {
                var currExpression = splittedExpressions[i];
                if (currExpression.Contains("("))//it is an expression that holds expressions
                {
                    //The recuresion
                    VMCodeWriter.ExpressionTokensToVmCode(currExpression);
                }
                else
                {
                    //stop condition

                }
                
               
            }
        }

        private static List<List<string>> GetSubExpressions(List<string> tokens)
        {
            List<List<string>> subExpressions = new List<List<string>>();

            List<string> currExpression = null;
            int bracketCounter = 0;
            foreach (string t in tokens)
            {
                if (currExpression == null)
                {
                    currExpression = new List<string>();
                }
                if (t == "(")
                {
                    if (bracketCounter == 0 && currExpression.Count > 0)
                    {
                        subExpressions.Add(currExpression);
                        currExpression = null;
                    }
                    bracketCounter++;
                    continue;
                }

                if (t == ")")
                {
                    bracketCounter--;
                    if (bracketCounter == 0)
                    {
                        subExpressions.Add(currExpression);
                        currExpression = null;
                        continue;
                    }
                }
                currExpression.Add(t);


            }
            if (currExpression != null)
            {
                subExpressions.Add(currExpression);
            }
            return subExpressions;
        }

        private static List<string> GetAllExpressionTokens(XmlNode expressionNode)
        {

            var tokens = VMCodeWriter.GetAllNodeTextRecursive(expressionNode);
            return tokens;
                
        }

        private static List<string> GetAllNodeTextRecursive(XmlNode node)
        {
            var text = new List<string>();

            foreach (XmlNode currNode in node.ChildNodes)
            {
                if (currNode.Value != null)
                {
                    text.Add(currNode.Value);
                }
                //the recursion
                text.AddRange(VMCodeWriter.GetAllNodeTextRecursive(currNode));
            }
            return text;
        }

        

        /// <summary>
        /// Gets the expression node from statement node.
        /// </summary>
        /// <param name="statementNode">The statement node.</param>
        /// <returns>the expression node</returns>
        private static XmlNode GetExpressionNodeFromStatement(XmlNode statementNode)
        {
            XmlNode generalStatementNode = statementNode.ChildNodes[0];
            XmlNode expressionNode = null;

            bool foundExpression = false;
            for (int i = 0; i < generalStatementNode.ChildNodes.Count && !foundExpression; i++)
            {
                XmlNode currNode = generalStatementNode.ChildNodes[i];
                if (String.Equals(currNode.Name, "expression", StringComparison.OrdinalIgnoreCase))
                {
                    expressionNode = currNode;
                    foundExpression = true;
                }
            }
            return expressionNode;
        }

        
        
    }


    #region Sub classes

    class StackArgumentObject
    {
        public Segments segment;
        public int index;

        public StackArgumentObject(Segments segment, int index)
        {
            this.segment = segment;
            this.index = index;
        }
    }

    enum Segments
    {
        constant,
        local,
        argument,
        pointer,
        @static,
        @this,
        that

    }
    public enum FunctionType
    {
        Method,
        Function
    }
    #endregion
}
