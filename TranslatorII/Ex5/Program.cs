using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ex5
{
    class Program
    {
        internal static bool WriteInitCode;
        static void Main(string[] args)
        {
            string path = args[0];
            List<String> aruments = args.Select(s=>s.ToLower()).ToList();
            Program.WriteInitCode = !aruments.Any(s => s.Equals("noinit", StringComparison.InvariantCultureIgnoreCase)
                                                    || s.Equals("-noinit", StringComparison.InvariantCultureIgnoreCase));
            Translator translator = new Translator(path);   // path can not contain white spaces
                                                            // path can be file or folder
        }
    }
}
