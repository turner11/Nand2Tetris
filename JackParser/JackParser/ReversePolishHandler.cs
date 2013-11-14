using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JackParser
{
    static class ReversePolishHandler
    {




        public static String ToReversePolish(List<String> tokens)
        {
            Stack<string> stack = new Stack<string>();
            
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < tokens.Count; i++)
            {
                string x = tokens[i];
                if (x == "(")
                    stack.Push(x);
                else if (x == ")")
                {
                    while (stack.Count > 0 && stack.Peek() != "(")
                        sb.Append(stack.Pop());
                    stack.Pop();
                }                
                else if (IsOperator(x))
                {
                    while (stack.Count > 0 && stack.Peek() != "(" && Prior(x) <= Prior(stack.Peek()))
                        sb.Append(stack.Pop());
                    stack.Push(x);
                }
                else if (IsOperandus(x))
                {
                    sb.Append(x);
                }
                else
                {
                    string y = stack.Pop();
                    if (y != "(")
                        sb.Append(y);
                }
            }
            while (stack.Count > 0)
            {
                sb.Append(stack.Pop());
            }
            return sb.ToString();
        }

        static bool IsOperator(string c)
        {
            return SymbolClassifications._oparations.Contains(c.ToString());
        }
        
        static bool IsOperandus(string c)
        {
            return !IsOperator(c);
        }
        static int Prior(string c)
        {
            switch (c)
            {
                case "=":
                    return 1;
                case "+":
                    return 2;
                case "-":
                    return 2;
                case "*":
                    return 3;
                case "/":
                    return 3;
                case "^":
                    return 4;
                default:
                    throw new ArgumentException("Rossz parameter");
            }
        }
    }
}

