using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;

namespace Ex2
{
    class Tokenizer
    {
        private string workingDir;
        

        public Tokenizer(string workingDir)
        {
            this.workingDir = workingDir;
        }

        public List<Exception> start(out List<Tuple<string, XmlDocument>> tokenFiles)
        {
            var jackFiles = Directory.GetFiles(workingDir, "*.jack");
            tokenFiles = new List<Tuple<string, XmlDocument>>();

            List<Exception> exList = new List<Exception>();
            foreach (string fileName in jackFiles)
            {
                try
                {
                    string str = File.ReadAllText(fileName);
                    str = removeComments(str);
                    str = recursiveTokenize(str);
                    str = "<tokens>" + Environment.NewLine + str + Environment.NewLine + "</tokens>";

                    XmlDocument tokens = new XmlDocument();
                    tokens.LoadXml(str);
                    Tuple<string,XmlDocument> tple= new Tuple<string,XmlDocument>(fileName,tokens);
                    tokenFiles.Add(tple);
                    /*string[] tmp = fileName.Split('.');
                    string newFileName = tmp[0] + "T.xml";
                    System.IO.File.WriteAllText(newFileName, str);*/
                }
                catch (Exception ex)
                {

                    Exception e = new Exception("Failed to create tokens file: " + fileName, ex);
                    exList.Add(e);
                    continue;
                }
                
            }
            return exList;
        }

        public string singleTokenize(string source)
        {
            string output = symbolTokenize(source);
            //Console.WriteLine(output);
            //Console.ReadLine();
            output = keywordTokenize(output);
            output = integerTokenize(output);
            output = trimString(output);
  
            output = identifierTokenize(output);
            output = niceString(output);
            
            return (output);
        }

        public string niceString(string source)
        {
            string output = source;
            int i = 0;
            while (i < output.Length - 1)
            {
                if (output.Substring(i, 2) == "</")
                {
                    while (output[i] != '>')
                    {
                        i++;
                    }
                    i++;
                    if (i >= output.Length)
                    {
                        return output;
                    }
                    string temp = output;
                    output = temp.Substring(0, i);
                    output += Environment.NewLine;
                    output += temp.Substring(i);
                }
                i++;
            }
            return output;
        }

        public string identifierTokenize(string source)
        {
            string output = source;
            int i = 0;
            while (i < output.Length - 1)
            {
                if (output.Substring(i, 2) == "</")
                {
                    while (output[i] != '>')
                    {
                        i++;
                    }
                    i++;
                    if (i >= output.Length)
                    {
                        return output;
                    }
                    while ((output[i] == ' ' || output[i] == '\r' || output[i] == '\n') && i < output.Length)
                    {
                        i++;
                    }
                    if (output[i] != '<')
                    {
                        int j = i + 1;
                        while (j < output.Length && output[j] != '<')
                        {
                            j++;
                        }
                        string temp = output;
                        output = temp.Substring(0, i);
                        output += addIdentifierTags(temp.Substring(i, j - i).Trim());
                        output += temp.Substring(j);
                        i = -1;
                    }
                }
                i++;
            }
            return output;

        }

        public string addIdentifierTags(string source)
        {
            string output = "";
            string[] arr = source.Split(' ');
            foreach (string str in arr)
            {
                output += "<identifier>";
                output += str;
                output += "</identifier>";
            }
            return output;
        }
        public string trimString(string source)
        {
            string line;
            string output = "";
            StringReader reader = new StringReader(source);
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                output += line;
            }
            return (output);
        }

        public string integerTokenize(string source)
        {
            string symbolsRegex = @">[\n\r| \t]*[0-9]+";
            
            string output = Regex.Replace(source, symbolsRegex, delegate(Match match)
            {
                string temp = match.ToString();
                string tempRegex = @"[0-9]+";
                temp = Regex.Match(temp, tempRegex).ToString();

                string result = ">" + Environment.NewLine + "<integerConstant>";
                result += temp;
                result += "</integerConstant>" + Environment.NewLine;
                return result;
            });

            return (output);
        }

        public string keywordTokenize(string source)
        {
            string symbolsRegex = @"\b(class|constructor|function|method|field|static|var|int|char|boolean|void|true|false|null|this|let|do|if|else|while|return)\b";


            string output = Regex.Replace(source, symbolsRegex, delegate(Match match)
            {
                string result = "<keyword>";
                result += match.ToString();
                result += "</keyword>" + Environment.NewLine;
                return result;
            });

            return (output);
        }

        public string symbolTokenize(string source)
        {
            string symbolsRegex = @"[\{}\(\)\[\]\.,;\+\-\*/&\|<>=~#]";
           

            string output = Regex.Replace(source, symbolsRegex, delegate(Match match)
            {
                string result = "<symbol>";
                if (match.ToString() == "<")
                {
                    result += "&lt;";
                }
                else if (match.ToString() == ">")
                {
                    result += "&gt;";
                }
                else if (match.ToString() == "&")
                {
                    result += "&amp;";
                }
                else
                {
                    result += match.ToString();
                }
                result += "</symbol>" + Environment.NewLine;
                return result;
            });

            return (output);
        }



        public string recursiveTokenize(string source)
        {
            int firstQuotePos = firstQuotePosition(source);
            if (firstQuotePos == -1)
            {

                return singleTokenize(source);
            }
            else
            {
                int nextQuotePos = nextQuotePosition(source, firstQuotePos);
                if (nextQuotePos == -1)
                {
                    Console.WriteLine("Error occured while looking for string constants. Single quote found");
                    return ("");
                }

                string strContent = Environment.NewLine + "<stringConstant>";
                strContent += source.Substring(firstQuotePos + 1, nextQuotePos - firstQuotePos - 1);
                strContent += "</stringConstant>" + Environment.NewLine;
                string pre = source.Substring(0, firstQuotePos);
                string post = source.Substring(nextQuotePos + 1);
                return recursiveTokenize(pre) + strContent + recursiveTokenize(post);
            } 
        }


        public int firstQuotePosition(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '"')
                {
                    return i;
                }
            }
            return -1;
        }

        public int nextQuotePosition(string str, int pos)
        {
            if (pos + 1 >= str.Length)
            {
                return -1;
            }
            for (int i = pos + 1; i < str.Length; i++)
            {
                if (str[i] == '"')
                {
                    return i;
                }
            }
            return -1;
        }


// ---------------- REMOVE COMMENTS  ---------------------------------------------------------------------------------------------------
        public string removeComments(string source)
        {
            while (true)
            {
                int lineComm = lineCommentStart(source);
                int blockComm = blockCommentStart(source);
                if (lineComm != -1)  //Found commented line
                {
                    if (lineComm < blockComm || blockComm == -1)
                    {
                        source = clearFromPosToLineEnd(source, lineComm);
                    }
                }
                if (blockComm != -1)  //Found comment block
                {
                    if (blockComm < lineComm || lineComm == -1)
                    {
                        source = clearCommentBlock(source, blockComm); 
                    }
                }
                if (lineComm == -1 && blockComm == -1)
                {
                    break;
                }
                
            }

            return source;
        }

        public string clearCommentBlock(string str, int pos)
        {
            int i;
            for (i = pos; i < str.Length - 1; i++)
            {
                string subString = str.Substring(i, 2);
                if (subString == "*/")
                {
                    break;
                }
            }
            string head = str.Substring(0, pos);
            string tail = str.Substring(i + 2);
            return head + tail;
        }

        public string clearFromPosToLineEnd(string str, int pos)
        {
            int i;
            for (i = pos; i < str.Length; i++)
            {
                if (str[i].ToString() == "\n")
                {
                    break;
                }
            }
            string head = str.Substring(0, pos);
            string tail = str.Substring(i + 1);
            return head + tail;
        }

        public int lineCommentStart(string line)
        {
            bool inString = false;
            for (int i = 0; i < line.Length - 1; i++)
            {
                
                if (line[i] == '"')
                {
                    if (inString == false)
                    {
                        inString = true;
                    }
                    else
                    {
                        inString = false;
                    }
                }

                string subString = line.Substring(i, 2);
                if (!inString && subString == "//")
                {
                    return i;
                }
            }

            return -1;
        } 

        public int blockCommentStart(string line)
        {
            bool inString = false;
            for (int i = 0; i < line.Length - 1; i++)
            {
                if (line[i] == '"')
                {
                    if (inString == false)
                    {
                        inString = true;
                    }
                    else
                    {
                        inString = false;
                    }
                }

                string subString = line.Substring(i, 2);
                if (!inString && subString == "/*")
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
