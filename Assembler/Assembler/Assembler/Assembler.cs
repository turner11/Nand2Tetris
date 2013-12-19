#if DEBUG
    #define Exceptions
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Assembler
{
    class Assembler
    {

        #region Data members

        const int VARIABLE_BASE_ADDRESS = 16;
        const int BINARY_ROW_LENGTH = 16;
                
        Dictionary<string, int> _varAddressByName;
        Dictionary<string, int> _LabelAddressByName;




        private readonly ReadOnlyCollection<String> _hackLines;
        /// <summary>
        /// The hack lines to convert to assembly
        /// </summary>
        public ReadOnlyCollection<String> HackStr
        {
            get { return _hackLines; }
        }
        
 
	#endregion

        #region C'tor
        public Assembler(string[] hackLines)
        {
            string[] cleanHack = this.stripComments(hackLines);
            this._hackLines = new ReadOnlyCollection<String>(cleanHack );
            this.Init();
        }



        /// <summary>
        /// Initializess this instance.
        /// </summary>
        private void Init()
        {
            this._LabelAddressByName = new Dictionary<string,int>();
            this._varAddressByName = new Dictionary<string, int>();
            this._varAddressByName.Add("SP",0);
            this._varAddressByName.Add("LCL", 1);
            this._varAddressByName.Add("ARG", 2);
            this._varAddressByName.Add("THIS", 3);
            this._varAddressByName.Add("THAT", 4);
        } 
        #endregion


        private string[] stripComments(string[] lines)
        {
            if (lines == null)
            {
                return new string[0];
            }
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string commentStartSymble = "//";
                bool hasComment = line.Contains(commentStartSymble);
                if (hasComment)
                {
                    String[] parts = line.Split(new string[]{commentStartSymble},StringSplitOptions.None);
                    line = (parts.Length>0? parts[0]: String.Empty).Trim();
                    lines[i] = line;
                }
            }
            string[] cleanLines = lines.Where(l => !String.IsNullOrWhiteSpace(l) && !l.StartsWith("//")).Select(l=>l.Trim()).ToArray();
            return cleanLines;
        } 
        /// <summary>
        /// Gets the assembly code.
        /// </summary>
        /// <returns>the assembly code</returns>
        internal string GetAssemblyCode()
        {
            this.Init();
            string[] hackCode = this._hackLines.ToArray();
            string[] noLabelsHack = this.GetHackWithNoLabels(hackCode);
            string[] noVariablesHack = this.GetHackWithNoVariables(noLabelsHack);

            string asm = GetAssemblyCodeInternal(noVariablesHack);
            return asm;
        }

        /// <summary>
        /// Gets the hack code after replacing lables.
        /// </summary>
        /// <param name="hackCode">The hack code.</param>
        /// <returns>the hack code with no lables</returns>
        private string[] GetHackWithNoLabels(string[] hackCode)
        {
            List<string> nolblHack = new List<string>();
            for (int i = 0; i < hackCode.Length; i++)
            {
                string hackLine = hackCode[i].Trim();
                bool isLbl = hackLine.StartsWith("(") && hackLine.EndsWith(")");
                if (isLbl)
                {
                    string lblName = hackLine.Substring(1, hackLine.Length - 2);
                    int lblAddress = i + 1;// label address is always next line...
                    this._LabelAddressByName.Add(lblName, lblAddress);
                    
                    hackLine = String.Empty;
                }

                nolblHack.Add(hackLine);
            }

            for (int i = 0; i < nolblHack.Count; i++)
            {
                string hackLine = hackCode[i].Trim();
                if (hackLine.StartsWith("@"))
                {
                    string potentialLabel = hackLine.Substring(1, hackLine.Length - 1);
                    bool isLabel = this._LabelAddressByName.ContainsKey(potentialLabel);
                    if (isLabel)
                    {
                        int lblAddress = this._LabelAddressByName[potentialLabel];
                        string updatedHackLine = Assembler.GetBinaryValue(lblAddress);
                        nolblHack[i] = updatedHackLine;
                    }
                }
                
            }

            
            return nolblHack.ToArray();
        }
        
        /// <summary>
        /// Gets the hack code after replacing variables.
        /// </summary>
        /// <param name="hackCode">The hack code.</param>
        /// <returns>the hack code with no variables</returns>
        private string[] GetHackWithNoVariables(string[] hackCode)
        {
            //replace all variable with a number reperesentation
            List<string> noVarsHack = new List<string>();
            for (int i = 0; i < hackCode.Length; i++)
            {
                string hackLine = hackCode[i];
                int temp;
                bool isVar = hackLine.Length > 1 && 
                            hackLine[0] == '@' && 
                            !int.TryParse(hackLine[1].ToString(), out temp);
                if (isVar)
                {
                    string varName = hackLine.Substring(1, hackLine.Length - 1);
                    int variableAddress = GetVariableAddress(varName);

                    string variableAsBinary = Assembler.GetBinaryValue(variableAddress);
                    hackLine = variableAsBinary;
                }

                noVarsHack.Add(hackLine);
            }
            return noVarsHack.ToArray();
        }

        /// <summary>
        /// Gets the variable address.
        /// </summary>
        /// <param name="hackLine">The variable Name.</param>
        /// <returns></returns>
        private int GetVariableAddress(string varName)
        {
            int directMemAddress;
            string memAddress = varName.Substring(1, varName.Length - 1);

            if (varName.StartsWith("R") && int.TryParse(memAddress,out directMemAddress))
            {
                //this is a direct memory access (e.g. R15)
                return directMemAddress;
            }

            int variableAddress ;
            if (this._varAddressByName.ContainsKey(varName))
            {
                variableAddress = this._varAddressByName[varName];
            }
            else
            {
                //add the address
                int offset = this._varAddressByName.Keys.Count;
                variableAddress = Assembler.VARIABLE_BASE_ADDRESS + offset;
                this._varAddressByName.Add(varName, variableAddress);
           
            }
            
            return variableAddress;
        }


        /// <summary>
        /// Gets the assembly code.
        /// </summary>
        /// <param name="noVariablesHack">The hack code after taking variables out .</param>
        /// <returns>the assembly code</returns>
        private string GetAssemblyCodeInternal(string[] noVariablesHack)
        {
            string assembly = String.Empty;

            for (int i = 0; i < noVariablesHack.Length; i++)
            {
                string hackLine = noVariablesHack[i];
                string currAssemblyCode = this.GetAssemblyCodeByLine(hackLine);
                assembly += (currAssemblyCode + Environment.NewLine);//.Trim(); //the trim will remove empty lines
            }
            return assembly;
        }

        #region Handle lines
        /// <summary>
        /// Gets the assembly code by hack line.
        /// </summary>
        /// <param name="hackLine">The hack line.</param>
        /// <returns>the assembly code for the hack line</returns>
        private string GetAssemblyCodeByLine(string hackLine)
        {
            LineTypes lType = this.GetHackLineType(hackLine);
            string asmLine = String.Empty;
            switch (lType)
            {
                case LineTypes.Acommand:
                    asmLine = this.GetACommandAssemblyLine(hackLine);
                    break;
                case LineTypes.Dcommand:
                    asmLine = this.GetDCommandAssemblyLine(hackLine);
                    break;
                case LineTypes.Comment:
                case LineTypes.Empty:
                    asmLine = String.Empty;
                    break;
                default:
                    this.ThrowException("Unknonw line type: " + lType);
                    break;
            }

            return asmLine;

        }

        /// <summary>
        /// Gets the A command assembly line.
        /// </summary>
        /// <param name="hackLine">The hack line.</param>
        /// <returns>the assembly line</returns>
        private string GetACommandAssemblyLine(string hackLine)
        {
            string asmLine = String.Empty;
            string clnHackLine = hackLine.Trim();
            if (clnHackLine.StartsWith("@"))
            {
                string cleanValue = clnHackLine.Substring(1, clnHackLine.Length - 1);
                int num;
                if (int.TryParse(cleanValue, out num))
                {
                    asmLine = Assembler.GetBinaryValue(num);
                }
                else
                {
                    this.ThrowException(String.Format("Expected number in A command, but got '{0}'", hackLine));
                }
                   
                
            }
            return asmLine;
        }

        /// <summary>
        /// Throws an exception.
        /// </summary>
        /// <param name="p">The message.</param>
        /// <param name="hackLine">The hack line.</param>
        private void ThrowException(string msg)
        {
#if Exceptions
            throw new Exception(msg);
#endif
        }

        /// <summary>
        /// Gets the D command assembly line.
        /// </summary>
        /// <param name="hackLine">The hack line.</param>
        /// <returns>the assembly line</returns>
        private string GetDCommandAssemblyLine(string hackLine)
        {
            string hackLineUp= hackLine.ToUpper();
            Regex cleaner = new Regex(" +");
            string[] raw = cleaner.Replace(hackLineUp, String.Empty).Split(new char[] { ';', '=' }, 3);
            string prefix = "111";
            string comp = String.Empty;
            string dest = String.Empty;
            string jmp = String.Empty;

            bool hasDest = hackLine.Contains("=");
            int compLocation = hasDest? 1 : 0;
            if (raw.Length > compLocation)
            {
                string rawComp = raw[compLocation].Trim();
                comp = GetDcommandCompareSegment(rawComp);
            }


            if (hasDest && raw.Length > 0)
            {
                string rawDest = raw[0].Trim();
                dest = GetDcommandDestinationSegment(rawDest);
            }
            else
            {
                dest = "111";
            }

            bool hasJump = hackLine.Contains(";");
            int jumpLocation = hasDest?2:1;
            if (hasJump && raw.Length > jumpLocation)
            {
                string rawDest = raw[jumpLocation].Trim();
                jmp = GetDcommandJumpSegment(rawDest);
            }
            else
            {
                jmp = "000";
            }



            char paddingChar = '0';
#if Exceptions
            paddingChar = '-';
#endif
            string ret = prefix + comp.PadRight(7, paddingChar) + dest.PadRight(3, paddingChar) + jmp.PadRight(3, paddingChar);
            return ret;
        }

        private string GetDcommandJumpSegment(string rawDest)
        {
            int decVal = 0;
            switch (rawDest)
            {
                case "null": decVal = 0; break;
                case "JGT": decVal = 1; break;
                case "JEQ": decVal = 2; break;
                case "JGE": decVal = 3; break;
                case "JLT": decVal = 4; break;
                case "JNE": decVal = 5; break;
                case "JLE": decVal = 6; break;
                case "JMP": decVal = 7; break;
                default:
                    this.ThrowException("Got an unexpected D command jump segment: " + rawDest);
                    break;
            }
            string dest = Assembler.GetBinaryValue(decVal, 3);
            return dest;
        }

        private string GetDcommandDestinationSegment(string HackDestStr)
        {
            int decVal = 0;
            switch (HackDestStr)
            {
                case "null": decVal = 0; break;
                case "M": decVal = 1; break;
                case "D": decVal = 2; break;
                case "MD": decVal = 3; break;
                case "A": decVal = 4; break;
                case "AM": decVal = 5; break;
                case "AD": decVal = 6; break;
                case "AMD": decVal = 7; break;
                default:
                    this.ThrowException("Got an unexpected D command dest segment: " + HackDestStr);
                    break;
            }
            string dest = Assembler.GetBinaryValue(decVal, 3);
            return dest;
        }

        /// <summary>
        /// Gets the d command compare segment.
        /// </summary>
        /// <param name="hackComp">The hack compare string.</param>
        /// <returns></returns>
        private string GetDcommandCompareSegment(string hackComp)
        {
            string comp = String.Empty;
            comp += hackComp.Contains('M') ? 1 : 0;
            hackComp = hackComp.Replace('M', 'A'); //for doing table only once...
            switch (hackComp)
            {
                case "0": comp += "101010"; break;
                case "1": comp += "111111"; break;
                case "-1": comp += "111010"; break;
                case "D": comp += "001100"; break;
                case "A": comp += "110000"; break;
                case "!D": comp += "001101"; break;
                case "!A": comp += "110001"; break;
                case "-D": comp += "001111"; break;
                case "-A": comp += "110011"; break;
                case "D+1": comp += "011111"; break;
                case "A+1": comp += "110111"; break;
                case "D-1": comp += "001110"; break;
                case "A-1": comp += "110010"; break;
                case "A+D":
                case "D+A": comp += "000010"; break;
                case "D-A": comp += "010011"; break;
                case "A-D": comp += "000111"; break;
                case "D&A": comp += "000000"; break;
                case "D|A": comp += "010101"; break;
                default:
                    this.ThrowException("Got an unexpected D command comp " + hackComp);
                    break;
            }
            return comp;
        }

        /// <summary>
        /// Gets the type of the hack line.
        /// </summary>
        /// <param name="hackLine">The hack line.</param>
        /// <returns></returns>
        private LineTypes GetHackLineType(string hackLine)
        {
            LineTypes lType = LineTypes.Unknown;
            if (hackLine.Length == 0)
            {
                lType = LineTypes.Empty;
            }
            else if (hackLine.StartsWith("@"))
            {
                lType = LineTypes.Acommand;
            }
            else if (hackLine.StartsWith("//"))
            {
                lType = LineTypes.Comment;
            }
            else if (hackLine.All(c=> new char[]{'0','1',' '}.Contains(c)) && 
                hackLine.Replace(" ",String.Empty).Length == BINARY_ROW_LENGTH)
            {
                lType = LineTypes.Assembly;
            }
            else
            {
                lType = LineTypes.Dcommand;
            }
            return lType;
        } 
        #endregion

        /// <summary>
        /// Gets the binary value of specified number (padded to <see cref="BINARY_ROW_LENGTH"/> length).
        /// </summary>
        /// <param name="number">The number.</param>
        /// <returns>the binary value</returns>
        private static string GetBinaryValue(int number)
        {
           return Assembler.GetBinaryValue(number,BINARY_ROW_LENGTH);

        }

        /// <summary>
        /// Gets the binary value of specified number (padded to <see cref="BINARY_ROW_LENGTH"/> length).
        /// </summary>
        /// <param name="number">The number.</param>
        /// <returns>the binary value</returns>
        private static string GetBinaryValue(int number, int digits)
        {
            string binaryString = Convert.ToString(number, 2);
            binaryString = binaryString.PadLeft(digits, '0');
            return binaryString;

        }

       

        #region Sub classes
        enum LineTypes
        {
            Unknown = 0,
            Acommand = 1,
            Dcommand = 2,
            Comment = 3,
            Assembly = 3,
            Empty = 4
        } 
        #endregion

        
    }
}
