using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace JackParser
{
    public static class JackParser
    {

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
            JackParser.AddToken(classRoot, tClass, ProgramStructureNodes.@class, TokenTypes.keyword);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
            
            /*Class Name*/
            XmlNode tClassName = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(classRoot, tClassName, TokenTypes.identifier);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);

            /*{*/
            XmlNode tOpenBracket = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(classRoot, tOpenBracket,"{", TokenTypes.symbol);
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
            //create class root element
            XmlNode classVarDec = classRoot.OwnerDocument.CreateNode(XmlNodeType.Element, ProgramStructureNodes.classVarDec.ToString(), String.Empty);
            //adding the class root node
            xml.AppendChild(classRoot);
            while (GetFirstTokenType(tokensDoc) == ProgramStructureNodes.classVarDec)
            {
                JackParser.AppendVariableDeclaration(classRoot, tokensDoc);
            }

            
            
        }

        private static void AppendVariableDeclaration(XmlNode classRoot, XmlDocument tokensDoc)
        {
            XmlNode nextToken = JackParser.GetNextToken(tokensDoc);
            JackParser.AddToken(classRoot, nextToken, TokenTypes.identifier);
            //token was handled, remove it
            JackParser.RemoveFirstToken(tokensDoc);
        }

        private static ProgramStructureNodes GetFirstTokenType(XmlDocument tokensDoc)
        {
            string name = JackParser.GetNextToken(tokensDoc).Name;
            ProgramStructureNodes nodeType;
            if (!Enum.TryParse<ProgramStructureNodes>(name, out nodeType))
            {
                nodeType = ProgramStructureNodes.other;
            }
            return nodeType;
        }

        /// <summary>
        /// Appends the sub routines declarations nodes to the specified class root node.
        /// </summary>
        /// <param name="classRoot">The class root node.</param>
        /// <param name="tokensDoc">The tokens doc to take nodes from.</param>
        private static void AppendSubRoutinesDeclarations(XmlNode classRoot, XmlDocument tokensDoc)
        {
            throw new NotImplementedException();
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
            return (tokensDoc != null && tokensDoc.FirstChild != null)? tokensDoc.FirstChild.FirstChild: null;
        }

        /// <summary>
        /// Adds the class token to class root.
        /// </summary>
        /// <param name="parentNode">The node to add the token to.</param>
        /// <param name="token">The token node to be added.</param>
        /// <param name="expectedNodeInnerText">The expected node inner text.</param>
        /// <param name="expectedNodeType">Expected type of the node.</param>
        private static void AddToken(XmlNode parentNode, XmlNode token, TokenTypes expectedNodeType)
        {
            JackParser.AddToken(parentNode, token, String.Empty, expectedNodeType);
        }

        /// <summary>
        /// Adds the class token to class root.
        /// </summary>
        /// <param name="parentNode">The node to add the token to.</param>
        /// <param name="token">The token node to be added.</param>
        /// <param name="expectedNodeInnerText">The expected node inner text.</param>
        /// <param name="expectedNodeType">Expected type of the node.</param>
        private static void AddToken(XmlNode parentNode, XmlNode token, ProgramStructureNodes expectedNodeInnerText, TokenTypes expectedNodeType)
        {
            JackParser.AddToken(parentNode, token, expectedNodeInnerText.ToString(), expectedNodeType);
        }
        /// <summary>
        /// Adds the class token to class root.
        /// </summary>
        /// <param name="parentNode">The node to add the token to.</param>
        /// <param name="token">The token node to be added.</param>
        /// <param name="expectedNodeInnerText">The expected node inner text.</param>
        /// <param name="expectedNodeType">Expected type of the node.</param>
        private static void AddToken(XmlNode parentNode, XmlNode token, string expectedText, TokenTypes expectedNodeType)
        {
            if (!String.IsNullOrWhiteSpace(expectedText))
            {
                string errorMsg = String.Format("Expected node to have inner text of: {0}", expectedText);
                JackParser.AssertNodeText(token, expectedText, errorMsg);
            }

            string msg = String.Format("Expected node to be of type: {0}", expectedNodeType);
            JackParser.AssertNodeType(token, expectedNodeType,msg);

            
            //adding class token
            XmlNode importedToken = parentNode.OwnerDocument.ImportNode(token, true);
            parentNode.AppendChild(importedToken);
            
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
        private static void AssertNodeText(XmlNode node, string expectedText, string errorMessage)
        {
            if (!String.Equals(node.InnerText.Trim(), expectedText, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception(errorMessage);
            }
        }

        /// <summary>
        /// Asserts the specified expression.
        /// </summary>
        /// <param name="expression">if is <c>false</c> will throw an exception.</param>
        /// <param name="errorMessage">The error message in exception, if thrown.</param>
        /// <exception cref="System.Exception">Exception with epecified message</exception>
        private static void AssertNodeType(XmlNode node, TokenTypes expectedType, string errorMessage)
        {
            if (!String.Equals(node.Name.Trim(), expectedType.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception(errorMessage);
            }
        }

        /// <summary>
        /// Tokens that represents high level noes in program structure
        /// </summary>
        private enum ProgramStructureNodes
        {
            other,
            @class,
            classVarDec,
            @type,
            subRoutineDec,
            parameterList,
            subRoutineBody,
            varDec,
            className,
            subRoutineName,
            varName,
        }

        /// <summary>
        /// Types that token XML nodes might have
        /// </summary>
        private enum TokenTypes
        {
            keyword,
            identifier,
            symbol

        }

        /// <summary>
        /// Gets the jack XML string from tokens.
        /// </summary>
        /// <param name="tokens">The tokens to extract XML from.</param>
        /// <returns>the content string of Jack XML file</returns>
        internal static string GetJackXmlStringFromTokens(XmlDocument tokens)
        {
            XmlDocument xmlOutput =  JackParser.GetJackXmlFromTokens(tokens);
            return (xmlOutput ?? new XmlDocument()).ToXmlString();
        }
    }
}
