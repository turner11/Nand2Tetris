using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assembler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No path recieved");
                return;
            }
            string path = args[0];
            FileInfo fi = new FileInfo(path);
            if (!fi.Exists)
            {
                Console.WriteLine(String.Format("File argument supplied does not exists ({0})",path));
                return;
            }
            if (!fi.Extension.Equals(".asm",StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine(String.Format("File extention was not 'asm'. It was: '{0}'", fi.Extension));
                return;
            }

            try
            {
                String[] hackStr = File.ReadAllLines(path);
                Assembler asm = new Assembler(hackStr);
                
                string assemblyCode = asm.GetAssemblyCode();

                string fileName = Path.Combine(System.IO.Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path)) + ".hack";
                
                File.WriteAllText(fileName, assemblyCode);
            }
            catch (Exception e)
            {
                
                Console.WriteLine(String.Format("An error occured: {0}",e.Message));
                Console.WriteLine(e.ToString());
            }
        }
    }
}
