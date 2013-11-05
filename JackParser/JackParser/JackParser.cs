using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace JackParser
{
    public static class JackParser
    {
        #region symbol classifications
        static ReadOnlyCollection<string> _allKeyWords;
        static ReadOnlyCollection<string> _generalKeyWords = new ReadOnlyCollection<string>(
            new List<String> { "class", "var", "void", "true", "false", "null", "this", "else" });

        static ReadOnlyCollection<string> _statementsHeaders = new ReadOnlyCollection<string>(
           new List<String> { "let", "do", "if", "while", "return" });

        static ReadOnlyCollection<string> _variablesModifiers = new ReadOnlyCollection<string>(
            new List<String> { "static", "field" });

        static ReadOnlyCollection<string> _variablesTypes = new ReadOnlyCollection<string>(
            new List<String> { "int", "char", "boolean" });

        static ReadOnlyCollection<string> _subRoutineReturnType;

        static ReadOnlyCollection<string> _subRoutineModifiers = new ReadOnlyCollection<string>(
            new List<String> { "constructor", "function", "method" });

        static ReadOnlyCollection<string> _symbols;


        static ReadOnlyCollection<string> _oparations =
            new ReadOnlyCollection<string>(new List<String> { "+", "-", "*", "/", "&", "|", "<", ">", "=" });

        static ReadOnlyCollection<string> _unariOparations =
            new ReadOnlyCollection<string>(new List<String> { "~", "-", "#" });
        #endregion

        #region C'tor
        static JackParser()
        {
            var srRetTypes = new List<string>(JackParser._variablesTypes);
            srRetTypes.Add("void");

            List<string> allKeyWords = new List<string>();

            /*symbols*/
            List<string> allSymbols = new List<string>() { "[", "]", "{", "}", "(", ")", ".", ",", ";" };
            allSymbols.AddRange(JackParser._oparations);
            allSymbols.AddRange(JackParser._unariOparations);
            JackParser._symbols = new ReadOnlyCollection<string>(allSymbols);

            /*all keywords*/
            JackParser._subRoutineReturnType = new ReadOnlyCollection<string>(srRetTypes);
            allKeyWords.AddRange(JackParser._generalKeyWords);
            allKeyWords.AddRange(JackParser._variablesModifiers);
            allKeyWords.AddRange(JackParser._subRoutineReturnType);
            allKeyWords.AddRange(JackParser._statementsHeaders);
            allKeyWords.AddRange(JackParser._oparations);
            allKeyWords.AddRange(JackParser._unariOparations);
            allKeyWords.AddRange(JackParser._symbols);
            allKeyWords.AddRange(JackParser._variablesTypes.Distinct());


            allKeyWords.AddRange(JackParser._subRoutineModifiers);
            JackParser._allKeyWords = new ReadOnlyCollection<string>(allKeyWords.Distinct().ToList());
        }
        #endregion

        #region Internal Methods
        /// <summary>
        /// Extension to XmlDocument. returns the document as a string
        /// </summary>
        /// <param name="xmlDoc">The XML doc.</param>
        /// <returns></returns>
        internal static string ToXmlString(this XmlDocument xmlDoc)
        {

            //converting xml to string
            using (var stringWriter = new StringWriter())
            using (var xmlTextWriter = XmlWriter.Create(stringWriter))
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.NewLineChars = Environment.NewLine;
                settings.NewLineHandling = NewLineHandling.Replace;
                settings.Indent = true;
                settings.IndentChars = "\t";
                settings.NamespaceHandling = NamespaceHandling.OmitDuplicates;
                settings.OutputMethod.ToString();// = XmlOutputMethod.Xml;
                xmlDoc.WriteTo(xmlTextWriter);
                xmlTextWriter.Flush();
                return stringWriter.GetStringBuilder().ToString();
            }

        }
        /// <summary>
        /// Gets the jack <see cref="System.Xml.XmlDocument"/> from tokens.
        /// </summary>
        /// <param name="tokensDoc">The tokens to extract XML document from.</param>
        /// <returns>the XML document constracted from token</returns>
        internal static XmlDocument GetJackXmlFromTokens(XmlDocument tokensDoc)
        {
            XmlDocument xml;
            XmlDocument tokenClone = (XmlDocument)tokensDoc.Clone();
            JackParser.GetClass(tokenClone, out xml);

            //XmlNodeList tokens = tokensDoc.FirstChild.ChildNodes;
            //for (int i = 0; i < tokens.Count; i++)
            //{
            //    XmlNode currNode = tokens[i];
            //    string nodeType = currNode.Name;
            //    string nodeValue = currNode.InnerText.Trim() ;

            //}

            return xml;

        }

        /// <summary>
        /// Gets the jack XML string from tokens.
        /// </summary>
        /// <param name="tokens">The tokens to extract XML from.</param>
        /// <returns>the content string of Jack XML file</returns>
        internal static string GetJackXmlStringFromTokens(XmlDocument tokens)
        {
            XmlDocument xmlOutput = JackParser.GetJackXmlFromTokens(tokens);
            return (xmlOutput ?? new XmlDocument()).ToXmlString();
        }
        #endregion

        /// <summary>
        /// Adds the root class (and content) to the <see cref="doc"/> argument.
        /// </summary>
        /// <param name="tokensDoc">The tokens document.</param>
        /// <param name="xml">The XML for adding class element to.</param>
        private static void GetClass(XmlDocument tokensDoc, out XmlDocument xml)
        {
            xml = new XmlDocument();
            //create class root element
            XmlNode classRoot = xml.CreateNode(XmlNodeType.Element, OutputStructureNodes.@class.ToString(), String.Empty);
            //adding the class root node
            xml.AppendChild(classRoot);

            /*Class token*/
            XmlNode tClass = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(classRoot, tClass, OutputStructureNodes.@class, TokenTypes.keyword, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            /*Class Name*/
            XmlNode tClassName = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(classRoot, tClassName, TokenTypes.identifier, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            /*{*/
            XmlNode tOpenBracket = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(classRoot, tOpenBracket, "{", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            /*Variable declarations*/
            JackParser.AppendVariableDeclarations(classRoot, tokensDoc);

            /*Sub routine declarations*/
            JackParser.AppendSubRoutinesDeclarations(classRoot, tokensDoc);

            /*}*/
            XmlNode tEndBracket = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(classRoot, tEndBracket, "}", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
            return;


        }

        /// <summary>
        /// Appends the variable declarations nodes to the specified class root node.
        /// </summary>
        /// <param name="classRoot">The class root node.</param>
        /// <param name="tokensDoc">The tokens doc to take nodes from.</param>
        private static void AppendVariableDeclarations(XmlNode classRoot, XmlDocument tokensDoc)
        {

            while (JackParser.IsVarDecBegining(tokensDoc))
            {
                JackParser.AppendVariableDeclaration(classRoot, tokensDoc);
            }
        }

        /// <summary>
        /// Appends a single variable declaration node with all of it's sub nodes to the specified class root node.
        /// <remarks>
        /// Note that tokens that were added to class root will be removed from tokens doc
        /// </remarks>
        /// </summary>
        /// <param name="classRoot">The class root node.</param>
        /// <param name="tokensDoc">The tokens doc to take nodes from.</param>
        private static void AppendVariableDeclaration(XmlNode classRoot, XmlDocument tokensDoc)
        {
            XmlDocument xml = classRoot.OwnerDocument;
            //create classVarDec root element
            XmlNode rootVarDec = xml.CreateNode(XmlNodeType.Element, OutputStructureNodes.classVarDec.ToStringByDescription(), String.Empty);
            //adding the variable declaration node
            classRoot.AppendChild(rootVarDec);

            /*field | static------------------------------------------------------------------------*/
            XmlNode tKeyWordModifier = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootVarDec, tKeyWordModifier, JackParser._variablesModifiers, TokenTypes.keyword, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            /*var type------------------------------------------------------------------------*/
            XmlNode varTypeToken = JackParser.GetNextToken(tokensDoc);

            List<string> possibleTexts = JackParser.GetValidTypeName(varTypeToken.InnerText);

            JackParser.AddToken(rootVarDec, varTypeToken, possibleTexts, TokenTypes.keyword, OutputStructureNodes.type.ToStringByDescription());
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            /*var name------------------------------------------------------------------------*/
            bool hasAnotherVar = false; // until proven otherwise, we assume we are nor facing "field int x, y;"
            do
            {
                hasAnotherVar = false;
                XmlNode varNameToken = JackParser.GetNextToken(tokensDoc);
                bool isvalidName = JackParser.IsVariableName(varNameToken.InnerText);
                /*Not valid name; quit*/
                if (!isvalidName) { throw new Exception(String.Format("variable name {0} is not valid", varNameToken.InnerText)); }

                JackParser.AddToken(rootVarDec, varNameToken, String.Empty, TokenTypes.identifier, null);
                //token was handled, remove it
                JackParser.RemoveFirstToken(tokensDoc);
                if (JackParser.GetFirstTokenText(tokensDoc) == ",")
                {
                    hasAnotherVar = true;

                    XmlNode commaToken = JackParser.GetNextToken(tokensDoc);
                    JackParser.AddToken(rootVarDec, commaToken, ",", TokenTypes.symbol, null);
                    //token was handled, remove it
                    JackParser.RemoveFirstToken(tokensDoc);
                }
            } while (hasAnotherVar);

            /*;------------------------------------------------------------------------*/
            XmlNode semiCommaToken = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootVarDec, semiCommaToken, ";", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
        }
        /// <summary>
        /// Appends a single variable subroutine node with all of it's sub nodes to the specified class root node.
        /// <remarks>
        /// Note that tokens that were added to class root will be removed from tokens doc
        /// </remarks>
        /// </summary>
        /// <param name="classRoot">The class root node.</param>
        /// <param name="tokensDoc">The tokens doc to take nodes from.</param>
        private static void AppendSubroutine(XmlNode classRoot, XmlDocument tokensDoc)
        {
            XmlDocument xml = classRoot.OwnerDocument;
            //create classVarDec root element
            XmlNode rootSubDec = xml.CreateNode(XmlNodeType.Element, OutputStructureNodes.subroutineDec.ToStringByDescription(), String.Empty);
            //adding the variable declaration node
            classRoot.AppendChild(rootSubDec);

            JackParser.AppendFunctionDecHeader(tokensDoc, rootSubDec);

            JackParser.AppendFunctionDecBody(tokensDoc, rootSubDec);
        }

        /// <summary>
        /// Appends the function decleration body.
        /// </summary>
        /// <param name="tokensDoc">The tokens doc.</param>
        /// <param name="rootSubDec">The root sub dec.</param>
        private static void AppendFunctionDecBody(XmlDocument tokensDoc, XmlNode rootSubDec)
        {
            /*{------------------------------------------------------------------------*/
            XmlNode openBodyBracket = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootSubDec, openBodyBracket, "{", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            JackParser.GetParameterList(rootSubDec, tokensDoc);

            JackParser.AppendStatements(rootSubDec, tokensDoc);

            /*}------------------------------------------------------------------------*/
            XmlNode closeBodyBracket = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootSubDec, closeBodyBracket, "}", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
        }

        /// <summary>
        /// Appends the function decleration header header.
        /// </summary>
        /// <param name="tokensDoc">The tokens doc.</param>
        /// <param name="rootSubDec">The root sub dec.</param>
        /// <exception cref="System.Exception"></exception>
        private static void AppendFunctionDecHeader(XmlDocument tokensDoc, XmlNode rootSubDec)
        {
            /*constructor | function | method------------------------------------------------------------------------*/
            XmlNode tFuncType = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootSubDec, tFuncType, JackParser._subRoutineModifiers, TokenTypes.keyword, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            /*function return type------------------------------------------------------------------------*/
            XmlNode retTypeToken = JackParser.GetNextToken(tokensDoc);

            List<string> possibleTexts = JackParser.GetValidTypeName(retTypeToken.InnerText);

            JackParser.AddToken(rootSubDec, retTypeToken, possibleTexts, TokenTypes.identifier, OutputStructureNodes.type.ToStringByDescription());
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
            /*function name------------------------------------------------------------------------*/

            XmlNode varfuncNameToken = JackParser.GetNextToken(tokensDoc);
            bool isvalidName = JackParser.IsSubRoutineName(varfuncNameToken.InnerText);
            /*Not valid name; quit*/
            if (!isvalidName) { throw new Exception(String.Format("sub-routine name {0} is not valid", varfuncNameToken.InnerText)); }

            JackParser.AddToken(rootSubDec, varfuncNameToken, String.Empty, TokenTypes.identifier, OutputStructureNodes.subroutineName.ToStringByDescription());
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
            /*(------------------------------------------------------------------------*/
            XmlNode openBracket = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootSubDec, openBracket, "(", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            /*Parameter List------------------------------------------------------------------------*/
            JackParser.GetParameterList(rootSubDec, tokensDoc);

            /*)------------------------------------------------------------------------*/
            XmlNode closeBracket = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootSubDec, closeBracket, ")", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
        }

        /// <summary>
        /// Appends the expression in brackets: '(' Expression ')'
        /// </summary>
        /// <param name="tokensDoc">The tokens doc.</param>
        /// <param name="rootIfNode">The root if node.</param>
        private static void AppendExpressionInBrackets(XmlDocument tokensDoc, XmlNode rootIfNode)
        {
            /*(*/
            XmlNode bracketStartToken = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootIfNode, bracketStartToken, "(", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            JackParser.AppendExpression(rootIfNode, tokensDoc);


            /*)*/
            XmlNode bracketEndToken = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootIfNode, bracketEndToken, ")", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
        }

        /// <summary>
        /// Appends the expression that is composed from nodes at top of tokens Doc to theparent node. Nodes will be removed from tokenns doc.
        /// </summary>
        /// <param name="parentNode">The parent node.</param>
        /// <param name="tokensDoc">The tokens doc.</param>
        private static void AppendExpression(XmlNode parentNode, XmlDocument tokensDoc)
        {

            JackParser.AppendTerm(parentNode, tokensDoc);
            JackParser.AppendOpTerms(parentNode, tokensDoc);
        }
        /// <summary>
        /// Appends the expression list that is composed from nodes at top of tokens Doc to the parent node. Nodes will be removed from tokenns doc.
        /// </summary>
        /// <param name="parentNode">The parent node.</param>
        /// <param name="tokensDoc">The tokens doc.</param>
        private static void AppendExpressionList(XmlNode parentNode, XmlDocument tokensDoc)
        {


            /*Expressions List (could be 0 or more)------------------------------------------------------------------------*/
            //JackParser.GetParameterList(parentNode, tokensDoc);
            bool isFirst = true;
            while (JackParser.IsNextTokenTerm(tokensDoc))
            {
                if (!isFirst)
                {
                    XmlNode commaToken = JackParser.GetNextToken(tokensDoc);
                    JackParser.AddToken(parentNode, commaToken, ",", TokenTypes.symbol, null);
                    //token was handled, remove it
                    JackParser.RemoveFirstToken(tokensDoc);
                }
                JackParser.AppendExpression(parentNode, tokensDoc);


                isFirst = false;
            }


        }



        /// <summary>
        /// Appends 0 or more sequences of opperations-terms (op-term)*.
        /// </summary>
        /// <param name="parentNode">The parent node.</param>
        /// <param name="tokensDoc">The tokens doc.</param>
        private static void AppendOpTerms(XmlNode parentNode, XmlDocument tokensDoc)
        {
            while (JackParser.IsNextNodeBinaryOp(tokensDoc))
            {
                /*OP*/
                XmlNode opToken = JackParser.GetNextToken(tokensDoc);
                JackParser.AddToken(parentNode, opToken, JackParser._oparations, TokenTypes.operation, null);
                //token was handled, remove it
                JackParser.RemoveFirstToken(tokensDoc);
                /*term*/
                JackParser.AppendTerm(parentNode, tokensDoc);
            }
        }
        /// <summary>
        /// Appends the term to parent nod.
        /// </summary>
        /// <param name="parentNode">The parent node.</param>
        /// <param name="tokensDoc">The tokens doc.</param>
        private static void AppendTerm(XmlNode parentNode, XmlDocument tokensDoc)
        {
            //TODO: to be implemented: integerConstant stringConstant keywordConstant varName
            throw new NotImplementedException();
            TermTypes ttype = JackParser.GetNextTokenTermType(tokensDoc);
        }

        /// <summary>
        /// Gets the parameter list and adds it to <see cref="parentNode"/> .
        /// <remarks>
        /// Nodes that were added to parentNode will be removed from tokensDoc
        /// </remarks>
        /// </summary>
        /// <param name="rootSubDec">The root sub dec.</param>
        /// <param name="tokensDoc">The tokens doc.</param>
        private static void GetParameterList(XmlNode parentNode, XmlDocument tokensDoc)
        {
            //create classVarDec root element
            XmlNode rootPramList = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.parameterList.ToStringByDescription(), String.Empty);
            //adding the variable declaration node
            parentNode.AppendChild(rootPramList);


            //check if there are any parameters:
            XmlNode nFirst = JackParser.GetNextToken(tokensDoc);
            XmlNode nSecond = nFirst.NextSibling;

            List<string> validTexts = JackParser.GetValidTypeName(nFirst.InnerText);
            //is it a type?
            bool hasParams = JackParser.ValidateToken(nFirst, validTexts, TokenTypes.keyword, false);
            //is it a name
            hasParams &= nSecond != null && JackParser.IsVariableName(nSecond.InnerText);
            if (!hasParams)
            {
                return;
            }



            try
            {

                bool isMiltipleVars = false; // until proven otherwise, we assume we are nor facing "field int x, y;"
                do
                {
                    isMiltipleVars = false;  //resetting
                    /*var type*/
                    XmlNode varTypeToken = JackParser.GetNextToken(tokensDoc);
                    List<string> possibleTexts = JackParser.GetValidTypeName(varTypeToken.InnerText);
                    JackParser.AddToken(rootPramList, varTypeToken, possibleTexts, TokenTypes.keyword, OutputStructureNodes.type.ToStringByDescription());
                    //token was handled, remove it
                    JackParser.RemoveFirstToken(tokensDoc);

                    /*var name*/
                    XmlNode varNameToken = JackParser.GetNextToken(tokensDoc);
                    bool isvalidName = JackParser.IsVariableName(varNameToken.InnerText);
                    /*Not valid name; quit*/
                    if (!isvalidName) { throw new Exception(String.Format("variable name {0} is not valid", varNameToken.InnerText)); }

                    JackParser.AddToken(rootPramList, varNameToken, String.Empty, TokenTypes.identifier, null);
                    //token was handled, remove it
                    JackParser.RemoveFirstToken(tokensDoc);
                    if (JackParser.GetFirstTokenText(tokensDoc) == ",")
                    {
                        isMiltipleVars = true;

                        XmlNode commaToken = JackParser.GetNextToken(tokensDoc);
                        JackParser.AddToken(rootPramList, commaToken, ",", TokenTypes.symbol, null);
                        //token was handled, remove it
                        JackParser.RemoveFirstToken(tokensDoc);
                    }
                } while (isMiltipleVars);


            }
            catch (Exception ex)
            {
                //TODO: This is not working with REF, it is affecting parent XML doc.
                //Find another way to rollback...
                //Idea: just add function: isParameterlist...
                ex.ToString();

            }
        }

        /// <summary>
        /// Gets the list of valid type names.
        /// </summary>
        /// <param name="classNameCandidate">The class name candidate.</param>
        /// <returns>list of all static typs a node might have + <see cref="classNameCandidate"/> if it is a valid className</returns>
        private static List<string> GetValidTypeName(string classNameCandidate)
        {
            bool isClassName = JackParser.IsClassName(classNameCandidate);
            List<string> possibleTexts = new List<string>(JackParser._variablesTypes);
            if (isClassName) { possibleTexts.Add(classNameCandidate); }
            return possibleTexts;
        }
        /// <summary>
        /// Appends the sub routines declarations nodes to the specified class root node.
        /// </summary>
        /// <param name="classRoot">The class root node.</param>
        /// <param name="tokensDoc">The tokens doc to take nodes from.</param>
        private static void AppendSubRoutinesDeclarations(XmlNode classRoot, XmlDocument tokensDoc)
        {
            while (JackParser.IsSubroutineBegining(tokensDoc))
            {
                JackParser.AppendSubroutine(classRoot, tokensDoc);
            }
        }


        /// <summary>
        /// Appends the subroutine call.
        /// </summary>
        /// <param name="rootdoNode">The rootdo node.</param>
        /// <param name="tokensDoc">The tokens doc.</param>
        private static void AppendSubroutineCall(XmlNode rootdoNode, XmlDocument tokensDoc)
        {
            OutputStructureNodes sRoutineOwner;
            bool isSubroutineCall = JackParser.IsSubroutineCallBegining(tokensDoc, out sRoutineOwner);
            if (sRoutineOwner == OutputStructureNodes.className)
            {
                JackParser.AppendSubroutineAndExpressions(rootdoNode, tokensDoc);
            }
            else if (sRoutineOwner == OutputStructureNodes.subroutineName)
            {

                JackParser.AppendSubroutineAndExpressions(rootdoNode, tokensDoc);
            }
            else
            {
                throw new Exception("Unknown format for subroutin");
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Appends the subroutine and expressions=> subroutineName'('expressionList')' 
        /// </summary>
        /// <param name="subroutineCallRootNode">The rootdo node.</param>
        /// <param name="tokensDoc">The tokens doc.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private static void AppendSubroutineAndExpressions(XmlNode subroutineCallRootNode, XmlDocument tokensDoc)
        {
            XmlNode dotToken = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(subroutineCallRootNode, dotToken, ".", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);


            /*Validating subroutine name*/
            string subRoutineName = JackParser.GetFirstTokenText(tokensDoc);
            bool isSubroutineName = JackParser.IsSubRoutineName(subRoutineName);
            if (!isSubroutineName) { throw new Exception(String.Format("{0} is not a valid subroutine name", subRoutineName)); }
            /*subroutine name ------------------------------------------------------------------------*/
            XmlNode subRoutinNameToken = JackParser.GetNextToken(tokensDoc);

            JackParser.AddToken(subroutineCallRootNode, subRoutinNameToken, TokenTypes.identifier, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            JackParser.AppendExpression(subroutineCallRootNode, tokensDoc);

        }



        #region Statements
        /// <summary>
        /// Gets the statements from head of tokens doc and appends them to parent Node.
        /// </summary>
        /// <param name="parentNode">The parent node.</param>
        /// <param name="tokensDoc">The tokens doc.</param>
        private static void AppendStatements(XmlNode parentNode, XmlDocument tokensDoc)
        {
            //create Statements root element
            XmlNode rootStatements = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.statements.ToStringByDescription(), String.Empty);
            //adding the variable declaration node
            parentNode.AppendChild(rootStatements);

            while (JackParser.IsStatementBegining(tokensDoc))
            {
                JackParser.AppendStatement(rootStatements, tokensDoc);
            }

        }

        /// <summary>
        /// Appends a single statement to rootStatements node.
        /// </summary>
        /// <param name="parentNode">The parentNode.</param>
        /// <param name="tokensDoc">The tokens doc.</param>
        private static void AppendStatement(XmlNode parentNode, XmlDocument tokensDoc)
        {
            //create Statements root element
            XmlNode rootStatement = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.statement.ToStringByDescription(), String.Empty);
            //adding the variable declaration node
            parentNode.AppendChild(rootStatement);

            XmlNode nextNode = JackParser.GetNextToken(tokensDoc);
            string firstStatementNodeString = (nextNode == null ? String.Empty : nextNode.InnerText).Trim();

            switch (firstStatementNodeString)
            {
                case "let":
                    JackParser.AppendLetStatement(parentNode, tokensDoc);
                    break;
                case "if":
                    JackParser.AppendIfStatement(parentNode, tokensDoc);
                    break;
                case "while":
                    JackParser.AppendWhileStatement(parentNode, tokensDoc);
                    break;
                case "do":
                    JackParser.AppendDoStatement(parentNode, tokensDoc);
                    break;
                case "return":
                    JackParser.AppendReturnStatement(parentNode, tokensDoc);
                    break;
                default:
                    throw new Exception(String.Format("Unknown statement type ({0})", firstStatementNodeString));
            }
            /*Example*/
            //XmlNode varTypeToken = JackParser.GetNextToken(tokensDoc);
            //List<string> possibleTexts = JackParser.GetValidTypeName(varTypeToken.InnerText);
            //JackParser.AddToken(rootPramList, varTypeToken, possibleTexts, TokenTypes.keyword, ProgramStructureNodes.type.ToStringByDescription());
            ////token was handled, remove it
            //JackParser.RemoveFirstToken(tokensDoc);
        }

        private static void AppendReturnStatement(XmlNode parentNode, XmlDocument tokensDoc)
        {
            //create Statements root element
            XmlNode rootReturnNode = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.ifStatement.ToStringByDescription(), String.Empty);
            //adding the variable declaration node
            parentNode.AppendChild(rootReturnNode);

            /*return value*/
            XmlNode retValToken = JackParser.GetNextToken(tokensDoc);
            if (retValToken.InnerText.Trim() != ";")
            {
                JackParser.AppendExpression(rootReturnNode, tokensDoc);
            }


            /*;*/
            XmlNode semiCommaToken = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootReturnNode, semiCommaToken, ";", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);


        }

        private static void AppendDoStatement(XmlNode parentNode, XmlDocument tokensDoc)
        {
            //create Statements root element
            XmlNode rootdoNode = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.ifStatement.ToStringByDescription(), String.Empty);
            //adding the variable declaration node
            parentNode.AppendChild(rootdoNode);

            JackParser.AppendSubroutineCall(rootdoNode, tokensDoc);
        }




        private static void AppendWhileStatement(XmlNode parentNode, XmlDocument tokensDoc)
        {
            //create Statements root element
            XmlNode rootWhileNode = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.ifStatement.ToStringByDescription(), String.Empty);
            //adding the variable declaration node
            parentNode.AppendChild(rootWhileNode);

            /*while*/
            XmlNode whileToken = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootWhileNode, whileToken, "while", TokenTypes.keyword, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
            //'(' Expression ')'
            JackParser.AppendExpressionInBrackets(tokensDoc, rootWhileNode);

            //'{' Statements '}'
            JackParser.AppendStatementsInCurlyBrackets(rootWhileNode, tokensDoc);
        }

        private static void AppendIfStatement(XmlNode parentNode, XmlDocument tokensDoc)
        {
            //create Statements root element
            XmlNode rootIfNode = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.ifStatement.ToStringByDescription(), String.Empty);
            //adding the variable declaration node
            parentNode.AppendChild(rootIfNode);

            /*if*/
            XmlNode ifToken = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootIfNode, ifToken, "if", TokenTypes.keyword, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
            //'(' Expression ')'
            JackParser.AppendExpressionInBrackets(tokensDoc, rootIfNode);

            //'{' Statements '}'
            JackParser.AppendStatementsInCurlyBrackets(rootIfNode, tokensDoc);


            /*else*/
            XmlNode elseToken = JackParser.GetNextToken(tokensDoc);
            if (elseToken.InnerText.Trim() == "else")
            {
                /*else*/
                JackParser.AddToken(rootIfNode, elseToken, "else", TokenTypes.keyword, null);
                //token was handled, remove it
                JackParser.RemoveFirstToken(tokensDoc);

                //'{' Statements '}'
                JackParser.AppendStatementsInCurlyBrackets(rootIfNode, tokensDoc);
            }
        }

        /// <summary>
        /// Gets the expressions in curly brackets: '{' Statements '}'
        /// </summary>
        /// <param name="rootIfNode">The root if node.</param>
        /// <param name="tokensDoc">The tokens doc.</param>
        private static void AppendStatementsInCurlyBrackets(XmlNode rootIfNode, XmlDocument tokensDoc)
        {
            /*{*/
            XmlNode tOpenBracket = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootIfNode, tOpenBracket, "{", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            JackParser.AppendStatements(rootIfNode, tokensDoc);

            /*}*/
            XmlNode tEndBracket = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootIfNode, tEndBracket, "}", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
        }

        private static void AppendLetStatement(XmlNode parentNode, XmlDocument tokensDoc)
        {

            //create Statements root element
            XmlNode rootLetNode = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.statements.ToStringByDescription(), String.Empty);
            //adding the variable declaration node
            parentNode.AppendChild(rootLetNode);
            /*Let*/
            XmlNode letToken = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootLetNode, letToken, "let", TokenTypes.keyword, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            /*variable name*/
            XmlNode varNameToken = JackParser.GetNextToken(tokensDoc);
            bool isvalidName = JackParser.IsVariableName(varNameToken.InnerText);
            /*Not valid name; quit*/
            if (!isvalidName) { throw new Exception(String.Format("variable name {0} is not valid", varNameToken.InnerText)); }

            JackParser.AddToken(rootLetNode, varNameToken, TokenTypes.identifier, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            /*Indexer*/

            XmlNode indexerStartToken = JackParser.GetNextToken(tokensDoc);
            if (indexerStartToken.InnerText.Trim() == "[")
            {
                /*[*/
                JackParser.AddToken(rootLetNode, indexerStartToken, "[", TokenTypes.symbol, null);
                //token was handled, remove it
                JackParser.RemoveFirstToken(tokensDoc);

                JackParser.AppendExpression(rootLetNode, tokensDoc);


                /*]*/
                XmlNode indexerEndToken = JackParser.GetNextToken(tokensDoc);
                JackParser.AddToken(rootLetNode, indexerEndToken, "]", TokenTypes.symbol, null);
                //token was handled, remove it
                JackParser.RemoveFirstToken(tokensDoc);
            }

            /*=*/
            XmlNode equalityToken = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootLetNode, equalityToken, "=", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            JackParser.AppendExpressionList(rootLetNode, tokensDoc);


            /*;*/
            XmlNode semoCommaToken = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootLetNode, semoCommaToken, ";", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

        }
        #endregion

        #region Token handeling
        /// <summary>
        /// Gets the term type of the first token.
        /// </summary>
        /// <param name="tokensDoc">The tokens doc.</param>
        /// <returns>the type of term (none if it not a term)</returns>
        private static TermTypes GetNextTokenTermType(XmlDocument tokensDoc)
        {

            if (JackParser.IsNextNodeTerm_integerConstant(tokensDoc)) { return TermTypes.integerConstant; }
            if (JackParser.IsNextNodeTerm_stringConstant(tokensDoc)) { return TermTypes.stringConstant; }
            if (JackParser.IsNextNodeTerm_keywoedConstant(tokensDoc)) { return TermTypes.keywoedConstant; }
            if (JackParser.IsNextNodeTerm_varName(tokensDoc)) { return TermTypes.varName; }
            if (JackParser.IsNextNodeTerm_Array(tokensDoc)) { return TermTypes.Array; }
            if (JackParser.IsNextNodeTerm_subRoutineCall(tokensDoc)) { return TermTypes.subRoutineCall; }
            if (JackParser.IsNextNodeTerm_ExpressionInBrackets(tokensDoc)) { return TermTypes.ExpressionInBrackets; }
            if (JackParser.IsNextNodeTerm_UnaryOpAndTerm(tokensDoc)) { return TermTypes.UnaryOpAndTerm; }

            return TermTypes.None;


        }



        /// <summary>
        /// Gets the type of the first token.
        /// </summary>
        /// <param name="tokensDoc">The tokens doc.</param>
        /// <returns></returns>
        private static TokenTypes GetNextTokenType(XmlDocument tokensDoc)
        {
            return JackParser.GetTokenTypeAtIndex(tokensDoc, 1);
        }

        private static TokenTypes GetTokenTypeAtIndex(XmlDocument tokensDoc, int index)
        {
            XmlNode node = JackParser.GetTokenAtIndex(tokensDoc, index);
            string name = node != null ? node.Name : String.Empty;
            TokenTypes nodeType;
            if (!Enum.TryParse<TokenTypes>(name, out nodeType))
            {
                nodeType = TokenTypes.other;
            }
            return nodeType;
        }

        /// <summary>
        /// Gets the text of the first token.
        /// </summary>
        /// <param name="tokensDoc">The tokens doc.</param>
        /// <returns>the value (text) of first doc</returns>
        private static string GetFirstTokenText(XmlDocument tokensDoc)
        {
            return JackParser.GetTokenTextAtIndex(tokensDoc, 1);
        }

        private static string GetTokenTextAtIndex(XmlDocument tokensDoc, int index)
        {
            XmlNode node = JackParser.GetTokenAtIndex(tokensDoc, index);
            string text = node != null ? node.InnerText : String.Empty;
            return text.Trim();

        }


        /// <summary>
        /// Gets the next (first) token from tokens xml document.
        /// <remarks>
        /// This acts as "Peek"and does not effect the document itslef
        /// </remarks>
        /// </summary>
        /// <param name="tokensDoc">The tokens doc.</param>
        /// <returns>the first token node</returns>
        private static XmlNode GetNextToken(XmlDocument tokensDoc)
        {
            return (tokensDoc != null && tokensDoc.FirstChild != null) ? tokensDoc.FirstChild.FirstChild : null;
        }

        /// <summary>
        /// Gets the token at the specified index.
        /// </summary>
        /// <param name="tokensDoc">The tokens doc.</param>
        /// <param name="index">The 1-based index.</param>
        /// <returns>the node at specified index</returns>
        private static XmlNode GetTokenAtIndex(XmlDocument tokensDoc, int index)
        {
            XmlNode retNode = JackParser.GetNextToken(tokensDoc);
            index--; //we have extracted 1...
            for (int i = 0; i < index && i > 0 && retNode != null; i++)
            {
                retNode = retNode.NextSibling;
            }

            return retNode;
        }
        /// <summary>
        /// Adds the class token to class root.
        /// </summary>
        /// <param name="parentNode">The node to add the token to.</param>
        /// <param name="token">The token node to be added.</param>
        /// <param name="expectedNodeInnerText">The expected node inner text.</param>
        /// <param name="expectedNodeType">Expected type of the node.</param>
        private static void AddToken(XmlNode parentNode, XmlNode token, TokenTypes expectedNodeType, string encapsulatingTagText)
        {
            JackParser.AddToken(parentNode, token, String.Empty, expectedNodeType, encapsulatingTagText);
        }

        /// <summary>
        /// Adds the class token to class root.
        /// </summary>
        /// <param name="parentNode">The node to add the token to.</param>
        /// <param name="token">The token node to be added.</param>
        /// <param name="expectedNodeInnerText">The expected node inner text.</param>
        /// <param name="expectedNodeType">Expected type of the node.</param>
        private static void AddToken(XmlNode parentNode, XmlNode token, OutputStructureNodes expectedNodeInnerText, TokenTypes expectedNodeType, string encapsulatingTagText)
        {
            JackParser.AddToken(parentNode, token, expectedNodeInnerText.ToString(), expectedNodeType, encapsulatingTagText);
        }

        /// <summary>
        /// Adds the class token to class root.
        /// </summary>
        /// <param name="parentNode">The node to add the token to.</param>
        /// <param name="token">The token node to be added.</param>
        /// <param name="expectedNodeInnerText">The expected node inner text.</param>
        /// <param name="expectedNodeType">Expected type of the node.</param>
        private static void AddToken(XmlNode parentNode, XmlNode token, string expectedText, TokenTypes expectedNodeType, string encapsulatingTagText)
        {
            JackParser.AddToken(parentNode, token, new List<string> { expectedText }, expectedNodeType, encapsulatingTagText);
        }
        /// <summary>
        /// Adds the class token to class root.
        /// </summary>
        /// <param name="parentNode">The node to add the token to.</param>
        /// <param name="token">The token node to be added.</param>
        /// <param name="expectedNodeInnerText">The expected node inner text.</param>
        /// <param name="expectedNodeType">Expected type of the node.</param>
        private static void AddToken(XmlNode parentNode, XmlNode token, IEnumerable<string> possibleText, TokenTypes expectedNodeType, string encapsulatingTagText)
        {
            #region validations
            bool throwExp = true;
            JackParser.ValidateToken(token, possibleText, expectedNodeType, throwExp);
            #endregion
            //adding class token
            XmlNode importedToken = parentNode.OwnerDocument.ImportNode(token, true);

            //it is common to add a "surrounding Node" to a newly added node...
            XmlNode surroundingNode = null;
            if (!String.IsNullOrWhiteSpace(encapsulatingTagText))
            {
                surroundingNode = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, encapsulatingTagText, String.Empty);
                parentNode.AppendChild(surroundingNode);
            }


            (surroundingNode ?? parentNode).AppendChild(importedToken);

        }

        /// <summary>
        /// Removes the first token from tokens XML. 
        /// Typically will be called after a Token handling was done and token is no longer required.
        /// </summary>
        /// <param name="tokensDoc">The tokens xml doc.</param>
        /// <exception cref="System.ArgumentException">supplied xml document is not a Tokens document</exception>
        private static void RemoveFirstToken(XmlDocument tokensDoc)
        {
            if (tokensDoc.FirstChild.Name != "tokens")
            {
                throw new ArgumentException("supplied xml document is not a Tokens document");
            }
            tokensDoc.FirstChild.RemoveChild(tokensDoc.FirstChild.FirstChild);
        }
        #endregion

        #region Assertions
        /// <summary>
        /// Asserts the specified expression.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="expectedText">The expected text.</param>
        /// <param name="errorMessage">The error message in exception, if thrown.</param>
        /// <exception cref="System.Exception">Exception with epecified message</exception>
        private static bool AssertNodeText(XmlNode node, string expectedText, string errorMessage, bool throwException)
        {
            return JackParser.AssertNodeText(node, new List<string> { expectedText }, errorMessage, throwException);
        }

        /// <summary>
        /// Asserts the specified expression.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="possibleText">The list of possible text to match.</param>
        /// <param name="errorMessage">The error message in exception, if thrown.</param>
        /// <exception cref="System.Exception">Exception with epecified message</exception>
        private static bool AssertNodeText(XmlNode node, List<string> possibleText, string errorMessage, bool throwException)
        {
            bool foundMatch = possibleText.Any(str => String.Equals(node.InnerText.Trim(), str.Trim(), StringComparison.OrdinalIgnoreCase));
            if (throwException && !foundMatch)
            {
                throw new Exception(errorMessage);
            }
            return foundMatch;
        }

        /// <summary>
        /// Asserts the specified expression.
        /// </summary>
        /// <param name="expression">if is <c>false</c> will throw an exception.</param>
        /// <param name="errorMessage">The error message in exception, if thrown.</param>
        /// <exception cref="System.Exception">Exception with epecified message</exception>
        private static bool AssertNodeType(XmlNode node, TokenTypes expectedType, string errorMessage, bool throwException)
        {
            return AssertNodeType(node, new List<TokenTypes> { expectedType }, errorMessage, throwException);
        }
        /// <summary>
        /// Asserts the specified expression.
        /// </summary>
        /// <param name="expression">if is <c>false</c> will throw an exception.</param>
        /// <param name="errorMessage">The error message in exception, if thrown.</param>
        /// <exception cref="System.Exception">Exception with epecified message</exception>
        private static bool AssertNodeType(XmlNode node, List<TokenTypes> expectedType, string errorMessage, bool throwException)
        {
            bool isMatchesAny = expectedType.Any(et => String.Equals(node.Name.Trim(), et.ToStringByDescription(), StringComparison.OrdinalIgnoreCase));
            if (throwException && !isMatchesAny)
            {
                throw new Exception(errorMessage);
            }
            return isMatchesAny;
        }

        private static bool ValidateToken(XmlNode token, IEnumerable<string> possibleText, TokenTypes expectedNodeType, bool throwException)
        {
            bool valid = true;
            /*is text valid?*/
            List<string> cleanPossibleText = possibleText.Where(s => !String.IsNullOrWhiteSpace(s)).ToList();
            if (possibleText != null && cleanPossibleText.Count() > 0)
            {
                string errorMsg = String.Format("Expected node to have inner text of: {0}", String.Join("|", cleanPossibleText));
                valid &= JackParser.AssertNodeText(token, cleanPossibleText, errorMsg, throwException);

            }


            string msg = String.Format("Expected node to be of type: {0}", expectedNodeType.ToStringByDescription());
            valid &= JackParser.AssertNodeType(token, expectedNodeType, msg, throwException);
            return valid;
        }
        #endregion

        #region Checking for values

        private static bool IsNextTokenTerm(XmlDocument tokensDoc)
        {
            TermTypes termType = JackParser.GetNextTokenTermType(tokensDoc);
            return termType != TermTypes.None;
        }
        
        /// <summary>
        /// Determines whether the specified candidate is legal Sub Routine Name.
        /// </summary>
        /// <param name="candidate">The candidate.</param>
        /// <returns>
        ///   <c>true</c> if the specified candidate is legal Sub Routine Name; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsClassName(string candidate)
        {
            return JackParser.IsIdentifier(candidate);
        }
        /// <summary>
        /// Determines whether the specified candidate is legal class name.
        /// </summary>
        /// <param name="candidate">The candidate.</param>
        /// <returns>
        ///   <c>true</c> if the specified candidate is legal class name; otherwise, <c>false</c>.
        /// 
        private static bool IsSubRoutineName(string candidate)
        {
            return JackParser.IsIdentifier(candidate);
        }
        /// <summary>
        /// Determines whether the specified candidate is legal variable name.
        /// </summary>
        /// <param name="candidate">The candidate.</param>
        /// <returns>
        ///   <c>true</c> if the specified candidate is legal variable name; otherwise, <c>false</c>.
        /// 
        private static bool IsVariableName(string candidate)
        {
            return JackParser.IsIdentifier(candidate);
        }
        /// <summary>
        /// Determines whether tokens head token is a begining of subroutine call
        /// </summary>
        /// <param name="tokensDoc">The tokens doc.</param>
        /// <param name="subRoutineOwner">The sub routine owner.
        /// (class|var).functionName() ==>  <see cref="OutputStructureNodes.classNam"/> 
        /// functionName() ==>   <see cref="OutputStructureNodes.SubRoutineName"/>
        /// </param>
        /// <returns>
        ///   <c>true</c> if tokens head token is a begining of subroutine call; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsSubroutineCallBegining(XmlDocument tokensDoc, out OutputStructureNodes subRoutineOwner)
        {
            bool isSubroutineCallBegining = false;
            TokenTypes tType = JackParser.GetNextTokenType(tokensDoc);
            string nodeText = JackParser.GetFirstTokenText(tokensDoc);


            //className,
            //subroutineName,
            //varName,

            switch (tType)
            {
                case TokenTypes.identifier:
                    isSubroutineCallBegining =
                        JackParser.IsClassName(nodeText)
                        || JackParser.IsSubRoutineName(nodeText)
                        || JackParser.IsVariableName(nodeText);
                    TokenTypes nextyNodeType = JackParser.GetTokenTypeAtIndex(tokensDoc, 2);
                    string nextyNodeText = JackParser.GetTokenTextAtIndex(tokensDoc, 2);

                    if (nextyNodeType == TokenTypes.symbol && nextyNodeText == ".")
                    {
                        //We are facing a situation of : (class|var).functionName()
                        subRoutineOwner = OutputStructureNodes.className;
                    }
                    else
                    {
                        //We are facing a situation of : functionName()
                        subRoutineOwner = OutputStructureNodes.subroutineName;
                    }
                    break;
                default:
                    isSubroutineCallBegining = false;
                    subRoutineOwner = OutputStructureNodes.unknown;
                    break;
            }

            return isSubroutineCallBegining;
        }

        /// <summary>
        /// Determines whether the specified candidate is a legal identifier name.
        /// </summary>
        /// <param name="candidate">The candidate.</param>
        /// <returns>
        ///   <c>true</c> if the specified candidate is legal identifier name; otherwise, <c>false</c>.
        /// 
        private static bool IsIdentifier(string candidate)
        {
            string cleanCandidate = candidate.Trim();
            Regex regex = new Regex("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.None);
            bool isLegalIdentifier = regex.Match(cleanCandidate).Success;

            //makes sense that a keyword cannot be an identifer, but it does not seem to be the case according to class PPT
            isLegalIdentifier &= !JackParser._allKeyWords.Contains(cleanCandidate);

            return isLegalIdentifier;
        }

        /// <summary>
        /// Determines whether tokens head token is a binary operator
        /// </summary>
        /// <param name="tokensDoc">The tokens doc.</param>
        /// <returns>
        ///   <c>true</c> if tokens head token is a binary operator; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsNextNodeBinaryOp(XmlDocument tokensDoc)
        {
            XmlNode nextNode = JackParser.GetNextToken(tokensDoc);
            string firstStatementNodeString = (nextNode == null ? String.Empty : nextNode.InnerText).Trim();
            return JackParser._oparations.Contains(firstStatementNodeString);
        }

        /// <summary>
        /// Determines whether tokens head token is a statement begining
        /// </summary>
        /// <param name="tokensDoc">The tokens doc.</param>
        /// <returns>
        ///   <c>true</c> if tokens head token is a statement begining; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsStatementBegining(XmlDocument tokensDoc)
        {
            XmlNode nextNode = JackParser.GetNextToken(tokensDoc);
            string firstStatementNodeString = (nextNode == null ? String.Empty : nextNode.InnerText).Trim();
            return JackParser._statementsHeaders.Contains(firstStatementNodeString);
        }

        /// <summary>
        /// Determines whether next node is a variable declaration begining at the specified tokens xml.
        /// </summary>
        /// <param name="tokensDoc">The tokens xml.</param>
        /// <returns>
        ///   <c>true</c> if next node is a variable declaration begining; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsVarDecBegining(XmlDocument tokensDoc)
        {
            bool isVarDecBegining = false;

            try
            {
                TokenTypes tType = GetNextTokenType(tokensDoc);
                string text = JackParser.GetFirstTokenText(tokensDoc);

                isVarDecBegining = tType == TokenTypes.keyword && JackParser._variablesModifiers.Contains(text);
            }
            catch (Exception)
            {
                isVarDecBegining = false;
            }


            return isVarDecBegining;


        }

        /// <summary>
        /// Determines whether next node is a subroutin declaration begining at the specified tokens xml.
        /// </summary>
        /// <param name="tokensDoc">The tokens xml.</param>
        /// <returns>
        ///   <c>true</c> if next node is a subroutin begining; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsSubroutineBegining(XmlDocument tokensDoc)
        {
            bool issubRoutinBegining = false;

            try
            {
                TokenTypes tType = GetNextTokenType(tokensDoc);
                string text = JackParser.GetFirstTokenText(tokensDoc);

                issubRoutinBegining = tType == TokenTypes.keyword && JackParser._subRoutineModifiers.Contains(text);
            }
            catch (Exception)
            {
                issubRoutinBegining = false;
            }


            return issubRoutinBegining;

        }

        private static bool IsNextNodeTerm_UnaryOpAndTerm(XmlDocument tokensDoc)
        {
              string text = JackParser.GetFirstTokenText(tokensDoc);
              return JackParser._unariOparations.Contains(text);
        }

        private static bool IsNextNodeTerm_ExpressionInBrackets(XmlDocument tokensDoc)
        {
            string text = JackParser.GetFirstTokenText(tokensDoc);
            bool hasOpeningBracket = text == "(";
            return hasOpeningBracket;
        }

        private static bool IsNextNodeTerm_subRoutineCall(XmlDocument tokensDoc)
        {
            string text = JackParser.GetFirstTokenText(tokensDoc);
            bool isVarName = JackParser.IsVariableName(text);
            string nextString = JackParser.GetTokenTextAtIndex(tokensDoc, 2);
            bool hasOpeningBracket = nextString == "(";
            return isVarName && hasOpeningBracket;
        }

        private static bool IsNextNodeTerm_Array(XmlDocument tokensDoc)
        {
              string text = JackParser.GetFirstTokenText(tokensDoc);
              bool isVarName = JackParser.IsVariableName(text);
              string nextString = JackParser.GetTokenTextAtIndex(tokensDoc, 2);
              bool hasIndexer = nextString == "[";
              return isVarName && hasIndexer;
        }

        private static bool IsNextNodeTerm_varName(XmlDocument tokensDoc)
        {
              string text = JackParser.GetFirstTokenText(tokensDoc);
              return JackParser.IsVariableName(text);
        }

        private static bool IsNextNodeTerm_keywoedConstant(XmlDocument tokensDoc)
        {
            string text = JackParser.GetFirstTokenText(tokensDoc);
            return JackParser._allKeyWords.Contains(text);
        }

        private static bool IsNextNodeTerm_stringConstant(XmlDocument tokensDoc)
        {
            bool isStringConst = true;
             bool isQuoteSurronded=false;
            bool allValidChars = false;
            string text = JackParser.GetFirstTokenText(tokensDoc);
            try 
	        {	        
		         isQuoteSurronded = text[0] == '\"' && text[text.Length -1] == '\"';
                char[] disallowdChars = new char[]{'\n','\"'};
                allValidChars = text.IndexOfAny(disallowdChars,1,text.Length-1) <0 ;
	        }
	        catch (Exception)
	        {
		
		        isStringConst = false;
	        }
           isStringConst &= isQuoteSurronded &allValidChars;
           return isStringConst;
        }

        private static bool IsNextNodeTerm_integerConstant(XmlDocument tokensDoc)
        {
            string text = JackParser.GetFirstTokenText(tokensDoc);
            int temp;
            return int.TryParse(text, out temp);
        }


        #endregion

        #region Enumerations
        /// <summary>
        /// Xml Nodes types in output that represents high level noes (i.e. the nodes that enables hierarchy)
        /// </summary>
        private enum OutputStructureNodes
        {
            unknown,
            [Description("class")]
            @class,
            [Description("classVarDec")]
            classVarDec,
            [Description("type")]
            @type,
            [Description("subroutineDec")]
            subroutineDec,
            [Description("parameterList")]
            parameterList,
            [Description("subroutineBody")]
            subroutineBody,
            [Description("varDec")]
            varDec,
            [Description("className")]
            className,
            [Description("subroutineName")]
            subroutineName,
            [Description("varName")]
            varName,
            [Description("statements")]
            statements,
            [Description("statement")]
            statement,
            [Description("letStatement")]
            letStatement,
            [Description("doStatement")]
            doStatement,
            [Description("ifStatement")]
            ifStatement,
            [Description("whileStatement")]
            whileStatement,
            [Description("subroutineCall")]
            subroutineCall,







        }

        /// <summary>
        /// Types that token XML nodes might have
        /// </summary>        
        private enum TokenTypes
        {
            [Description("keyword")]
            keyword,
            [Description("identifier")]
            identifier,
            [Description("symbol")]
            symbol,
            [Description("op")]
            operation,
            [Description("unaryOp")]
            unaricOperation,
            other,

        }

        enum TermTypes
        {
            None,
            integerConstant,
            stringConstant,
            keywoedConstant,
            varName,
            Array,
            subRoutineCall,
            ExpressionInBrackets,
            UnaryOpAndTerm
        }
        #endregion
    }
}
