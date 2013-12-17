#if DEBUG
   // #define COMMENT
#endif
#define COMMENT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ex5
{
    public class Printer
    {
        const string FUNCTION_DECLARATION_LABEL_SUFFIX = "_dec";
        //const string FUNCTION_CALL_LABEL_SUFFIX = "_call";
        const string RETURN_ADDRESS_LABEL_SUFFIX = "_return";

        static Dictionary<string, int> _functionCallsByName = new Dictionary<string, int>();
        
        public static int flowCounter = 0;
        public static int returnCounter = 0;

        public static string printMathCommand(string[] words)    //Stas: added currentFileName
        {
            if (words == null) return "";
            if (words.Length == 0) return "";
            if (words.Length > 1)
            {
#if DEBUG
                throw (new Exception("More than 1 word in mathematic command line"));
                
#endif
                return String.Empty ;
            }
            string result = "";
            result += Comment(words);
            string firstWord = words[0];
            switch (firstWord)
            {
                case "add":
                    result += printAddCommand();
                    break;

                case "sub":
                    result += printSubCommand();
                    break;

                case "neg":
                    result += printNegCommand();
                    break;

                case "not":
                    result += printNotCommand();
                    break;

                case "and":
                    result += printAndCommand();
                    break;

                case "or":
                    result += printOrCommand();
                    break;

                case "eq":
                    result += printEqCommand();   //Stas: added currentFileName
                    break;

                case "lt":
                    result += printLtCommand();   //Stas: added currentFileName
                    break;

                case "gt":
                    result += printGtCommand();   //Stas: added currentFileName
                    break;

                case "mult2":
                    result += printMult2Command();
                    break;

                default:
#if DEBUG
                    throw (new Exception("Unrecognized mathematic command"));
#endif
                    break;
            }


            return result;
        } // string printMathCommand(string[] words)

        public static string printPushStoredLocationCommand(string location)
        {
            Regex reg = new Regex("@+");
            location = reg.Replace(location, "@");
            
            return printPushCommand("push constant " + location);
        }

       

        public static string printMemCommand(string[] words)
        {
           
            if (words == null) return "";
            if (words.Length == 0) return "";
            string result = "";
            result += Comment(words);

            string firstWord = words[0];
            switch (firstWord)
            {
                case "push":
                    result += printPushCommand(words);
                    break;

                case "pop":
                    result += printPopCommand(words);
                    break;

                default:
                    throw (new Exception("Unrecognized memory command"));
            }
            return result;
        }


        private static string Comment(string line)
        {
#if COMMENT
            string str = "//" + line + Environment.NewLine;
            return str;
#endif
            return String.Empty;
        }
        private static string  Comment(string[] words)
        {


            string line = String.Join(" ", words);
            return Comment(line);

        } // string printMemCommand(string[] words)

        public static string printPushCommand(string line)
        {
            string[] words = line.Split(new char[] { ' ' }) ;
            return printPushCommand(words);
        }
        public static string printPushCommand(string[] words)
        {
            if (words == null) return "";
            if (words.Length == 0) return "";
            string result = "";

            string segmentName = words[1].ToLower();
            string value = words[2].Replace("@",String.Empty);
            string currentFileName = Translator.currentlyParsedFileName;

            switch (segmentName)
            {
                case "label":
                    result += @"@" + value + Environment.NewLine;
                    result += @"D = A" + Environment.NewLine;
                    result += @"@SP" + Environment.NewLine;
                    result += @"A= M" + Environment.NewLine;
                    result += @"M = D" + Environment.NewLine;
                    result += @"@SP" + Environment.NewLine;
                    result += @"M = M+1" + Environment.NewLine;
                    break;
                case "constant":
                    int temp;
                    bool isRegname = int.TryParse(value, out temp);
                    string valueLocation = isRegname ? "A" : "M";
                    result += @"@" + value + Environment.NewLine;
                    result += @"D = " + valueLocation + Environment.NewLine;
                    result += @"@SP" + Environment.NewLine;
                    result += @"A= M" + Environment.NewLine;
                    result += @"M = D" + Environment.NewLine;
                    result += @"@SP" + Environment.NewLine;
                    result += @"M = M+1" + Environment.NewLine;
                    break;

                case "static":
                    //result += @"@" + value + Environment.NewLine;
                    //result += @"D = A" + Environment.NewLine;
                    //result += @"@16" + Environment.NewLine;
                    //result += @"A = A+D" + Environment.NewLine;
                    //result += @"D = M" + Environment.NewLine;
                    //result += @"@SP" + Environment.NewLine;
                    //result += @"AM = M+1" + Environment.NewLine;
                    //result += @"A = A-1" + Environment.NewLine;
                    //result += @"M = D" + Environment.NewLine;

                    result += @"@" + currentFileName + @"_stat_var_" + value  + Environment.NewLine;
                    result += @"D = M" + Environment.NewLine;
                    result += @"@SP" + Environment.NewLine;
                    result += @"AM = M+1" + Environment.NewLine;
                    result += @"A = A-1" + Environment.NewLine;
                    result += @"M = D" + Environment.NewLine;
                    break;

                case "argument":
                    result += @"@" + value + Environment.NewLine;
                    result += @"D = A" + Environment.NewLine;
                    result += @"@ARG" + Environment.NewLine;
                    result += @"A = M+D" + Environment.NewLine;
                    result += @"D = M" + Environment.NewLine;
                    result += @"@SP" + Environment.NewLine;
                    result += @"AM = M+1" + Environment.NewLine;
                    result += @"A = A-1" + Environment.NewLine;
                    result += @"M = D" + Environment.NewLine;
                   
                    break;

                case "local":

                    result += @"@" + value + Environment.NewLine;
                    result += @"D = A" + Environment.NewLine;
                    result += @"@LCL" + Environment.NewLine;
                    result += @"A = M+D" + Environment.NewLine;
                    result += @"D = M" + Environment.NewLine;
                    result += @"@SP" + Environment.NewLine;
                    result += @"AM = M+1" + Environment.NewLine;
                    result += @"A = A-1" + Environment.NewLine;
                    result += @"M = D" + Environment.NewLine;
                    break;

                case "temp":
                    result += @"@" + value + Environment.NewLine;
                    result += @"D = A" + Environment.NewLine;
                    result += @"@5" + Environment.NewLine;
                    result += @"A = A+D" + Environment.NewLine;
                    result += @"D = M" + Environment.NewLine;
                    result += @"@SP" + Environment.NewLine;
                    result += @"AM = M+1" + Environment.NewLine;
                    result += @"A = A-1" + Environment.NewLine;
                    result += @"M = D" + Environment.NewLine;
                    break;

                case "nothing":
                    result += @"@" + value + Environment.NewLine;
                    result += @"D = A" + Environment.NewLine;
                    result += @"@SP" + Environment.NewLine;
                    result += @"M = M+D" + Environment.NewLine;
                    break;

                case "pointer":
                    if (value == "0")
                    {
                        result += @"@THIS" + Environment.NewLine;
                        result += @"D = M" + Environment.NewLine;
                        result += @"@SP" + Environment.NewLine;
                        result += @"AM = M+1" + Environment.NewLine;
                        result += @"A = A-1" + Environment.NewLine;
                        result += @"M = D" + Environment.NewLine;
                    }
                    else if (value == "1")
                    {
                        result += @"@THAT" + Environment.NewLine;
                        result += @"D = M" + Environment.NewLine;
                        result += @"@SP" + Environment.NewLine;
                        result += @"AM = M+1" + Environment.NewLine;
                        result += @"A = A-1" + Environment.NewLine;
                        result += @"M = D" + Environment.NewLine;
                    }
                    else
                    {
                        throw (new Exception("Unrecognized value for segment pointer in push command"));
                    }
                    break;

                case "this":
                    result += @"@" + value + Environment.NewLine;
                    result += @"D = A" + Environment.NewLine;
                    result += @"@THIS" + Environment.NewLine;
                    result += @"A = M+D" + Environment.NewLine;
                    result += @"D = M" + Environment.NewLine;
                    result += @"@SP" + Environment.NewLine;
                    result += @"AM = M+1" + Environment.NewLine;
                    result += @"A = A-1" + Environment.NewLine;
                    result += @"M = D" + Environment.NewLine;
                    break;

                case "that":
                    result += @"@" + value + Environment.NewLine;
                    result += @"D = A" + Environment.NewLine;
                    result += @"@THAT" + Environment.NewLine;
                    result += @"A = M+D" + Environment.NewLine;
                    result += @"D = M" + Environment.NewLine;
                    result += @"@SP" + Environment.NewLine;
                    result += @"AM = M+1" + Environment.NewLine;
                    result += @"A = A-1" + Environment.NewLine;
                    result += @"M = D" + Environment.NewLine;
                    break;

                default:
#if DEBUG
                    throw (new Exception("Unrecognized segment in push command"));
#endif
                    break;
            }

            return result;
        } // string printPushCommand(string[] words)


        public static string printPopCommand(string line)
        {
            string[] words = line.Split(new char[]{' '});
            return printPopCommand(words);
        }

        public static string printPopCommand(string[] words)
        {
            if (words == null) return "";
            if (words.Length == 0) return "";
            string result = "";

            string segmentName = words[1];
            string value = words[2];
            string currentFileName = Translator.currentlyParsedFileName;

            switch (segmentName)
            {
                case "static":
                    //result += @"@" + value + Environment.NewLine;
                    //result += @"D = A" + Environment.NewLine;
                    //result += @"@16" + Environment.NewLine;
                    //result += @"D = A+D" + Environment.NewLine;
                    //result += @"@5" + Environment.NewLine;
                    //result += @"M = D" + Environment.NewLine;  //saving offset

                    //result += @"@SP" + Environment.NewLine;
                    //result += @"AM = M-1" + Environment.NewLine;
                    //result += @"D = M" + Environment.NewLine;  //saving value

                    //result += @"@5" + Environment.NewLine;
                    //result += @"A = M" + Environment.NewLine;
                    //result += @"M = D" + Environment.NewLine;


                    result += @"@SP" + Environment.NewLine;
                    result += @"AM = M-1" + Environment.NewLine;
                    result += @"D = M" + Environment.NewLine;  //saving value

                    result += @"@" + currentFileName + @"_stat_var_" + value  + Environment.NewLine;
                    result += @"M = D" + Environment.NewLine;
                    break;

                case "nothing":
                    result += @"@" + value + Environment.NewLine;
                    result += @"D = A" + Environment.NewLine;
                    result += @"@SP" + Environment.NewLine;
                    result += @"M = M-D" + Environment.NewLine;
                    break;

                case "argument":
                    result += @"@" + value + Environment.NewLine;
                    result += @"D = A" + Environment.NewLine;
                    result += @"@ARG" + Environment.NewLine;
                    result += @"D = M+D" + Environment.NewLine;
                    result += @"@5" + Environment.NewLine;
                    result += @"M = D" + Environment.NewLine;  //saving offset

                    result += @"@SP" + Environment.NewLine;
                    result += @"AM = M-1" + Environment.NewLine;
                    result += @"D = M" + Environment.NewLine;  //saving value

                    result += @"@5" + Environment.NewLine;
                    result += @"A = M" + Environment.NewLine;
                    result += @"M = D" + Environment.NewLine;
                    break;

                case "local":
                    result += @"@" + value + Environment.NewLine;
                    result += @"D = A" + Environment.NewLine;
                    result += @"@LCL" + Environment.NewLine;
                    result += @"D = M+D" + Environment.NewLine;
                    result += @"@5" + Environment.NewLine;
                    result += @"M = D" + Environment.NewLine;  //saving offset

                    result += @"@SP" + Environment.NewLine;
                    result += @"AM = M-1" + Environment.NewLine;
                    result += @"D = M" + Environment.NewLine;  //saving value

                    result += @"@5" + Environment.NewLine;
                    result += @"A = M" + Environment.NewLine;
                    result += @"M = D" + Environment.NewLine;
                    break;

                case "temp":
                    if (value != "0")
                    {
                        result += @"@" + value + Environment.NewLine;
                        result += @"D = A" + Environment.NewLine;
                        result += @"@5" + Environment.NewLine;
                        result += @"D = A+D" + Environment.NewLine;
                        result += @"@5" + Environment.NewLine;
                        result += @"M = D" + Environment.NewLine;  //saving offset

                        result += @"@SP" + Environment.NewLine;
                        result += @"AM = M-1" + Environment.NewLine;
                        result += @"D = M" + Environment.NewLine;  //saving value

                        result += @"@5" + Environment.NewLine;
                        result += @"A = M" + Environment.NewLine;
                        result += @"M = D" + Environment.NewLine;
                    }
                    else
                    {
                        // Corner case : POP TEMP 0
                        result += @"@SP" + Environment.NewLine;  
                        result += @"AM = M-1" + Environment.NewLine;
                        result += @"D = M" + Environment.NewLine;  //saving value

                        result += @"@5" + Environment.NewLine;
                        result += @"M = D" + Environment.NewLine;
                    }
                    break;

                case "this":
                    result += @"@" + value + Environment.NewLine;
                    result += @"D = A" + Environment.NewLine;
                    result += @"@THIS" + Environment.NewLine;
                    result += @"D = M+D" + Environment.NewLine;
                    result += @"@5" + Environment.NewLine;
                    result += @"M = D" + Environment.NewLine;  //saving offset

                    result += @"@SP" + Environment.NewLine;
                    result += @"AM = M-1" + Environment.NewLine;
                    result += @"D = M" + Environment.NewLine;  //saving value

                    result += @"@5" + Environment.NewLine;
                    result += @"A = M" + Environment.NewLine;
                    result += @"M = D" + Environment.NewLine;
                    break;

                case "that":
                    result += @"@" + value + Environment.NewLine;
                    result += @"D = A" + Environment.NewLine;
                    result += @"@THAT" + Environment.NewLine;
                    result += @"D = M+D" + Environment.NewLine;
                    result += @"@5" + Environment.NewLine;
                    result += @"M = D" + Environment.NewLine;  //saving offset

                    result += @"@SP" + Environment.NewLine;
                    result += @"AM = M-1" + Environment.NewLine;
                    result += @"D = M" + Environment.NewLine;  //saving value

                    result += @"@5" + Environment.NewLine;
                    result += @"A = M" + Environment.NewLine;
                    result += @"M = D" + Environment.NewLine;

                    break;

                case "pointer":
                    if (value == "0")
                    {
                        result += @"@SP" + Environment.NewLine;
                        result += @"AM = M-1" + Environment.NewLine;
                        result += @"D = M" + Environment.NewLine;  //saving value

                        result += @"@THIS" + Environment.NewLine;
                        result += @"M = D" + Environment.NewLine;
                    }
                    else if (value == "1")
                    {
                        result += @"@SP" + Environment.NewLine;
                        result += @"AM = M-1" + Environment.NewLine;
                        result += @"D = M" + Environment.NewLine;  //saving value

                        result += @"@THAT" + Environment.NewLine;
                        result += @"M = D" + Environment.NewLine;
                    }
                    else
                    {
#if DEBUG
                        throw (new Exception("Unrecognized value for segment pointer in pop command"));
#endif
                    }
                    break;

                case "constant":
#if DEBUG
                    throw (new Exception("Constant is illegal segment for pop command"));
#endif

                default:
                    throw (new Exception("Unrecognized segment in pop command"));
            }


            return result;
        } // string printPopCommand(string[] words)

        public static string printAddCommand()
        {
            string result = "";
            result += @"@SP" + Environment.NewLine;
            result += @"AM = M-1" + Environment.NewLine;
            result += @"D = M" + Environment.NewLine;
            result += @"A = A-1" + Environment.NewLine;
            result += @"M = M+D" + Environment.NewLine;
            return result;
        } // string printAddCommand()

        public static string printSubCommand()
        {
            string result = "";
            result += @"@SP" + Environment.NewLine;
            result += @"AM = M-1" + Environment.NewLine;
            result += @"D = M" + Environment.NewLine;
            result += @"A = A-1" + Environment.NewLine;
            result += @"M = M-D" + Environment.NewLine;
            return result;
        } // string printSubCommand()

        public static string printMult2Command()
        {
            string result = "";
            result += @"@SP" + Environment.NewLine;
            result += @"A = M-1" + Environment.NewLine;
            result += @"D = M" + Environment.NewLine;
            result += @"M = M+D" + Environment.NewLine;
            return result;
        } // string printMult2Command()

        public static string printNegCommand()
        {
            string result = "";
            result += @"@SP" + Environment.NewLine;
            result += @"A = M-1" + Environment.NewLine;
            result += @"M = -M" + Environment.NewLine;
            return result;
        } // string printNegCommand()

        public static string printNotCommand()
        {
            string result = "";
            result += @"@SP" + Environment.NewLine;
            result += @"A = M-1" + Environment.NewLine;
            result += @"M = !M" + Environment.NewLine;
            return result;
        } // string printNotCommand()

        public static string printAndCommand()
        {
            string result = "";
            result += @"@SP" + Environment.NewLine;
            result += @"AM = M-1" + Environment.NewLine;
            result += @"D = M" + Environment.NewLine;
            result += @"A = A-1" + Environment.NewLine;
            result += @"M = M&D" + Environment.NewLine;
            return result;
        } // string printAndCommand()

        public static string printOrCommand()
        {
            string result = "";
            result += @"@SP" + Environment.NewLine;
            result += @"AM = M-1" + Environment.NewLine;
            result += @"D = M" + Environment.NewLine;
            result += @"A = A-1" + Environment.NewLine;
            result += @"M = M|D" + Environment.NewLine;
            return result;
        } // string printOrCommand()

        public static string printEqCommand()   //Stas: added currentFileName
        {
            string result = "";
            //in order to difrentiate betwnn files, we need to use file name
            string currentFileName = Translator.currentlyParsedFileName;
            result += @"@SP" + Environment.NewLine;  //D = X - Y
            result += @"AM = M-1" + Environment.NewLine;
            result += @"D = M" + Environment.NewLine;
            result += @"A = A-1" + Environment.NewLine;
            result += @"D = M-D" + Environment.NewLine;

            result += @"@TRUE_" + currentFileName + flowCounter + Environment.NewLine;  //IF   //Stas: added currentFileName
            result += @"D;JEQ" + Environment.NewLine;

            result += @"@SP" + Environment.NewLine; //FALSE
            result += @"A = M-1" + Environment.NewLine;
            result += @"M=0" + Environment.NewLine;
            result += @"@END_" + currentFileName + flowCounter  + Environment.NewLine;   //Stas: added currentFileName
            result += @"0;JMP" + Environment.NewLine;

            result += @"(TRUE_" + currentFileName + flowCounter + ")" + Environment.NewLine; //TRUE   //Stas: added currentFileName
            result += @"@SP" + Environment.NewLine;
            result += @"A = M-1" + Environment.NewLine;
            result += @"M=-1" + Environment.NewLine;
            result += @"(END_" + currentFileName + flowCounter + ")" + Environment.NewLine;   //Stas: added currentFileName

            flowCounter++;
            return result;
        } // string printEqCommand()

        public static string printLtCommand()   //Stas: added currentFileName
        {
            string result = "";
            //in order to difrentiate betwnn files, we need to use file name
            string currentFileName = Translator.currentlyParsedFileName;

            result += @"@SP" + Environment.NewLine;  //D = X - Y
            result += @"AM = M-1" + Environment.NewLine;
            result += @"D = M" + Environment.NewLine;
            result += @"A = A-1" + Environment.NewLine;
            result += @"D = M-D" + Environment.NewLine;

            result += @"@TRUE_" + currentFileName + flowCounter + Environment.NewLine;  //IF   //Stas: added currentFileName
            result += @"D;JLT" + Environment.NewLine;

            result += @"@SP" + Environment.NewLine; //FALSE
            result += @"A = M-1" + Environment.NewLine;
            result += @"M=0" + Environment.NewLine;
            result += @"@END_" + currentFileName + flowCounter+ Environment.NewLine;   //Stas: added currentFileName
            result += @"0;JMP" + Environment.NewLine;

            result += @"(TRUE_" + currentFileName + flowCounter + ")" + Environment.NewLine; //TRUE   //Stas: added currentFileName
            result += @"@SP" + Environment.NewLine;
            result += @"A = M-1" + Environment.NewLine;
            result += @"M=-1" + Environment.NewLine;
            result += @"(END_" + currentFileName + flowCounter + ")" + Environment.NewLine;   //Stas: added currentFileName

            flowCounter++;
            return result;
        } // string printLtCommand()

        public static string printGtCommand()   //Stas: added currentFileName
        {
            string result = "";
            //in order to difrentiate betwnn files, we need to use file name
            string currentFileName = Translator.currentlyParsedFileName;
            result += @"@SP" + Environment.NewLine;  //D = X - Y
            result += @"AM = M-1" + Environment.NewLine;
            result += @"D = M" + Environment.NewLine;
            result += @"A = A-1" + Environment.NewLine;
            result += @"D = M-D" + Environment.NewLine;

            result += @"@TRUE_" + currentFileName + flowCounter + Environment.NewLine;  //IF   //Stas: added currentFileName
            result += @"D;JGT" + Environment.NewLine;

            result += @"@SP" + Environment.NewLine; //FALSE
            result += @"A = M-1" + Environment.NewLine;
            result += @"M=0" + Environment.NewLine;
            result += @"@END_" + currentFileName + flowCounter + Environment.NewLine;   //Stas: added currentFileName
            result += @"0;JMP" + Environment.NewLine;

            result += @"(TRUE_" + currentFileName + flowCounter + ")" + Environment.NewLine; //TRUE   //Stas: added currentFileName
            result += @"@SP" + Environment.NewLine;
            result += @"A = M-1" + Environment.NewLine;
            result += @"M=-1" + Environment.NewLine;
            result += @"(END_" + currentFileName + flowCounter+ ")" + Environment.NewLine;   //Stas: added currentFileName

            return result;
        } // string printGtCommand()

        

        public static string printFunctionDeclaration(string[] words)
        {
            string strFuncDec = String.Empty ;

            strFuncDec += Comment(words);
            string fName = words[1];
            int localVarsCount = 0;
            int.TryParse(words[2], out localVarsCount);
            strFuncDec += "//Printing function declaration for function " + fName + Environment.NewLine;

            strFuncDec += PrintFunctionLabel(fName);

            
            for (int i = 0; i < localVarsCount; i++)
            {
                strFuncDec += Comment("Init local variable #: " + i + " to 0");
                
                string pushString = Printer.printPushCommand("push constant 0".Split(new char[] { ' ' }));
                string popCommand = "pop local " + i.ToString();
                pushString += Printer.printPopCommand(popCommand.Split(new char[] { ' ' }));
                strFuncDec += pushString+ Environment.NewLine;
            }


            return strFuncDec;
        }

        
		


         public static string GetFunctionName(string funName)
         {
             string name =
                String.Format("{0}{1}",  funName, FUNCTION_DECLARATION_LABEL_SUFFIX);
             //string name =
             //    String.Format("{0}.{1}_{2}", Translator.currentlyParsedFileName,funName,FUNCTION_DECLARATION_LABEL_SUFFIX);
             return name;
         }

         public static string GetLabelName(string lblName, bool returnLabel)
         {
             bool isSystemFile = String.IsNullOrWhiteSpace(Translator.currentlyParsedFileName);

             string label = lblName;
             if (isSystemFile)
             {
                 //system call, no need for file  specification
                 label = lblName;                 
             }
             else if (!lblName.Contains('.'))
             {
                 if (!lblName.StartsWith(Translator.currentlyParsedFileName))
                 {
                     label = String.Format("{0}.{1}", Translator.currentlyParsedFileName, lblName);    
                 }
                 
                 
             }

             if (returnLabel)
             {
                 label += RETURN_ADDRESS_LABEL_SUFFIX;
             }
            
             

             return label;
         }

        

        internal static string PrintFunctionLabel(string lblName)
        {
            string name = GetFunctionName(lblName);
            string label = String.Format("({0})", name) + Environment.NewLine;
            return label;
        }

        internal static string printFunctionCall(string[] words)
        {
            string strFuncCall = String.Empty;
            strFuncCall += Comment(words);

            string fName = words[1];
            int callId = ++returnCounter;
            int argCount = 0;
            int.TryParse(words[2], out argCount);
            string returnAddressLabelName = GetLabelName(fName, true);
            returnAddressLabelName += callId;

            strFuncCall += "//Printing function call for function " + fName + Environment.NewLine;

            strFuncCall += Comment("Push return address");
            strFuncCall += Printer.printPushCommand("push label "+returnAddressLabelName) + Environment.NewLine;
            
            char[] spaceSplitter = new char[] { ' ' };
            /*saving enviorment*/
            strFuncCall += Comment("Save srgment pointer LCL");
            strFuncCall += Printer.printPushStoredLocationCommand("LCL")+ Environment.NewLine;
            strFuncCall += Comment("Save srgment pointer ARG");
            strFuncCall += Printer.printPushStoredLocationCommand("ARG") + Environment.NewLine;
            strFuncCall += Comment("Save srgment pointer THIS");
            strFuncCall += Printer.printPushStoredLocationCommand("THIS") + Environment.NewLine;
            strFuncCall += Comment("Save srgment pointer THAT");
            strFuncCall += Printer.printPushStoredLocationCommand("THAT") + Environment.NewLine;
            /*arg = sp -n-5*/
            strFuncCall += Comment("arg = sp -n-5");

            int fullOffset = argCount + 5;
            strFuncCall += "@SP" +Environment.NewLine;
            strFuncCall += "D=M"+Environment.NewLine;

            
            strFuncCall += "@" + fullOffset + Environment.NewLine;
            strFuncCall += "D=D-A"+Environment.NewLine;
            strFuncCall += "@ARG"+Environment.NewLine;
            strFuncCall += "M=D" + Environment.NewLine;
            //@LCL = SP
            strFuncCall += Comment("@LCL = SP");
            strFuncCall += "@SP" + Environment.NewLine;
            strFuncCall += "D=M" + Environment.NewLine;
            strFuncCall += "@LCL" + Environment.NewLine;
            strFuncCall += "M=D" + Environment.NewLine;

            //goto called function
            strFuncCall += Comment("goto called function");
            //string labelBame = GetLabelName(fName,false);

            string funcLabel;
           
             if (fName =="Sys.init")
             {
                 funcLabel = fName + "_dec";                 
             }
             else
             {
                 funcLabel = fName + FUNCTION_DECLARATION_LABEL_SUFFIX;
             }
             strFuncCall += printFlowControlCommand("goto " + funcLabel);
            //print label for return
            strFuncCall += Comment("Declaring label for return");
            strFuncCall += printFlowControlCommand("label " + returnAddressLabelName);
            
           

            return strFuncCall;
        }

        private static string printFlowControlCommand(string line)
        {
            string[] words = line.Split(' ');
            return printFlowControlCommand(words);

        }

        private static string CopyPointerLocation(string pointerToSave, string savelocation)
        {
            string retStr = Printer.printPushStoredLocationCommand(pointerToSave) + Environment.NewLine;
            retStr += CopySPToD(true);
            retStr += CopyDTo(savelocation);
            return retStr;
        }

        /// <summary>
        /// Copies the value from D to the passed argument register.
        /// </summary>
        /// <param name="registerName">Name of the register.</param>
        /// <returns></returns>
        private static string CopyDTo(string registerName)
        {
            if (registerName.StartsWith("@"))
            {
                registerName =registerName.Substring(1,registerName.Length-1);
            }
            string strCopyValue = String.Empty;

            strCopyValue += "@" + registerName + Environment.NewLine;
            strCopyValue += @"M=D" + Environment.NewLine;           
            return strCopyValue;
        }

        /// <summary>
        /// Copies the value from SP to D.
        /// </summary>
        /// <param name="registerName">Name of the register.</param>
        /// <returns></returns>
        private static string CopySPToD(bool copyValue)
        {
            return printStoredLocationToD("SP",copyValue);
        }

        /// <summary>
        /// Copies the value from SP to D.
        /// </summary>
        /// <param name="registerName">Name of the register.</param>
        /// <returns></returns>
        private static string printStoredLocationToD(string location, bool copyValue)
        {
            Regex reg = new Regex("@+");
            location = reg.Replace(location, "@");

            string strCopyValue = String.Empty;

            strCopyValue += "@" + location + Environment.NewLine;
            if (copyValue)
            {
                strCopyValue += @"D = M" + Environment.NewLine;
            }
            else
            {
                strCopyValue += @"D = A" + Environment.NewLine;

            }

            return strCopyValue;
        }

        private static string CopySpTo(string registerName, bool value)
        {
            string prefix = CopySPToD(value);
            string suffix = CopyDTo(registerName);
            string str = prefix + suffix;   
            return str;
        }

        internal static string printReturnCommand(string[] words)
        {            
            string retCmd = String.Empty;
            retCmd += Comment(words);
            /*Frame = LCL*/
            retCmd += Comment("Return: Frame = LCL");

            retCmd += @"@LCL" + Environment.NewLine;
            retCmd += @"D=M" + Environment.NewLine;
            retCmd += @"@FRAME" + Environment.NewLine;
            retCmd += @"M=D" + Environment.NewLine;
            
            /*RET = *(Frame-5)*/
            retCmd += Comment("Return: RET = *(Frame-5)");

            retCmd += FramContentTo("RET", 5);

            /* *Arg = pop() */
            retCmd += Comment("Return: *Arg = pop()");

            retCmd += @"@SP" + Environment.NewLine;
            retCmd += @"AM = M-1" + Environment.NewLine;
            retCmd += @"D = M" + Environment.NewLine;  
            retCmd += @"@ARG" + Environment.NewLine;
            retCmd += @"A = M" + Environment.NewLine;
             retCmd += "M=D"+Environment.NewLine;
             /* SP = ARG+1*/
             retCmd += Comment("Return: SP = ARG+1");

             retCmd += "@ARG"+Environment.NewLine;
             retCmd += "D=M+1"+Environment.NewLine;
             retCmd += "@SP"+Environment.NewLine;
             retCmd += "M=D"+Environment.NewLine;

            /*THAT = *(FRAME-1)*/
            retCmd += Comment("Return: THAT = *(FRAME-1)");

            retCmd += FramContentTo("THAT", 1);

            /*THIS =*(FRAME-2)*/
            retCmd += Comment("Return: THIS =*(FRAME-2)*");

            retCmd += FramContentTo("THIS", 2);

            /*ARG=*(FRAME-3)*/
            retCmd += Comment("Return: ARG=*(FRAME-3)");

            retCmd += FramContentTo("ARG", 3);

            /*LCL=*(FRAME-4)*/
            retCmd += Comment("Return: LCL=*(FRAME-4)");

            retCmd += FramContentTo("LCL", 4);

            /*goto RET*/
            retCmd += Comment("Return: goto RET");

            retCmd += @"@RET" + Environment.NewLine;
            retCmd += @"A=M" + Environment.NewLine;
            retCmd += @"0;JMP" + Environment.NewLine;

            return retCmd;
        }

        private static string FramContentTo(string registerTarget, int offset)
        {
            string retCmd = String.Empty;
            
            retCmd += @"@FRAME" + Environment.NewLine;
            retCmd += @"D=M" + Environment.NewLine;

            retCmd += @"@"+offset + Environment.NewLine;
            retCmd += @"A=D-A"  + Environment.NewLine;
            retCmd += @"D=M" + Environment.NewLine;//D has Frame-offset 
            retCmd += @"@" + registerTarget + Environment.NewLine;
            retCmd += @"M=D" + Environment.NewLine;//registerTarget = *(FRAME-offset)
           
           

           
            return retCmd;
        }

        private static string CopyValue(string destination, string source, int offset)
        {
            bool negative = offset < 0;
            int affectiveOffset = Math.Abs(offset);
            
            string copyStr = String.Empty;

            copyStr += "@" + source + Environment.NewLine;
            copyStr += @"D = A" + Environment.NewLine;//D = source
            copyStr += "@"+affectiveOffset + Environment.NewLine;
            if (negative)
            {
                copyStr += Printer.printNegCommand();                
            } //A = offset
            
            copyStr += @"A = A+D" + Environment.NewLine; //A = source + offset
            copyStr += @"D = M" + Environment.NewLine;//D = *(source + offset)
            
            copyStr += "@" + destination + Environment.NewLine;
            copyStr += @"M = D" + Environment.NewLine;//destination = *(source + offset)


            return copyStr;
        }

        internal static string PrintInitCode()
        {
            string retVal = String.Empty;

            retVal += Comment("printing init code".Split(" ".ToCharArray()));
            //SP=256
            retVal += @"@256" + Environment.NewLine;
            retVal += @"D = A" + Environment.NewLine;
            retVal += @"@SP" + Environment.NewLine;
            retVal += @"M = D" + Environment.NewLine;
            //call Sys.init()
            string[] functionWords = "call Sys.init 0".Split(' ');
            retVal += Printer.printFunctionCall(functionWords) + Environment.NewLine;

            /**/
            

            return retVal;

        }
		

        public static string printFlowControlCommand(string[] words)  //Stas: New code flow control
        {
            if (words == null) return "";
            if (words.Length == 0) return "";
            string result = "";

            string command = words[0];
            string value = words[1];

            string labelName = GetLabelName(value,false);
            switch (command)
            {
                case "label":
                    result += "("+labelName+")" + Environment.NewLine;
                    break;

                case "goto":
                    result += "@" + labelName + Environment.NewLine;
                    result += "0;JMP" + Environment.NewLine;
                    break;

                case "if-goto":
                    result += "@SP" + Environment.NewLine;
                    result += "AM = M-1" + Environment.NewLine;
                    result += "D = M" + Environment.NewLine;

                    result += "@" + labelName + Environment.NewLine;
                    result += "D;JNE" + Environment.NewLine;//AVI:changed per course notes... result += @"D;JGT" + Environment.NewLine;
                    

                    break;
            }

            return result;
        } //string printFlowControlCommand(string[] words)
		[Obsolete]
        internal static string PrintJump(string[] words)
        {
            return PrintJump(words[1]);
        }

        [Obsolete]
        internal static string PrintJump(string words)
        {
            string ret = String.Empty;
            ret += Comment(("if-goto: "+words).Split(" ".ToCharArray()));
            ret += printPopCommand("pop temp 0");
            ret += "@5"+Environment.NewLine;
            ret += "A=M"+Environment.NewLine;

            ret += @"A;JMP" + Environment.NewLine;     
           
            return ret;
        }
    }
}
