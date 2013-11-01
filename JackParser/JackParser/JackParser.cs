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
        static ReadOnlyCollection<string> _allKeyWords;
        static ReadOnlyCollection<string> _generalKeyWords = new ReadOnlyCollection<string>(
            new List<String> { "class", "var", "void", "true", "false", "null", "this", "let", "do", "if", "else", "while", "return" });

        static ReadOnlyCollection<string> _variablesModifiers = new ReadOnlyCollection<string>(
            new List<String> { "static", "field" });

        static ReadOnlyCollection<string> _variablesTypes = new ReadOnlyCollection<string>(
            new List<String> { "int", "char", "boolean" });

        static ReadOnlyCollection<string> _subRoutineReturnType;

        static ReadOnlyCollection<string> _subRoutineModifiers = new ReadOnlyCollection<string>(
            new List<String> { "constructor", "function", "method" });

        static ReadOnlyCollection<string> _symbols =
            new ReadOnlyCollection<string>(new List<String> { "[", "]", "{", "}", "(", ")", 
                                        ".",",",";","+","-","*","/","&","|","<",">","=", "~"});

        static JackParser()
        {
            var srRetTypes = new List<string>(JackParser._variablesTypes);
            srRetTypes.Add("void");

            List<string> allKeyWords = new List<string>();

            JackParser._subRoutineReturnType = new ReadOnlyCollection<string>(srRetTypes);
            allKeyWords.AddRange(JackParser._generalKeyWords);
            allKeyWords.AddRange(JackParser._variablesModifiers);
            allKeyWords.AddRange(JackParser._subRoutineReturnType);
            allKeyWords.AddRange(JackParser._variablesTypes.Distinct());

            allKeyWords.AddRange(JackParser._subRoutineModifiers);
            JackParser._allKeyWords = new ReadOnlyCollection<string>(allKeyWords);
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
        /// Adds the root class (and content) to the <see cref="doc"/> argument.
        /// </summary>
        /// <param name="tokensDoc">The tokens document.</param>
        /// <param name="xml">The XML for adding class element to.</param>
        private static void GetClass(XmlDocument tokensDoc, out XmlDocument xml)
        {
            xml = new XmlDocument();
            //create class root element
            XmlNode classRoot = xml.CreateNode(XmlNodeType.Element, ProgramStructureNodes.@class.ToString(), String.Empty);
            //adding the class root node
            xml.AppendChild(classRoot);

            /*Class token*/
            XmlNode tClass = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(classRoot, tClass, ProgramStructureNodes.@class, TokenTypes.keyword, null);
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
                TokenTypes tType = GetFirstTokenType(tokensDoc);
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
                TokenTypes tType = GetFirstTokenType(tokensDoc);
                string text = JackParser.GetFirstTokenText(tokensDoc);

                issubRoutinBegining = tType == TokenTypes.keyword && JackParser._subRoutineModifiers.Contains(text);
            }
            catch (Exception)
            {
                issubRoutinBegining = false;
            }


            return issubRoutinBegining;

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
            XmlNode rootVarDec = xml.CreateNode(XmlNodeType.Element, ProgramStructureNodes.classVarDec.ToStringByDescription(), String.Empty);
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

            JackParser.AddToken(rootVarDec, varTypeToken, possibleTexts, TokenTypes.keyword, ProgramStructureNodes.type.ToStringByDescription());
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
            XmlNode rootSubDec = xml.CreateNode(XmlNodeType.Element, ProgramStructureNodes.subroutineDec.ToStringByDescription(), String.Empty);
            //adding the variable declaration node
            classRoot.AppendChild(rootSubDec);

            /*constructor | function | method------------------------------------------------------------------------*/
            XmlNode tFuncType = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootSubDec, tFuncType, JackParser._subRoutineModifiers, TokenTypes.keyword, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            /*function return type------------------------------------------------------------------------*/
            XmlNode retTypeToken = JackParser.GetNextToken(tokensDoc);

            List<string> possibleTexts = JackParser.GetValidTypeName(retTypeToken.InnerText);

            JackParser.AddToken(rootSubDec, retTypeToken, possibleTexts, TokenTypes.identifier, ProgramStructureNodes.type.ToStringByDescription());
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
            /*function name------------------------------------------------------------------------*/
            
            XmlNode varfuncNameToken = JackParser.GetNextToken(tokensDoc);
            bool isvalidName = JackParser.IsSubRoutineName(varfuncNameToken.InnerText);
            /*Not valid name; quit*/
            if (!isvalidName) { throw new Exception(String.Format("sub-routine name {0} is not valid", varfuncNameToken.InnerText)); }

            JackParser.AddToken(rootSubDec, varfuncNameToken, String.Empty, TokenTypes.identifier, ProgramStructureNodes.subroutineName.ToStringByDescription());
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
            
            /*{------------------------------------------------------------------------*/
            XmlNode openBodyBracket = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootSubDec, openBodyBracket, "{", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            JackParser.GetParameterList(rootSubDec, tokensDoc);

            /*}------------------------------------------------------------------------*/
            XmlNode closeBodyBracket = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(rootSubDec, closeBodyBracket, "}", TokenTypes.symbol, null);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
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
            XmlNode rootPramList = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, ProgramStructureNodes.parameterList.ToStringByDescription(), String.Empty);
            //adding the variable declaration node
            parentNode.AppendChild(rootPramList);


            //check if there are any parameters:
            XmlNode nFirst = JackParser.GetNextToken(tokensDoc);
            XmlNode nSecond = nFirst.NextSibling;

            List<string> validTexts = JackParser.GetValidTypeName(nFirst.InnerText);
            //is it a type?
            bool hasParams = JackParser.ValidateToken(nFirst, validTexts, TokenTypes.keyword,false);
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
                    JackParser.AddToken(rootPramList, varTypeToken, possibleTexts, TokenTypes.keyword, ProgramStructureNodes.type.ToStringByDescription());
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
        /// Gets the type of the first token.
        /// </summary>
        /// <param name="tokensDoc">The tokens doc.</param>
        /// <returns></returns>
        private static TokenTypes GetFirstTokenType(XmlDocument tokensDoc)
        {
            string name = JackParser.GetNextToken(tokensDoc).Name;
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
            string text = JackParser.GetNextToken(tokensDoc).InnerText;
            return text.Trim();
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
        private static void AddToken(XmlNode parentNode, XmlNode token, ProgramStructureNodes expectedNodeInnerText, TokenTypes expectedNodeType, string encapsulatingTagText)
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

        private static bool ValidateToken(XmlNode token, IEnumerable<string> possibleText, TokenTypes expectedNodeType ,bool throwException)
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
            return AssertNodeType(node, new List<TokenTypes> { expectedType }, errorMessage,throwException);
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

        /// <summary>
        /// Tokens that represents high level noes in program structure
        /// </summary>
        private enum ProgramStructureNodes
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
            other,

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
    }
}
