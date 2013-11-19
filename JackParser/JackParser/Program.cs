using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;


namespace Ex2
{
    class Program
    {
        static void Main(string[] args)
        {
            bool hasArgument = false;
            string folder = String.Empty;
            if (args.Length > 0 && args[0] != null)
            {
                folder = args[0].ToString();
                if (folder.EndsWith(@"\"))
                {
                    folder= folder.Substring(0,folder.Length-1);
                }
                hasArgument = Directory.Exists(folder);
                if (!hasArgument)
                {
                    string msg = 
                        String.Format("did not get a valid argument (folder might not exist): '{0}'",folder);
                    Console.WriteLine(msg);
                    Environment.Exit(1);
                }
                else
                {
                    string msg =
                        String.Format("got argument folder: '{0}'", folder);
                    Console.WriteLine(msg);
                }
            }
            
            Tokenizer tokenizer = new Tokenizer(folder);

            List<Exception> tokensError = new List<Exception>();
            List<Exception> parserError = new List<Exception>(); 
            try
            {
                List<Tuple<string,XmlDocument>> tokenFiles;
                tokensError = tokenizer.start(out  tokenFiles);

                List<Tuple<string, string>> vmFiles;
                parserError = JackParser.JackParser.Start(tokenFiles, out vmFiles);

               

                foreach (Tuple<string, string> vmTuple in vmFiles)
                {
                    string[] tmp = vmTuple.Item1.Split('.');
                    string fName = tmp[0];
                   
                    fName = fName + ".vm";
                    System.IO.File.WriteAllText(fName, vmTuple.Item2);
                }
                
                
            }
            catch (Exception ex)
            {

                Console.WriteLine("Failed to compile: "+ex.Message);
                Console.WriteLine(ex.ToString());
            }

            PrintErrors(tokensError, "Had errors while creating tokens");
            PrintErrors(parserError, "Had errors while parsing:");
            

        }

        private static void PrintErrors(List<Exception> errorList, string msg)
        {
            if (errorList.Count > 0)
            {
                Console.WriteLine(msg);
                Console.WriteLine("Total of "+ errorList.Count+" errors");
                Console.WriteLine(Environment.NewLine);
                foreach (Exception ex in errorList)
                {
                    
                    Console.WriteLine(">> "+ex.Message);
                    //Console.WriteLine(Environment.NewLine);
                    //Console.WriteLine(ex.ToString());
                    //Console.WriteLine(Environment.NewLine);

                }
                Console.WriteLine(Environment.NewLine);
               
            }
            
            
        }
    }
}
