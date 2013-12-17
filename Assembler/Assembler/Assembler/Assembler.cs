#if DEBUG
    #define Exceptions
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                    line = parts.Length>0? parts[0]: String.Empty;
                }
            }
            string[] cleanLines = lines.Where(l => !String.IsNullOrWhiteSpace(l) && !l.StartsWith("//")).ToArray();
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
                assembly += (currAssemblyCode + Environment.NewLine).Trim(); //the trim will remove empty lines
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
                    throw new NotImplementedException("Unknonw line type: " + lType);
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the D command assembly line.
        /// </summary>
        /// <param name="hackLine">The hack line.</param>
        /// <returns>the assembly line</returns>
        private string GetDCommandAssemblyLine(string hackLine)
        {
            throw new NotImplementedException();
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
            string binaryString = Convert.ToString(number, 2);
            binaryString = binaryString.PadLeft(BINARY_ROW_LENGTH, '0');
            return binaryString;

        }

       

        #region Sub classes
        enum LineTypes
        {
            Unknown = 0,
            Acommand = 1,
            Dcommand = 2,
            Comment = 3,
            Empty = 4
        } 
        #endregion

        
    }
}
