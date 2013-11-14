using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace JackParser
{
    public static class JackParser
    {
        

        /// <summary>
        /// for debugging, helps to keep track
        /// </summary>
        static int _TokenRowNumber = 1;
        static string _DEBUG_xmlString;
        static int _DEBUG_xmlRowNumber = 0;


        #region C'tor
        static JackParser()
        {
            var srRetTypes = new List<string>(SymbolClassifications._variablesTypes);
            srRetTypes.Add("void");

            List<string> allKeyWords = new List<string>();

            /*symbols*/
            List<string> allSymbols = new List<string>() { "[", "]", "{", "}", "(", ")", ".", ",", ";" };
            allSymbols.AddRange(SymbolClassifications._oparations);
            allSymbols.AddRange(SymbolClassifications._unariOparations);
            SymbolClassifications._symbols = new ReadOnlyCollection<string>(allSymbols);

            /*all keywords*/
            SymbolClassifications._subRoutineReturnType = new ReadOnlyCollection<string>(srRetTypes);
            allKeyWords.AddRange(SymbolClassifications._generalKeyWords);
            allKeyWords.AddRange(SymbolClassifications._classVariablesModifiers);
            allKeyWords.AddRange(SymbolClassifications._subRoutineReturnType);
            allKeyWords.AddRange(SymbolClassifications._statementsHeaders);
            allKeyWords.AddRange(SymbolClassifications._oparations);
            allKeyWords.AddRange(SymbolClassifications._unariOparations);
            allKeyWords.AddRange(SymbolClassifications._symbols);
            allKeyWords.AddRange(SymbolClassifications._variablesTypes.Distinct());
            allKeyWords.AddRange(SymbolClassifications._constantKeyWords);
            allKeyWords.AddRange(SymbolClassifications._variablesModifiers);


            allKeyWords.AddRange(SymbolClassifications._subRoutineModifiers);
            SymbolClassifications._allKeyWords = new ReadOnlyCollection<string>(allKeyWords.Distinct().ToList());
        }
        #endregion

        #region Internal Methods

        public static List<Exception> Start(string folderName)
        {
            List<Exception> exList = new List<Exception>();
            var jackFiles = Directory.GetFiles(folderName, "*T.xml");
            foreach (string fileName in jackFiles)
            {
                XmlDocument tokens = new XmlDocument();
                try
                {
                    tokens.Load(fileName);
                }
                catch (Exception ex)
                {

                    Exception e = new Exception("Got bad tokens document: "+ fileName,ex);
                    exList.Add(e);
                    continue;
                }

                try
                {
                    string xmlStr = JackParser.GetCleanJackXmlStringFromTokens(tokens);
                    
                    string[] tmp = fileName.Split('.');
                    string fName = tmp[0];
                    if (fName.EndsWith("T"))
                    {
                        fName = fName.Substring(0, fName.Length - 1);
                    }
                    fName = fName + ".xml";
                    System.IO.File.WriteAllText(fName, xmlStr);
                }
                catch (Exception ex)
                {

                    Exception e = new Exception("Failed to parse tokens file: " + fileName, ex);
                    exList.Add(e);
                    continue;
                }
                
                
            }
            return exList;

        }
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
                settings.NamespaceHandling = NamespaceHandling.Default;
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
            tokenClone.NodeRemoved += tokenClone_NodeRemoved;
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

        static void tokenClone_NodeRemoved(object sender, XmlNodeChangedEventArgs e)
        {
            JackParser._TokenRowNumber++;
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
        internal static string GetCleanJackXmlStringFromTokens(XmlDocument tokens)
        {
            string str = GetJackXmlStringFromTokens(tokens);
             /*Removing first row of XML declaration*/
            int firstRowEnd = str.IndexOf(">", 1);
            if (firstRowEnd>0)
            {
                str = str.Substring(firstRowEnd + 1, str.Length - firstRowEnd-1);
            }

            /*replacing empty single elements with open / close tags*/
            string cleanStr = str.Replace("<expressionList/>", "<expressionList />").Replace("<parameterList/>", "<parameterList />");
            cleanStr = cleanStr.Replace("<parameterList />", String.Format("<parameterList>" + Environment.NewLine + "</parameterList>"))
                .Replace("<expressionList />", String.Format("<expressionList>" + Environment.NewLine + "</expressionList>"));


            /*Reformatting as per late instructor request*/
            /*newline after closing tag of: identifier, stringConstant, integerConstant, symbol, keyword 
             *all other tags - newline after opening\closing tags
             */
            cleanStr = Regex.Replace(cleanStr, @"</*[a-z]*[A-Z]*>", delegate(Match match)
	        {
	            string endTag = match.ToString();
                string retTag = endTag+Environment.NewLine;
                return retTag;
	        },RegexOptions.IgnoreCase);


            string[] oneLineTags = 
            {
               "identifier",
               "stringConstant",
               "integerConstant",
                "symbol",
                "keyword"
            };

            foreach (string currTag in oneLineTags)
            {
                /*remove new line after opening tag*/
                string regexPattern = @"<" + currTag + ">" + Environment.NewLine;
                cleanStr = Regex.Replace(cleanStr, regexPattern, delegate(Match match)
                {
                    string openingTag = match.ToString();
                    string retTag = openingTag.Replace(Environment.NewLine,String.Empty).Trim();
                    return retTag;
                }, RegexOptions.IgnoreCase);
                
            }




            cleanStr = cleanStr.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine).Trim();
            return cleanStr;
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
            //for debugging
            xml.NodeInserted += xml_NodeInserted;

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
            VMCodeWriter.ClassName = JackParser.GetFirstTokenText(tokensDoc);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            /*{*/
            XmlNode tOpenBracket = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(classRoot, tOpenBracket, "{", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
            
            /*Variable declarations*/
            JackParser.AppendVariableDeclarations(classRoot, tokensDoc,true);

            /*Sub routine declarations*/
            JackParser.AppendSubRoutinesDeclarations(classRoot, tokensDoc);

            /*}*/
            XmlNode tEndBracket = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(classRoot, tEndBracket, "}", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
            return;


        }

        static void xml_NodeInserted(object sender, XmlNodeChangedEventArgs e)
        {
            if (e.Node != null)
            {
                UpdateString(e.Node.OwnerDocument);
            }
            
        }

        [Conditional("DEBUG")]
        private static void UpdateString(XmlDocument xmlDocument)
        {
            if (xmlDocument != null)
            {
                JackParser._DEBUG_xmlString = xmlDocument.ToXmlString();
                _DEBUG_xmlRowNumber++;
            }
        }

        /// <summary>
        /// Appends the variable declarations nodes to the specified class root node.
        /// </summary>
        /// <param name="classRoot">The class root node.</param>
        /// <param name="tokensDoc">The tokens doc to take nodes from.</param>
        /// <param name="classVariable">if set to <c>true</c> will expect variables for class template, otherwise local variables for subroutine.</param>
        private static void AppendVariableDeclarations(XmlNode classRoot, XmlDocument tokensDoc, bool classVariable)
        {

            while (JackParser.IsVarDecBegining(tokensDoc))
            {
                JackParser.AppendVariableDeclaration(classRoot, tokensDoc, classVariable);
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
        /// <param name="isClassVariable">if set to <c>true</c> will expect variables for class template, otherwise local variables for subroutine.</param>
        private static void AppendVariableDeclaration(XmlNode classRoot, XmlDocument tokensDoc, bool isClassVariable)
        {
            XmlDocument xml = classRoot.OwnerDocument;
            //create classVarDec root element
            OutputStructureNodes nodeType = isClassVariable ? OutputStructureNodes.classVarDec : OutputStructureNodes.varDec;
            XmlNode rootVarDec = xml.CreateNode(XmlNodeType.Element, nodeType.ToStringByDescription(), String.Empty);
            //adding the variable declaration node
            classRoot.AppendChild(rootVarDec);

            /*field | static | var------------------------------------------------------------------------*/
            XmlNode tKeyWordModifier = JackParser.GetNextToken(tokensDoc);
            IEnumerable<string> validModifiers = isClassVariable ? (IEnumerable<string>)SymbolClassifications._classVariablesModifiers : new List<string> { "var" };
            JackParser.AddToken(rootVarDec, tKeyWordModifier, validModifiers, TokenTypes.keyword, null);
            
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            /*var type------------------------------------------------------------------------*/
            XmlNode varTypeToken = JackParser.GetNextToken(tokensDoc);

            List<string> possibleTexts = JackParser.GetValidTypeName(varTypeToken.InnerText);

            JackParser.AddToken(rootVarDec, varTypeToken, possibleTexts, new List<Enum> { TokenTypes.identifier, TokenTypes.keyword }, OutputStructureNodes.type.ToStringByDescription());
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
                VMCodeWriter.AddVariablbe(tKeyWordModifier.InnerText, varNameToken.InnerText);
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

            JackParser.AppendFunctionDecHeader(rootSubDec, tokensDoc);

            JackParser.AppendFunctionDecBody(tokensDoc, rootSubDec);
        }

        /// <summary>
        /// Appends the function decleration body.
        /// </summary>
        /// <param name="tokensDoc">The tokens doc.</param>
        /// <param name="rootSubDec">The root sub dec.</param>
        private static void AppendFunctionDecBody(XmlDocument tokensDoc, XmlNode rootSubDec)
        {
            XmlDocument xml = rootSubDec.OwnerDocument;
            //create classVarDec root element
            XmlNode rootSubroutineBodyNode = xml.CreateNode(XmlNodeType.Element, OutputStructureNodes.subroutineBody.ToStringByDescription(), String.Empty);
            //adding the variable declaration node
            rootSubDec.AppendChild(rootSubroutineBodyNode);


            /*{------------------------------------------------------------------------*/
            XmlNode openBodyBracket = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootSubroutineBodyNode, openBodyBracket, "{", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            JackParser.AppendVariableDeclarations(rootSubroutineBodyNode, tokensDoc, false);

            JackParser.AppendStatements(rootSubroutineBodyNode, tokensDoc);

            /*}------------------------------------------------------------------------*/
            XmlNode closeBodyBracket = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootSubroutineBodyNode, closeBodyBracket, "}", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
        }

        /// <summary>
        /// Appends the function decleration header header.
        /// </summary>
        /// <param name="tokensDoc">The tokens doc.</param>
        /// <param name="rootSubDec">The root sub dec.</param>
        /// <exception cref="System.Exception"></exception>
        private static void AppendFunctionDecHeader(XmlNode rootSubDec, XmlDocument tokensDoc)
        {
            /*constructor | function | method------------------------------------------------------------------------*/
            XmlNode tFuncType = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootSubDec, tFuncType, SymbolClassifications._subRoutineModifiers, TokenTypes.keyword, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            /*function return type------------------------------------------------------------------------*/
            XmlNode retTypeToken = JackParser.GetNextToken(tokensDoc);


            List<string> possibleTexts = JackParser.GetValidTypeName(retTypeToken.InnerText);
            List<Enum> possibleTpyes = new List<Enum> {TokenTypes.identifier /*for int, bool...*/, TokenTypes.keyword /*for return type of class...*/};
            bool returnsType = JackParser.ValidateToken(retTypeToken, possibleTexts, possibleTpyes, false);

            
            if (returnsType)
            {
                JackParser.AddToken(rootSubDec, retTypeToken, possibleTexts, possibleTpyes, OutputStructureNodes.type.ToStringByDescription());

            }
            else //must return void...
            {
                JackParser.AddToken(rootSubDec, retTypeToken, "void", TokenTypes.keyword, null);
            }            
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
            int paramCount = JackParser.GetParameterList(rootSubDec, tokensDoc);
            VMCodeWriter.AddFunction(tFuncType.InnerText, varfuncNameToken.InnerText, paramCount);

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
        /// <param name="rootNode">The root node.</param>
        private static void AppendExpressionInBrackets(XmlNode rootNode, XmlDocument tokensDoc, bool expressionList)
        {
            /*(*/
            XmlNode bracketStartToken = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootNode, bracketStartToken, "(", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
            if (expressionList)
            {
                JackParser.AppendExpressionList(rootNode, tokensDoc);
            }
            else
            {
                JackParser.AppendExpression(rootNode, tokensDoc);
            }
            


            /*)*/
            XmlNode bracketEndToken = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootNode, bracketEndToken, ")", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
        }

        /// <summary>
        /// Appends the uary op and term token => unaryOp term.
        /// </summary>
        /// <param name="rootTermToken">The root term token.</param>
        /// <param name="tokensDoc">The tokens doc.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private static void AppendUaryOpAndTerm(XmlNode rootTermToken, XmlDocument tokensDoc)
        {
            /*UnaryOp*/
            XmlNode unaryOpSymbol = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootTermToken, unaryOpSymbol, SymbolClassifications._unariOparations,TokenTypes.symbol,TokenTypes.unaricOperation.ToStringByDescription());
            JackParser.RemoveFirstToken(tokensDoc);
            /*Term*/
            JackParser.AppendTerm(rootTermToken, tokensDoc);

        }


        /// <summary>
        /// Gets the expression in indexer brackets => '[' expression']'.
        /// </summary>
        /// <param name="parentNode">The parent node.</param>
        /// <param name="tokensDoc">The tokens doc.</param>
        private static void GetExpressionInIndexerBrackets(XmlNode parentNode, XmlDocument tokensDoc)
        {
            XmlNode indexerStartToken = JackParser.GetNextToken(tokensDoc);
            /*[*/
            JackParser.AddToken(parentNode, indexerStartToken, "[", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            JackParser.AppendExpression(parentNode, tokensDoc);


            /*]*/
            XmlNode indexerEndToken = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(parentNode, indexerEndToken, "]", TokenTypes.symbol, null);
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
            /*term root*/
            XmlNode expressionNode = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.expression.ToStringByDescription(), String.Empty);
            //adding the variable declaration node
            parentNode.AppendChild(expressionNode);

            JackParser.AppendTerm(expressionNode, tokensDoc);
            JackParser.AppendOpTerms(expressionNode, tokensDoc);
        }
        /// <summary>
        /// Appends the expression list that is composed from nodes at top of tokens Doc to the parent node. Nodes will be removed from tokenns doc.
        /// </summary>
        /// <param name="parentNode">The parent node.</param>
        /// <param name="tokensDoc">The tokens doc.</param>
        private static void AppendExpressionList(XmlNode parentNode, XmlDocument tokensDoc)
        {


            /*Expressions List (could be 0 or more)------------------------------------------------------------------------*/
            ///create class root element
            XmlNode rootParamList = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.expressionList.ToStringByDescription(), String.Empty);
            //adding the class root node
            parentNode.AppendChild(rootParamList);
            while (JackParser.IsNextTokenTerm(tokensDoc))
            {
                JackParser.AppendExpression(rootParamList, tokensDoc);

                string nextNodeText = JackParser.GetFirstTokenText(tokensDoc);
                if (nextNodeText == ",")
                {
                    XmlNode commaToken = JackParser.GetNextToken(tokensDoc);
                    JackParser.AddToken(rootParamList, commaToken, ",", TokenTypes.symbol, null);
                    //token was handled, remove it
                    JackParser.RemoveFirstToken(tokensDoc);
                }
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
                JackParser.AddToken(parentNode, opToken, SymbolClassifications._oparations, TokenTypes.symbol, TokenTypes.operation.ToStringByDescription());
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


            XmlNode termToken = JackParser.GetNextToken(tokensDoc);
            string text = JackParser.GetFirstTokenText(tokensDoc);

            XmlNode rootTermToken;
            TermTypes ttype = JackParser.GetNextTokenTermType(tokensDoc);
            switch (ttype)
            {
                case TermTypes.None:
                    throw new Exception("next node is not a term!");
                case TermTypes.integerConstant:
                    /*term root*/
                    rootTermToken = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.term.ToStringByDescription(), String.Empty);
                    //adding the variable declaration node
                    parentNode.AppendChild(rootTermToken);

                    JackParser.AddToken(rootTermToken, termToken, TermTypes.integerConstant, null);
                    //token was handled, remove it
                    JackParser.RemoveFirstToken(tokensDoc);
                    break;
                case TermTypes.stringConstant:
                    /*term root*/
                    rootTermToken = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.term.ToStringByDescription(), String.Empty);
                    //adding the variable declaration node
                    parentNode.AppendChild(rootTermToken);

                    JackParser.AddToken(rootTermToken, termToken, TermTypes.stringConstant, null);
                    //token was handled, remove it
                    JackParser.RemoveFirstToken(tokensDoc);
                    break;
                case TermTypes.keywordConstant:
                    /*term root*/
                    rootTermToken = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.term.ToStringByDescription(), String.Empty); 
                    //adding the term to the expresssion
                    parentNode.AppendChild(rootTermToken);

                    XmlNode constNode = JackParser.GetNextToken(tokensDoc);

                    JackParser.AddToken(rootTermToken, constNode, SymbolClassifications._constantKeyWords,TokenTypes.keyword, TermTypes.keywordConstant.ToStringByDescription());
                    //token was handled, remove it
                    JackParser.RemoveFirstToken(tokensDoc);
                    break;
                case TermTypes.varName:
                    bool isVarName = JackParser.IsVariableName(text);
                    if (!isVarName) { throw new Exception("Got bad variable name"); }

                    /*term root*/
                    rootTermToken = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.term.ToStringByDescription(), String.Empty);
                    //adding the variable declaration node
                    parentNode.AppendChild(rootTermToken);


                    JackParser.AddToken(rootTermToken, termToken, TokenTypes.identifier, null);
                    //token was handled, remove it
                    JackParser.RemoveFirstToken(tokensDoc);
                    break;
                case TermTypes.Array:

                    /*term root*/
                    rootTermToken = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.term.ToStringByDescription(), String.Empty);
                    //adding the variable declaration node
                    parentNode.AppendChild(rootTermToken);

                    bool isVariableName = JackParser.IsVariableName(text);
                    if (!isVariableName) { throw new Exception("Got bad variable name for array"); }
                    JackParser.AddToken(rootTermToken, termToken, TokenTypes.identifier, null);
                    //token was handled, remove it
                    JackParser.RemoveFirstToken(tokensDoc);
                    JackParser.GetExpressionInIndexerBrackets(rootTermToken, tokensDoc);
                    break;
                case TermTypes.subRoutineCall:
                    /*term root*/
                    rootTermToken = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.term.ToStringByDescription(), String.Empty);
                    //adding the variable declaration node
                    parentNode.AppendChild(rootTermToken);

                    JackParser.AppendSubroutineCall(rootTermToken, tokensDoc);
                    break;
                case TermTypes.ExpressionInBrackets:
                    /*term root*/
                    rootTermToken = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.term.ToStringByDescription(), String.Empty);
                    //adding the variable declaration node
                    parentNode.AppendChild(rootTermToken);

                    JackParser.AppendExpressionInBrackets(rootTermToken, tokensDoc,false);
                    break;
                case TermTypes.UnaryOpAndTerm:
                    /*term root*/
                    rootTermToken = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.term.ToStringByDescription(), String.Empty);
                    //adding the variable declaration node
                    parentNode.AppendChild(rootTermToken);

                    JackParser.AppendUaryOpAndTerm(rootTermToken, tokensDoc);
                    break;
                default:
                    throw new Exception("Unknown term type: " + ttype.ToStringByDescription());
            }
        }


        /// <summary>
        /// Gets the parameter list and adds it to <see cref="parentNode"/> .
        /// <remarks>
        /// Nodes that were added to parentNode will be removed from tokensDoc
        /// </remarks>
        /// </summary>
        /// <param name="rootSubDec">The root sub dec.</param>
        /// <param name="tokensDoc">The tokens doc.</param>
        private static int GetParameterList(XmlNode parentNode, XmlDocument tokensDoc)
        {
            int paramCount = 0;
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
                return 0 ;
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

                    paramCount++;
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
            return paramCount++; 
        }

        /// <summary>
        /// Gets the list of valid type names.
        /// </summary>
        /// <param name="classNameCandidate">The class name candidate.</param>
        /// <returns>list of all static typs a node might have + <see cref="classNameCandidate"/> if it is a valid className</returns>
        private static List<string> GetValidTypeName(string classNameCandidate)
        {
            bool isClassName = JackParser.IsClassName(classNameCandidate);
            List<string> possibleTexts = new List<string>(SymbolClassifications._variablesTypes);
            if (isClassName) { possibleTexts.Add(classNameCandidate.Trim()); }
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
        /// <param name="parentNode">The rootdo node.</param>
        /// <param name="tokensDoc">The tokens doc.</param>
        private static void AppendSubroutineCall(XmlNode parentNode, XmlDocument tokensDoc)
        {
            OutputStructureNodes sRoutineOwner;
            bool isSubroutineCall = JackParser.IsSubroutineCallBegining(tokensDoc, out sRoutineOwner);
            if (isSubroutineCall)
            {
                //create subroutine element
                XmlNode subRoutineNode = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.subroutineCall.ToStringByDescription(), String.Empty);
                //adding the variable declaration node
                parentNode.AppendChild(subRoutineNode);

                if (sRoutineOwner == OutputStructureNodes.className || sRoutineOwner == OutputStructureNodes.varName)
                {
                    XmlNode classOrVarNameNode = JackParser.GetNextToken(tokensDoc);
                    string nextText = JackParser.GetFirstTokenText(tokensDoc);
                    bool isValidName = JackParser.IsIdentifier(nextText);

                    if (!isValidName) { throw new Exception("Invalid name for subroutine caller"); }

                    JackParser.AddToken(subRoutineNode, classOrVarNameNode, TokenTypes.identifier, null);
                    //token was handled, remove it
                    JackParser.RemoveFirstToken(tokensDoc);

                    XmlNode dotToken = JackParser.GetNextToken(tokensDoc);
                    JackParser.AddToken(subRoutineNode, dotToken, ".", TokenTypes.symbol, null);
                    //token was handled, remove it
                    JackParser.RemoveFirstToken(tokensDoc);

                    JackParser.AppendSubroutineAndExpressions(subRoutineNode, tokensDoc);
                }
                else if (sRoutineOwner == OutputStructureNodes.subroutineName)
                {

                    JackParser.AppendSubroutineAndExpressions(subRoutineNode, tokensDoc);
                }
                else
                {
                    throw new Exception("Unknown format for subroutin");
                }
            }
        }

        /// <summary>
        /// Appends the subroutine and expressions=> subroutineName'('expressionList')' 
        /// </summary>
        /// <param name="subroutineCallRootNode">The rootdo node.</param>
        /// <param name="tokensDoc">The tokens doc.</param>
        private static void AppendSubroutineAndExpressions(XmlNode subroutineCallRootNode, XmlDocument tokensDoc)
        {
            


            /*Validating subroutine name*/
            string subRoutineName = JackParser.GetFirstTokenText(tokensDoc);
            bool isSubroutineName = JackParser.IsSubRoutineName(subRoutineName);
            if (!isSubroutineName) { throw new Exception(String.Format("{0} is not a valid subroutine name", subRoutineName)); }
            /*subroutine name ------------------------------------------------------------------------*/
            XmlNode subRoutinNameToken = JackParser.GetNextToken(tokensDoc);

            JackParser.AddToken(subroutineCallRootNode, subRoutinNameToken, TokenTypes.identifier, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            JackParser.AppendExpressionInBrackets(subroutineCallRootNode, tokensDoc, true);

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
                    JackParser.AppendLetStatement(rootStatement, tokensDoc);
                    VMCodeWriter.AddLetStatement(rootStatement);
                    break;
                case "if":
                    JackParser.AppendIfStatement(rootStatement, tokensDoc);
                    VMCodeWriter.AddIfStatement(rootStatement);
                    break;
                case "while":
                    JackParser.AppendWhileStatement(rootStatement, tokensDoc);
                    VMCodeWriter.AddWhileStatement(rootStatement);
                    break;
                case "do":
                    JackParser.AppendDoStatement(rootStatement, tokensDoc);
                    VMCodeWriter.AddDoStatement(rootStatement);
                    break;
                case "return":
                    JackParser.AppendReturnStatement(rootStatement, tokensDoc);
                    VMCodeWriter.AddReturnStatement(rootStatement);
                    break;
                default:
                    throw new Exception(String.Format("Unknown statement type ({0})", firstStatementNodeString));
            }
            
        }

        private static void AppendReturnStatement(XmlNode parentNode, XmlDocument tokensDoc)
        {
            //create Statements root element
            XmlNode rootReturnNode = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.returnStatement.ToStringByDescription(), String.Empty);
            //adding the variable declaration node
            parentNode.AppendChild(rootReturnNode);

            /*return value*/
            XmlNode retNode = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootReturnNode, retNode, "return", TokenTypes.keyword, null);
            JackParser.RemoveFirstToken(tokensDoc);



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
            XmlNode rootdoNode = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.doStatement.ToStringByDescription(), String.Empty);
            //adding the variable declaration node
            parentNode.AppendChild(rootdoNode);

            XmlNode doToken = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootdoNode, doToken, "do", TokenTypes.keyword, null);
            JackParser.RemoveFirstToken(tokensDoc);

            JackParser.AppendSubroutineCall(rootdoNode, tokensDoc);


            /*;*/
            XmlNode semiCommaToken = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootdoNode, semiCommaToken, ";", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
        }




        private static void AppendWhileStatement(XmlNode parentNode, XmlDocument tokensDoc)
        {
            //create Statements root element
            XmlNode rootWhileNode = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.whileStatement.ToStringByDescription(), String.Empty);
            //adding the variable declaration node
            parentNode.AppendChild(rootWhileNode);

            /*while*/
            XmlNode whileToken = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootWhileNode, whileToken, "while", TokenTypes.keyword, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
            //'(' Expression ')'
            JackParser.AppendExpressionInBrackets(rootWhileNode, tokensDoc, false);

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
            JackParser.AppendExpressionInBrackets(rootIfNode, tokensDoc, false);

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
            XmlNode rootLetNode = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, OutputStructureNodes.letStatement.ToStringByDescription(), String.Empty);
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

            string nextText = JackParser.GetFirstTokenText(tokensDoc);
            if (nextText == "[")
            {
                JackParser.GetExpressionInIndexerBrackets(rootLetNode, tokensDoc);
            }

            /*=*/
            XmlNode equalityToken = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootLetNode, equalityToken, "=", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            JackParser.AppendExpression(rootLetNode, tokensDoc);


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

            if (JackParser.IsNextNodeTerm_Array(tokensDoc)) { return TermTypes.Array; }//should be before var name
            if (JackParser.IsNextNodeTerm_integerConstant(tokensDoc)) { return TermTypes.integerConstant; }
            if (JackParser.IsNextNodeTerm_stringConstant(tokensDoc)) { return TermTypes.stringConstant; }
            if (JackParser.IsNextNodeTerm_keywordConstant(tokensDoc)) { return TermTypes.keywordConstant; }
            if (JackParser.IsNextNodeTerm_subRoutineCall(tokensDoc)) { return TermTypes.subRoutineCall; }//should be before var name
            if (JackParser.IsNextNodeTerm_varName(tokensDoc)) { return TermTypes.varName; }            
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
            for (int i = 0; i < index && i >= 0 && retNode != null; i++)
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
        private static void AddToken(XmlNode parentNode, XmlNode token, Enum expectedNodeType, string encapsulatingTagText)
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
        private static void AddToken(XmlNode parentNode, XmlNode token, Enum expectedNodeInnerText, Enum expectedNodeType, string encapsulatingTagText)
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
        private static void AddToken(XmlNode parentNode, XmlNode token, string expectedText, Enum expectedNodeType, string encapsulatingTagText)
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
        private static void AddToken(XmlNode parentNode, XmlNode token, IEnumerable<string> possibleText, Enum expectedNodeType, string encapsulatingTagText)
        {
            JackParser.AddToken(parentNode, token, possibleText, new List<Enum> { expectedNodeType }, encapsulatingTagText);
        }

        /// <summary>
        /// Adds the class token to class root.
        /// </summary>
        /// <param name="parentNode">The node to add the token to.</param>
        /// <param name="token">The token node to be added.</param>
        /// <param name="expectedNodeInnerText">The expected node inner text.</param>
        /// <param name="expectedNodeType">Expected type of the node.</param>
        private static void AddToken(XmlNode parentNode, XmlNode token, IEnumerable<string> possibleText, IEnumerable<Enum> expectedNodeTypes, string encapsulatingTagText)
        {
            #region validations
            bool throwExp = true;
            JackParser.ValidateToken(token, possibleText, expectedNodeTypes, throwExp);
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
        private static bool AssertNodeType(XmlNode node, Enum expectedType, string errorMessage, bool throwException)
        {
            return JackParser.AssertNodeType(node, new List<Enum> { expectedType }, errorMessage, throwException);
        }
        /// <summary>
        /// Asserts the specified expression.
        /// </summary>
        /// <param name="expression">if is <c>false</c> will throw an exception.</param>
        /// <param name="errorMessage">The error message in exception, if thrown.</param>
        /// <exception cref="System.Exception">Exception with epecified message</exception>
        private static bool AssertNodeType(XmlNode node, IEnumerable<Enum> expectedType, string errorMessage, bool throwException)
        {
            bool isMatchesAny = expectedType.Any(et => String.Equals(node.Name.Trim(), et.ToStringByDescription(), StringComparison.OrdinalIgnoreCase));
            if (throwException && !isMatchesAny)
            {
                throw new Exception(errorMessage);
            }
            return isMatchesAny;
        }


        private static bool ValidateToken(XmlNode token, IEnumerable<string> possibleText, Enum expectedNodeType, bool throwException)
        {
            return JackParser.ValidateToken(token, possibleText, new List<Enum> { expectedNodeType }, throwException);
        }
        private static bool ValidateToken(XmlNode token, IEnumerable<string> possibleText, IEnumerable<Enum> expectedNodeTypes, bool throwException)
        {
            bool valid = true;
            /*is text valid?*/
            List<string> cleanPossibleText = possibleText.Where(s => !String.IsNullOrWhiteSpace(s)).ToList();
            if (possibleText != null && cleanPossibleText.Count() > 0)
            {
                string errorMsg = String.Format("Expected node to have inner text of: '{0}' but found '{1}'", String.Join("|", cleanPossibleText), token.InnerText);
                valid &= JackParser.AssertNodeText(token, cleanPossibleText, errorMsg, throwException);

            }


            object[] args = expectedNodeTypes.Select(t => t.ToStringByDescription()).ToArray();
            string msg = String.Format("Expected node to be of type: {0}", String.Join(" | ", args));
            valid &= JackParser.AssertNodeType(token, expectedNodeTypes, msg, throwException);
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
            isLegalIdentifier &= !SymbolClassifications._allKeyWords.Contains(cleanCandidate);

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
            return SymbolClassifications._oparations.Contains(firstStatementNodeString);
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
            return SymbolClassifications._statementsHeaders.Contains(firstStatementNodeString);
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

                isVarDecBegining = tType == TokenTypes.keyword 
                    && (SymbolClassifications._classVariablesModifiers.Contains(text) || SymbolClassifications._variablesModifiers.Contains(text));
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

                issubRoutinBegining = tType == TokenTypes.keyword && SymbolClassifications._subRoutineModifiers.Contains(text);
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
            return SymbolClassifications._unariOparations.Contains(text);
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
            bool isStandAloneCall = isVarName && hasOpeningBracket;

            bool hasDot = nextString == ".";
            string next2String = JackParser.GetTokenTextAtIndex(tokensDoc, 3);
            bool isObjectWithSubroutines = JackParser.IsClassName(next2String) || JackParser.IsVariableName(next2String);
            string next3String = JackParser.GetTokenTextAtIndex(tokensDoc, 4);
            hasOpeningBracket = next3String == "(";
            bool isObjectsCall = hasDot && isObjectWithSubroutines && hasOpeningBracket;

            return isStandAloneCall || isObjectsCall;

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

        private static bool IsNextNodeTerm_keywordConstant(XmlDocument tokensDoc)
        {
            string text = JackParser.GetFirstTokenText(tokensDoc);
            return SymbolClassifications._constantKeyWords.Contains(text);
        }

        private static bool IsNextNodeTerm_stringConstant(XmlDocument tokensDoc)
        {
            XmlNode node = JackParser.GetNextToken(tokensDoc);
            if (node == null)
            {
                return false;
            }
            bool isStringConst = true;
            bool allValidChars = false;
            bool isMarkedStrConstant = node.Name == "stringConstant";
            string text = JackParser.GetFirstTokenText(tokensDoc);
            try
            {                
                char[] disallowdChars = new char[] { '\n', '\"' };
                allValidChars = text.IndexOfAny(disallowdChars, 1, text.Length - 1) < 0;
            }
            catch (Exception)
            {

                isStringConst = false;
            }
            isStringConst &= allValidChars && isMarkedStrConstant;
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
            [Description("returnStatement")]
            returnStatement,
            [Description("subroutineCall")]
            subroutineCall,
            [Description("term")]
            term,
            [Description("expression")]
            expression,
            [Description("expressionList")]
            expressionList,
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
            keywordConstant,
            varName,
            Array,
            subRoutineCall,
            ExpressionInBrackets,
            UnaryOpAndTerm
        }
        #endregion
    }
}
