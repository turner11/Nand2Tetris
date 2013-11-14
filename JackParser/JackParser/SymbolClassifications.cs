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
            new List<String> { "int", "char", "boolean" });

        internal static ReadOnlyCollection<string> _subRoutineReturnType;

        internal static ReadOnlyCollection<string> _subRoutineModifiers = new ReadOnlyCollection<string>(
            new List<String> { "constructor", "function", "method" });

        internal static ReadOnlyCollection<string> _symbols;


        internal static ReadOnlyCollection<string> _oparations =
            new ReadOnlyCollection<string>(new List<String> { "+", "-", "*", "/", "&", "|", "<", ">", "=" });

        internal static ReadOnlyCollection<string> _unariOparations =
            new ReadOnlyCollection<string>(new List<String> { "~", "-", "#" });
        #endregion

    }
}
