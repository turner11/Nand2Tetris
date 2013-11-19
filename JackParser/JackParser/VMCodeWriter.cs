#if DEBUG
    //#define WRITE_COMMENTS
#endif

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace JackParser
{
    class VMCodeWriter
    {
        #region Data members

        public static event EventHandler<FunctionTypeEventArgs> onFuncTypeInfoRequired;

        static Stack<int> _IfOpeners = new Stack<int>();
        static Stack<int> _whileOpeners = new Stack<int>();

        static event EventHandler<VmCodeChangedArgs> onVmCodeChanged;
        static Action<GotLocalVariablesCount> onGotFunctionsLocalVariableNumber;

        static int _whileExpressionCount = 0;
        static int _ifExpressionCount = 0;
        

        static string _vmCode = String.Empty;
        public static string VmCode
        {
            get { return _vmCode; }
            set
            {
                
                string delta = value.Substring(_vmCode.Length, value.Length - _vmCode.Length).Trim();
                _vmCode = value ?? String.Empty;
                if (VMCodeWriter.onVmCodeChanged != null)
                {
                    VmCodeChangedArgs e = new VmCodeChangedArgs();
                    e.vm_code = _vmCode;
                    e.delta = delta;
                    VMCodeWriter.onVmCodeChanged(null, e);
                }
            }
        }

        static int _codeLines
        {
            get
            {
                int count = _vmCode.Split(new string[]{Environment.NewLine},StringSplitOptions.None).Length-1;
                return count;
            }
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

        static BiDictionary<string, int> _this;
        static BiDictionary<string, int> _that;

        static Dictionary<string, FunctionType> _functionTypeByName;

        static bool _isWritingConstructor;

        #endregion

        #region C'tors
        static VMCodeWriter()
        {
            VMCodeWriter._arguments = new BiDictionary<string, int>();
            VMCodeWriter._classVaraibles = new BiDictionary<string, int>();
            VMCodeWriter._functionTypeByName = new Dictionary<string, FunctionType>();
            VMCodeWriter._localVariables = new BiDictionary<string, int>();
            VMCodeWriter._staticVariables = new BiDictionary<string, int>();
            VMCodeWriter._that = new BiDictionary<string, int>();
            VMCodeWriter._that.Add("that", 0);
            VMCodeWriter._this = new BiDictionary<string, int>();
            VMCodeWriter._this.Add("this", 0);
            VMCodeWriter.VmCode = String.Empty;
            VMCodeWriter.ClassName = String.Empty;

            VMCodeWriter.onVmCodeChanged += VMCodeWriter_onVmCodeChanged;
        }

        static void VMCodeWriter_onVmCodeChanged(object sender, VmCodeChangedArgs e)
        {
            /*This is for easier debugging...*/
            //e.vm_code.ToString();
            //if (e.vm_code.Trim().Contains("call CircleMaker.Memory.deAlloc 1"))                
            //if (e.vm_code.Trim().EndsWith("push constant 0"))
            if (_codeLines == 400)            
            {
                /*this is a progrematic breakpoint zzz*/
                System.Diagnostics.Debugger.Break();
            }
        }
        #endregion

        #region Internal methods
        /// <summary>
        /// Adds the variablbe.
        /// </summary>
        /// <param name="variableModifier">The variable modifier.</param>
        /// <param name="varName">Name of the variable.</param>
        /// <exception cref="System.Exception">Got unknown modifier for variable: +variableModifier</exception>
        internal static void AddVariablbe(string variableModifier, string varName, bool isArgument)
        {
            BiDictionary<string, int> varDic;
            if (isArgument)
            {
                varDic = VMCodeWriter._arguments;
            }
            else
            {


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
            }
            int varIdx = varDic.Count;
            varDic.Add(varName, varIdx);

        }

        /// <summary>
        /// Adds a function declaration to the VM Code.
        /// </summary>
        /// <param name="fModifier">The f modifier.</param>
        /// <param name="name">The name.</param>
        /// <exception cref="System.Exception">Got unkown function modifier</exception>
        internal static void AddFunctionDeclaration(string fModifier, string name)
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
                VMCodeWriter.AddVariablbe(String.Empty, "dummy this", true);
            }
            else
            {
                throw new ArgumentException("Got unkown function modifier: " + fModifier);
            }
            /*Note local variables countwill be added with body*/
            if (!VMCodeWriter._functionTypeByName.ContainsKey(name))
            {
                VMCodeWriter._functionTypeByName.Add(name, fType);  
            }

            VMCodeWriter._localVariables.Clear();
                     
            VMCodeWriter._isWritingConstructor = isConstructor;

            Action<GotLocalVariablesCount> aWriteFunc = null;
            aWriteFunc = (GotLocalVariablesCount varsCountArgs) =>
            {
                WriteFunctionDeclaration(VMCodeWriter._className, name, varsCountArgs.variablesCount);
                
                if (VMCodeWriter._isWritingConstructor)
                {
                    VMCodeWriter.WriteClassMemoryAllocation();
                }

                if (isMethod)
                {
                    VMCodeWriter.WriteMethodInstancePushStetment();
                }
                onGotFunctionsLocalVariableNumber -= aWriteFunc;
            };

            onGotFunctionsLocalVariableNumber += aWriteFunc;

            
            

        }

        private static void WriteMethodInstancePushStetment()
        {
            VMCodeWriter.WritePushStetment(Segments.argument,0);
            VMCodeWriter.WritePopStatement(Segments.pointer,0);
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
            int intVar;
            bool boolVar;
            if (int.TryParse(varName, out intVar))
            {
                segment = Segments.constant;
                index = intVar;
            }
            else if(Boolean.TryParse(varName,out boolVar ))
            {
                segment = Segments.constant;
                index = 0;
            }
            else
            {
                BiDictionary<string, int> dic = VMCodeWriter.GetDictionaryByVariableName(varName);
                segment = VMCodeWriter.GetSegmentByDictionary(dic);

                if (!dic.TryGetByFirst(varName, out index))
                {
                    throw new Exception("failed to get variable's index");
                }
            }
            
        }



       

        /// <summary>
        /// Adds the while statement.
        /// </summary>
        /// <param name="statementNode">The node containg statement</param>
        /// <exception cref="System."></exception>
        internal static void AddWhileStatement(XmlNode statementNode)
        {
            XmlNode expression = VMCodeWriter.GetExpressionNodeFromStatement(statementNode);

            VMCodeWriter.VmCode += "label WHILE_EXP" + _whileExpressionCount + Environment.NewLine;
            VMCodeWriter.ExpressionNodeToVmCode(expression);
            VMCodeWriter.VmCode += "not" + Environment.NewLine;
            VMCodeWriter.VmCode += "if-goto WHILE_END" + (_whileExpressionCount) + Environment.NewLine;

            _whileOpeners.Push(_whileExpressionCount);
            _whileExpressionCount++;
            
        }

        internal static void CloseWhileStatement()
        {
            int whileCounter = _whileOpeners.Pop();
            VMCodeWriter.VmCode += "goto WHILE_EXP" + whileCounter+Environment.NewLine;
            VMCodeWriter.VmCode += "label WHILE_END" + whileCounter+ Environment.NewLine;
            
        }

        /// <summary>
        /// Adds the do statement.
        /// </summary>
        /// <param name="statementNode">The node containg statement.</param>
        internal static void AddDoStatement(XmlNode statementNode)
        {
            //<doStatement><keyword>do</keyword><subroutineCall><identifier>Memory</identifier><symbol>.</symbol><identifier>deAlloc</identifier><symbol>(</symbol><expressionList><expression><term><keywordConstant><keyword>this</keyword></keywordConstant></term></expression></expressionList><symbol>)</symbol></subroutineCall><symbol>;</symbol></doStatement>"

            string xPath = "doStatement/subroutineCall";
            XmlNode callNode = statementNode.SelectSingleNode(xPath);
            string nodeText = callNode.InnerText;
            string functionCallStr = nodeText.Substring(0, nodeText.IndexOf('(', 0));

             FunctionType fType = VMCodeWriter.GetFunctionTypeByName(functionCallStr);
             if (fType == FunctionType.Method)
             {
                 VMCodeWriter.WritePushStetment(Segments.pointer, 0);
             }

            string xmlExpressionList = "doStatement/subroutineCall/expressionList";
            XmlNode expressionListNode = statementNode.SelectSingleNode(xmlExpressionList);
            int expCall = 0;
            for (int i = 0; i < expressionListNode.ChildNodes.Count ; i++)
            {
                XmlNode currNode = expressionListNode.ChildNodes[i];
                if (String.Equals(currNode.Name, "expression", StringComparison.OrdinalIgnoreCase))
                {
                    expCall++;
                    VMCodeWriter.ExpressionNodeToVmCode(currNode);
                }
            }
            XmlNode expressionNode = VMCodeWriter.GetExpressionNodeFromStatement(statementNode);
            
            
           

            VMCodeWriter.WriteCallFunction(functionCallStr, expCall);

            /*TODO: temp!! this is for void*/
            WritePopStatement(Segments.temp, 0);
            
        }

        /// <summary>
        /// Adds the return statement.
        /// </summary>
        /// <param name="statementNode">The node containg statement.</param>
        internal static void AddReturnStatement(XmlNode statementNode)
        {
            XmlNode expressionNode = GetExpressionNodeFromStatementNode(statementNode);
            if (expressionNode != null)
            {
                VMCodeWriter.ExpressionNodeToVmCode(expressionNode);

            }
            else
            {
                WritePushStetment(Segments.constant, 0);
            }


            VMCodeWriter.VmCode += "return" + Environment.NewLine;
            VMCodeWriter.Arguments.Clear();
            VMCodeWriter._isWritingConstructor = false;
            
        }

        private static XmlNode GetExpressionNodeFromStatementNode(XmlNode statementNode)
        {
            string statementHeaderName = statementNode.FirstChild.Name;
            string xPath = statementHeaderName + "/expression";
            XmlNode expressionNode = statementNode.SelectSingleNode(xPath);
            return expressionNode;
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
            if (dic.Equals(VMCodeWriter._that))
            {
                return Segments.that;
            }
            if (dic.Equals(VMCodeWriter._this))
            {
                return Segments.pointer;
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
                VMCodeWriter._localVariables,//second
                VMCodeWriter._classVaraibles,                
                VMCodeWriter._staticVariables,
                VMCodeWriter._that,
                VMCodeWriter._this

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

            System.Diagnostics.Debug.WriteLine(String.Format(">>>> Pusing '{0}': (semgment {1}; Idx {2})",varName,segment,index));

            VMCodeWriter.WritePushStetment(segment, index);
            if (varName == "true")/*speacial case*/
            {
                VMCodeWriter.VmCode += "not" + Environment.NewLine;
            }
            
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
            System.Diagnostics.Debug.WriteLine(String.Format(">>>> Poping into '{0}': (semgment {1}; Idx {2})",varName,segment,index));
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
            argumets.Add(new StackArgumentObject(Segments.constant, VMCodeWriter._classVaraibles.Count));

            VMCodeWriter.WriteCallFunction("Memory", "alloc", argumets);
            VMCodeWriter.WritePopStatement(new StackArgumentObject(Segments.pointer, 0));
        }


        /// <summary>
        /// Writes the VM code that calls a function
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="argumets">The argumets count passed to function</param>
        private static void WriteCallFunction( string functionName, int argumentCount)
        {
            string className;
            string fName;
            SplittoClassAndFunctionNames(functionName, out className, out fName);

            VMCodeWriter.WriteCallFunction(className, fName, argumentCount);
        }

        private static void SplittoClassAndFunctionNames(string functionName, out string className, out string fName)
        {

            string[] fCall = functionName.Split(new char[] { '.' }, 2, StringSplitOptions.None);
            if (fCall.Length == 1)
            {
                string funcName = fCall[0];
                fCall = new string[2] { VMCodeWriter._className, funcName };
            }

            className = fCall[0];
            fName = fCall[1];
        }
        /// <summary>
        /// Writes the VM code that calls a function
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="argumets">The argumets count passed to function</param>
        private static void WriteCallFunction(string className, string functionName, int argumentCount)
        {
            FunctionType ftype = VMCodeWriter.GetFunctionTypeByName(functionName);
            argumentCount += ftype == FunctionType.Method ? 1 : 0;
            VMCodeWriter.VmCode += String.Format("call {0}.{1} {2}", className, functionName, argumentCount) + Environment.NewLine;
        }

        /// <summary>
        /// Writes the VM code that calls a function, in  context of current class
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="argumets">The argumets to put in stack for function.</param>
        private static void WriteCallFunction(string functionName, List<StackArgumentObject> argumets)
        {
            string className;
            string fName;
            SplittoClassAndFunctionNames(functionName, out className, out fName);
            VMCodeWriter.WriteCallFunction(className, fName, argumets);
        }

        /// <summary>
        /// Writes the VM code that calls a function
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="argumets">The argumets to put in stack for function.</param>
        private static void WriteCallFunction(string className, string functionName, List<StackArgumentObject> argumets)
        {
            foreach (StackArgumentObject arg in argumets)
            {
                VMCodeWriter.WritePushStetment(arg);
            }
            VMCodeWriter.WriteCallFunction(className,functionName, argumets.Count);
        }
        /// <summary>
        /// Adds a function declaration to the VM Code.
        /// </summary>
        /// <param name="fType">Type of the function.</param>
        /// <param name="name">The name of function.</param>

        private static void WriteFunctionDeclaration(string className, string name, int varsCount)
        {
            VMCodeWriter._ifExpressionCount = 0;
            VMCodeWriter._whileExpressionCount = 0;
            VMCodeWriter.VmCode += String.Format("function {0}.{1} {2}", VMCodeWriter.ClassName, name, varsCount) + Environment.NewLine;
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
        /// <param name="expressionClone">The expression node.</param>
        /// <returns>the expression VM code</returns>
        private static void ExpressionNodeToVmCode(XmlNode expressionNode)
        {
            XmlNode expressionClone = expressionNode.CloneNode(true);
            if (expressionClone == null)
            {
                return;
            }
            string content = expressionClone.InnerText;
            XmlNodeList nodes = expressionClone.ChildNodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode currNode = nodes[i];
                string currNodeName = currNode.Name ;
                
                if (currNodeName ==  "term")
                {
                    VMCodeWriter.TermNodeToVmCode(currNode);
                }
                else if (currNodeName == "op")
                {
                    string @operator  = currNode.InnerText;
                    XmlNode nextTermNode = nodes[++i];//handle it first, and advance the index
                    VMCodeWriter.TermNodeToVmCode(nextTermNode);
                    VMCodeWriter.WriteBinaryOperation(@operator);
                }
                else
                {
                    1.ToString();
                }
            }


        }

        private static void TermNodeToVmCode(XmlNode termNode)
        {
            string currNodeName = termNode.Name;
            if (currNodeName != "term")
            {
                throw new Exception("expected node to be of term type");
            }
            XmlNodeList currNodes = termNode.ChildNodes;
            for (int j = 0; j < currNodes.Count; j++)
            {
                XmlNode currTermChild = currNodes[j];
                string currContent = currTermChild.InnerText;
                switch (currTermChild.Name)
                {
                    case "integerConstant":

                        int value;
                        if (!int.TryParse(currContent, out value))
                        {
                            throw new Exception("failed to parse integer constant expression: " + currContent);
                        }
                        VMCodeWriter.WritePushStetment(Segments.constant, value);
                        break;
                    case "keywordConstant":
                    case "identifier":

                        VMCodeWriter.WritePushStetment(currContent);
                        break;
                    case "exp op exp":
                        throw new Exception();
                        break;
                    case "unaryOp":
                         XmlNode nextNode = currNodes[++j];//handle it first, and advance the index
                         VMCodeWriter.TermNodeToVmCode(nextNode);
                        VMCodeWriter.WriteUnaryOperation(currContent);
                        break;
                    case "subroutineCall":
                        VMCodeWriter.WriteCallFunction(currTermChild);
                        break;
                    case "term":
                        VMCodeWriter.HandleTermExpression(currTermChild);
                        break;
                    case "symbol":
                    //nothing to do
                        break;
                    case "expression":
                        VMCodeWriter.ExpressionNodeToVmCode(currTermChild);
                        break;
                    case "stringConstant":
                        VMCodeWriter.WriteStringConstant(currContent);
                        break;
                    default:
                        throw new Exception("Failed to determin expression type: " + currTermChild.Name);
                }
            }
        }

        private static void HandleTermExpression(XmlNode currTermChild)
        {
            XmlNode expression = currTermChild.SelectSingleNode("expression");
            if (expression == null)
            {
                throw new Exception("failed to get expression from sub term");
            }
            else
            {
                VMCodeWriter.ExpressionNodeToVmCode(expression);
            }
        }

        private static void WriteCallFunction(XmlNode subroutineNode)
        {
            string xPath = "expressionList";
            XmlNode expressionList = subroutineNode.SelectSingleNode(xPath);

            string text = subroutineNode.InnerText;
            int openBrackIdx = text.IndexOf("(");
            string fName = text.Substring(0, openBrackIdx);

            FunctionType fType = GetFunctionTypeByName(fName);
            if (fType == FunctionType.Method)
            {
                VMCodeWriter.WritePushStetment(Segments.pointer,0);
            }

            int argCount = 0;
            if (expressionList != null)
            {
                for (int i = 0; i < expressionList.ChildNodes.Count; i++)
                {
                    XmlNode expressionNode = expressionList.ChildNodes[i];
                    if (expressionNode.Name == "expression")
                    {
                        argCount++;
                        ExpressionNodeToVmCode(expressionNode);
                    }
                }
            }

            VMCodeWriter.WriteCallFunction(fName, argCount);
        }

      

        private static void WriteStringConstant(string str)
        {
            WritePushStetment(Segments.constant, str.Length);
            WriteCallFunction("String.new", 1);
            for (int i = 0; i < str.Length; i++)
            {
                int index = (int)str[i];
                WritePushStetment(Segments.constant, index);
                WriteCallFunction("String.appendChar", 2);
                
            }
        }

        private static FunctionType GetFunctionTypeByName(string fName)
        {
            FunctionType fType = FunctionType.Function;
            if(VMCodeWriter._functionTypeByName.ContainsKey(fName) )
            {
                fType= VMCodeWriter._functionTypeByName[fName];
            }
            else
            {
                FunctionTypeEventArgs e= new FunctionTypeEventArgs(fName);
                onFuncTypeInfoRequired(null,e);
                fType = e.functionType;
                VMCodeWriter._functionTypeByName[fName] = fType;
            }
            return fType;
        }

        private static void GetInternalFunctionIndexes(string str,int startIndex,  out int openBrackIdx, out int closeBrackIdx)
        {
            
            openBrackIdx = str.IndexOf("(", startIndex );
            closeBrackIdx =-1;
            int counter = 1;
            int idx = openBrackIdx + 1;
            while (counter != 0 && idx < str.Length)
            {
                if (str[idx] == '(')
                {
                    counter++;
                }
                if (str[idx] == ')')
                {
                    counter--;
                }
                idx++;
            }
            closeBrackIdx = idx;
            int length = closeBrackIdx-openBrackIdx-2;
            string s = str.Substring(openBrackIdx+1,length);
            
            //char[] separators = new char[] { '(', ')', '=', ',' };
            //string[] arr = str.Split(separators, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Trim().Length != 0).Select(x => x.Trim()).ToArray();
            

           // Console.WriteLine("Required index is {0}", openBrackIdx);
        }

        private static void WriteComment(string currExpression)
        {
#if WRITE_COMMENTS
            VMCodeWriter._vmCode += "# " + currExpression + Environment.NewLine;
#endif
        }

        private static List<string> GetSubExpressions(string currExpression)
        {
            List<string> splittedExpressions = null;
            //string firstOperand = currExpression.Substring;

            List<string> splitters = new List<string>(SymbolClassifications._oparations);
            splitters.Add(",");
            var emuStrings =
            currExpression.Select<char, string>(c => splitters.Contains(c.ToString()) ? "@" + c + "@" : c.ToString());
            string revisedExpression = String.Join(String.Empty, emuStrings);
            splittedExpressions = revisedExpression.Split(new string[] { "@" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            splittedExpressions = splittedExpressions.Select(s => s.Replace("@", String.Empty)).ToList();


            splittedExpressions.RemoveAll(s => s == ",");
            //keep brackets in tackt
            for (int i = 0; i < splittedExpressions.Count - 1; i++)
            {
                int bCount = 0;
                if (splittedExpressions[i].Contains("("))
                {
                    bCount++;
                    bool closed = IsLegalBracketExpression(splittedExpressions[i]) ;
                    while (!closed && i < splittedExpressions.Count-1)
                    {
                        if (splittedExpressions[i + 1].Contains("("))
                            bCount++;
                        if (splittedExpressions[i + 1].Contains(")"))
                            bCount--;

                        splittedExpressions[i] += splittedExpressions[i + 1];
                        splittedExpressions.RemoveAt(i + 1);

                        if (bCount == 0)
                        {
                            closed = true;
                        }
                    }
                }
            }

            string firstExp = splittedExpressions[0];
            int expCount = SymbolClassifications._allOparations.Contains(firstExp) ? 0 : 1;


            bool haveFullExpression = expCount == 2 || splittedExpressions.Count == 3 || splittedExpressions.Count == 1;
                while (!haveFullExpression)
                {
                    string currToken = splittedExpressions[1];

                    splittedExpressions[0] += currToken;
                    splittedExpressions.RemoveAt(1);

                    bool isOperation = SymbolClassifications._allOparations.Contains(currToken);
                    if (!isOperation)
                    {
                        expCount++;
                    }
                    haveFullExpression = expCount == 2 || splittedExpressions.Count == 1;
                }
            

            //collapse to 3 cells
                while(splittedExpressions.Count >3)
                {
                    splittedExpressions[2] += splittedExpressions[3];
                    splittedExpressions.RemoveAt(3);
                }


            return splittedExpressions;
        }

        private static bool IsLegalBracketExpression(string str)
        {
            Stack<char> stack = new Stack<char>();
            foreach (char c in str)
            {
                try
                {


                    if (c == '(')
                    {
                        stack.Push(c);
                    }
                    else if (c == ')')
                    {
                        stack.Pop();

                    }
                }
                catch (Exception)
                {

                    return false;
                }
              
            }
            // check that every opening bracket was closed
            return stack.Count == 0;
        }





        private static void WriteBinaryOperation(string @operator)
        {
            
            /*speacial cases*/
            if (@operator == "*")
            {
                VMCodeWriter.WriteCallFunction("Math","multiply", 2);

            }
            else if (@operator == "/")
            {
                VMCodeWriter.WriteCallFunction("Math", "divide", 2);
            }
            else if (@operator == "#")
            {
                VMCodeWriter.WriteCallFunction("Math", "sqrt", 2);
            }
            else //Actually an OS operator
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
                VMCodeWriter.VmCode += operation + Environment.NewLine;

                //if (!String.IsNullOrWhiteSpace(destinationVarName))
                //{
                //    VMCodeWriter.WritePopStatement(destinationVarName);
                //}
            }




        }

        private static void WriteUnaryOperation(string @operator)
        {
            Dictionary<string, string> operationsNames = new Dictionary<string, string>()
            {
                {"~","not"},
                {"-","neg"},

            };

            string operation = operationsNames[@operator];
            VMCodeWriter.VmCode += operation + Environment.NewLine;
            
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



        /// <summary>
        /// Writes the function local variables count.
        /// </summary>
        /// <param name="loaclVarsCount">The loacl vars count.</param>
        internal static void WriteFunctionLocalVariablesCount(int loaclVarsCount)
        {
            if (onGotFunctionsLocalVariableNumber != null)
            {
                GotLocalVariablesCount e = new GotLocalVariablesCount(loaclVarsCount);
                onGotFunctionsLocalVariableNumber(e);
            }
            
        }

        internal static void AddArgument(string varname)
        {
            VMCodeWriter._arguments.Add(varname, VMCodeWriter._arguments.Count);
        }

        /// <summary>
        /// Adds if statement.
        /// </summary>
        /// <param name="statementNode">The node containg statement</param>        
        internal static void AddIfStatement(XmlNode statementNode)
        {
            XmlNode expression = VMCodeWriter.GetExpressionNodeFromStatement(statementNode);
            VMCodeWriter.ExpressionNodeToVmCode(expression);
            //VMCodeWriter.VmCode += "eq" + Environment.NewLine;

            VMCodeWriter.VmCode += "if-goto IF_TRUE" + _ifExpressionCount + Environment.NewLine;
            VMCodeWriter.VmCode += "goto IF_FALSE" + _ifExpressionCount + Environment.NewLine;
            VMCodeWriter.VmCode += "label IF_TRUE" + _ifExpressionCount + Environment.NewLine;

            //VMCodeWriter.VmCode += "if-goto IF_TRUE" + _ifExpressionCount + Environment.NewLine;
            _IfOpeners.Push(_ifExpressionCount);
            _ifExpressionCount++;
            //throw new NotImplementedException();
        }

        internal static void AddElseStatement()
        {
            int ifCounter = _IfOpeners.Peek();
            VMCodeWriter.VmCode += "goto IF_END" + ifCounter + Environment.NewLine;
            VMCodeWriter.VmCode += "label IF_FALSE" + ifCounter + Environment.NewLine;

        }

        internal static void CloseIfStatement(bool hasElse)
        {
            int ifCounter = _IfOpeners.Pop(); 
            if (hasElse)
            {
                   
                VMCodeWriter.VmCode += "label IF_END" + ifCounter + Environment.NewLine;

            }
            else
            {
                VMCodeWriter.VmCode += "label IF_FALSE" + ifCounter + Environment.NewLine;
            }


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
        temp,
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
        [Description("method")]        
        Method,
        [Description("function")]
        Function
    }

    class VmCodeChangedArgs : EventArgs
    {
        public string vm_code;
        public string delta;
    }

    public class FunctionTypeEventArgs : EventArgs
    {
        public string functionName;
        public FunctionType functionType;
        public FunctionTypeEventArgs(string name)
        {
            this.functionName = name;
        }
    }

    class GotLocalVariablesCount : EventArgs
    {
        public int variablesCount;


        public GotLocalVariablesCount(int varsCount)
        {
            this.variablesCount = varsCount;
        }
        
    }

    #endregion
}
