using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

namespace Ex5
{
    class Translator
    {
        private string[] mathematicCommands = { "add", "sub", "neg", "eq", "gt", "lt", "and", "or", "not", "mult2" };
        private string[] memoryCommands = { "pop", "push" };
        private string[] segmentCommands = { "constant", "nothing" };

        private string[] flowControlCommands = { "label", "goto", "if-goto" };   //Stas: New code flow control
        internal static string currentlyParsedFileName = "";  //Stas: New code flow control

        public Translator(string fileName)
        {
            string initCode = String.Empty;
            string cleanFileName = Path.GetFileNameWithoutExtension(fileName);
            if (Program.WriteInitCode)
            {
                
                initCode += this.PrintInitCode();
            }

            //detect whether its a directory or file
            FileAttributes attr = File.GetAttributes(fileName);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                

                string allOutputs = String.Empty;
                //Directory
                string[] files = Directory.GetFiles(fileName, "*.vm", SearchOption.TopDirectoryOnly);
                foreach (string file in files)
                {
                    string currentFileName = Path.GetFileNameWithoutExtension(file);
                    string rawVMInput = openFile2String(file);
                    string traslatedOutput = TranslateVM2Asm(rawVMInput, currentFileName);
                    allOutputs += traslatedOutput + Environment.NewLine;
                    
                }
                string newFileName = Path.Combine(fileName, cleanFileName + ".asm");

                
                //allOutputs = allOutputs.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);
                string[] lines = allOutputs.Split(new string[]{Environment.NewLine},StringSplitOptions.RemoveEmptyEntries).ToArray();
                
                //string[] probLine = lines.Where(l => l.Trim().EndsWith("_")).ToArray();
                //lines = lines.Select(l => l.Trim().EndsWith("_") ? l.Trim() + "aaaa" : l.Trim()).ToArray();

                allOutputs = String.Join(Environment.NewLine, lines);
                string tofile = initCode + allOutputs;
                writeString2File(newFileName, tofile);
              
            }
            else
            {
                //File
                cleanFileName = Path.GetFileNameWithoutExtension(fileName);
                string rawVMInput = openFile2String(fileName);
                string traslatedOutput = initCode+ TranslateVM2Asm(rawVMInput,cleanFileName);
                string newFileName = fileName.Split('.')[0] + ".asm";
                writeString2File(newFileName, traslatedOutput);

            }               

        }

        private string TranslateVM2Asm(string VMString, string fileName)
        {
            Translator.currentlyParsedFileName = fileName;
            VMString = VMString ?? String.Empty;
            string result = "";
           
            if (!String.IsNullOrWhiteSpace(VMString))
            {
                // STEP 1: Strip comments
                result = stripComments(VMString);

                // STEP 2: Translate Lines
                result = translateLines(result);
            }
            return result;
        }

        private string PrintInitCode()
        {
            return Printer.PrintInitCode();
        } // string TranslateVM2Asm(string VMString)


        private string translateLines(string inputText)
        {
            if (inputText == "") return "";
            string result = "";
            string[] lines = GetLines(inputText);
            foreach (string line in lines)
            {
                result += translateSingleLine(line)+Environment.NewLine;
            }
            return result;
        } //string translateLines(string inputText)


        private string translateSingleLine(string inputLine)
        {
            inputLine = inputLine.Trim();
            if (inputLine == "") return "";
            string result = "";

            string[] words = inputLine.Split(' ');
            if (words.Length == 0) return "";
            string firstWord = words[0].Trim();
            if (firstWord == "") return "";

            if (mathematicCommands.Contains(firstWord))
            {
                result += Printer.printMathCommand(words);                              // MATH COMMAND  //Stas: added this.currentlyParsedFileName
            }
            else if (memoryCommands.Contains(firstWord))
            {
                result += Printer.printMemCommand(words);                               // MEM COMMAND
            }
            else if (String.Equals(firstWord, "function",StringComparison.InvariantCultureIgnoreCase))
            {
                 result += Printer.printFunctionDeclaration(words);
            }
            else if (String.Equals(firstWord, "call", StringComparison.InvariantCultureIgnoreCase))
            {
                result += Printer.printFunctionCall(words);
            }
            else if (String.Equals(firstWord, "return", StringComparison.InvariantCultureIgnoreCase))
            {
                result += Printer.printReturnCommand(words);
            }

            else if (flowControlCommands.Contains(firstWord))   //Stas: New code flow control
            {
                result += Printer.printFlowControlCommand(words);
            }

            else //          <<<------- Add before this line other command types
            {
#if DEBUG
                throw new Exception("Unrecognized first word in line under parsing");
              
#endif
            }

            return result;
        } //string translateSingleLine(string inputLine)


        private string stripComments(string inputText)
        {
            if (inputText == "") return "";
            string result = "";
            string[] lines = GetLines(inputText);
            foreach (string line in lines)
            {
                string[] words = line.Split(' ');
                foreach (string word in words)
                {
                    if (word.Equals(@"//")) { break; }
                    result += word;
                    result += " ";
                }
                result += Environment.NewLine;
            }
            return result;
        } //string stripComments(string inputText)

        private static string[] GetLines(string inputText)
        {
            string[] lines = Regex.Split(inputText, @"\r\n|\r|\n");
            lines = lines.Where(s => !String.IsNullOrWhiteSpace(s)).Select(l => l.Trim()).ToArray();
            return lines;
        }

        private string openFile2String(string fileName)
        {
            string result = "";
            result = System.IO.File.ReadAllText(fileName);
            return result;
        } //string openFile2String(string fileName)

        public void writeString2File(string newFileName, string traslatedOutput)
        {
            System.IO.File.WriteAllText(newFileName, traslatedOutput);
        }
    }
}
