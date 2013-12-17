using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace JackParser
{
    internal static class SymbolClassifications
    {
        #region symbol classifications
        internal static ReadOnlyCollection<string> _allKeyWords;
        internal static ReadOnlyCollection<string> _generalKeyWords = new ReadOnlyCollection<string>(
            new List<String> { "class", "void", "else" });

        internal static ReadOnlyCollection<string> _constantKeyWords = new ReadOnlyCollection<string>(
            new List<String> { "true", "false", "null", "this" });

        internal static ReadOnlyCollection<string> _statementsHeaders = new ReadOnlyCollection<string>(
           new List<String> { "let", "do", "if", "while", "return" });

        internal static ReadOnlyCollection<string> _classVariablesModifiers = new ReadOnlyCollection<string>(
            new List<String> { "internal", "static", "field" });

        internal static ReadOnlyCollection<string> _variablesModifiers = new ReadOnlyCollection<string>(
            new List<String> { "var" });

        internal static ReadOnlyCollection<string> _variablesTypes = new ReadOnlyCollection<string>(
            new List<String> { "int", "char", "boolean","String" });

        internal static ReadOnlyCollection<string> _subRoutineReturnType;

        internal static ReadOnlyCollection<string> _subRoutineModifiers = new ReadOnlyCollection<string>(
            new List<String> { "constructor", "function", "method" });

        internal static ReadOnlyCollection<string> _symbols;


        internal static ReadOnlyCollection<string> _oparations =
            new ReadOnlyCollection<string>(new List<String> { "+", "-", "*", "/", "&", "|", "<", ">", "=" });

        internal static ReadOnlyCollection<string> _unariOparations =
            new ReadOnlyCollection<string>(new List<String> { "~", "-", "#" });

        internal static ReadOnlyCollection<string> _allOparations;
        #endregion


        static SymbolClassifications()
        {
            var srRetTypes = new List<string>(SymbolClassifications._variablesTypes);
            srRetTypes.Add("void");

            List<string> allKeyWords = new List<string>();

            /*symbols*/
            List<string> allSymbols = new List<string>() { "[", "]", "{", "}", "(", ")", ".", ",", ";" };

            List<string> allOperations = new List<string>();
            allOperations.AddRange(SymbolClassifications._oparations);
            allOperations.AddRange(SymbolClassifications._unariOparations);
            SymbolClassifications._allOparations = new ReadOnlyCollection<string>(allOperations.Distinct().ToList());

            allSymbols.AddRange(SymbolClassifications._allOparations);
            
            SymbolClassifications._symbols = new ReadOnlyCollection<string>(allSymbols);

            /*all keywords*/
            SymbolClassifications._subRoutineReturnType = new ReadOnlyCollection<string>(srRetTypes);
            allKeyWords.AddRange(SymbolClassifications._generalKeyWords);
            allKeyWords.AddRange(SymbolClassifications._classVariablesModifiers);
            allKeyWords.AddRange(SymbolClassifications._subRoutineReturnType);
            allKeyWords.AddRange(SymbolClassifications._statementsHeaders);
            allKeyWords.AddRange(SymbolClassifications._oparations);
            allKeyWords.AddRange(SymbolClassifications._unariOparations);
            allKeyWords.AddRange(SymbolClassifications._symbols);
            allKeyWords.AddRange(SymbolClassifications._variablesTypes.Distinct());
            allKeyWords.AddRange(SymbolClassifications._constantKeyWords);
            allKeyWords.AddRange(SymbolClassifications._variablesModifiers);


            allKeyWords.AddRange(SymbolClassifications._subRoutineModifiers);
            SymbolClassifications._allKeyWords = new ReadOnlyCollection<string>(allKeyWords.Distinct().ToList());
        }
    }
}
