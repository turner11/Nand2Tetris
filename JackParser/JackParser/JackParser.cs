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
        /// Gets the jack XML string from tokens.
        /// </summary>
        /// <param name="tokens">The tokens to extract XML from.</param>
        /// <returns>the content string of Jack XML file</returns>
        internal static string GetJackXmlStringFromTokens(XmlDocument tokens)
        {
            XmlDocument retXml = JackParser.GetJackXmlFromTokens(tokens);
           //converting xml to string
            using (var stringWriter = new StringWriter())
            using (var xmlTextWriter = XmlWriter.Create(stringWriter))
            {
                retXml.WriteTo(xmlTextWriter);
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
            XmlDocument xml = new XmlDocument();
            JackParser.GetClass(tokensDoc, ref xml);
            
            XmlNodeList tokens = tokensDoc.FirstChild.ChildNodes;
            for (int i = 0; i < tokens.Count; i++)
            {
                XmlNode currNode = tokens[i];
                string nodeType = currNode.Name;
                string nodeValue = currNode.InnerText.Trim() ;
                
            }
            
            return xml;
            
        }

        /// <summary>
        /// Adds the root class (and content) to the <see cref="doc"/> argument.
        /// </summary>
        /// <param name="tokensDoc">The tokens document.</param>
        /// <param name="xml">The XML for adding class element to.</param>
        private static void GetClass(XmlDocument tokensDoc, ref XmlDocument xml)
        {
            XmlNode tClassNode = tokensDoc.FirstChild.FirstChild;
            JackParser.Assert(tClassNode.InnerText.Trim() == ProgramStructureNodes.@class.ToString(), "Top element must be class");
            XmlElement classNode = xml.CreateElement("class");
            xml.AppendChild(classNode);

        }

        /// <summary>
        /// Asserts the specified expression.
        /// </summary>
        /// <param name="expression">if is <c>false</c> will throw an exception.</param>
        /// <param name="errorMessage">The error message in exception, if thrown.</param>
        /// <exception cref="System.Exception">Exception with epecified message</exception>
        private static void Assert(bool expression, string errorMessage)
        {
            if (!expression)
            {
                throw new Exception(errorMessage);
            }
        }

        /// <summary>
        /// Tokens that represents high level noes in program structure
        /// </summary>
        private enum ProgramStructureNodes
        {
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
    }
}
