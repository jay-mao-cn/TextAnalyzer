using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TextAnalyzer.Models;

namespace TextAnalyzer.Helpers
{
    internal class FilterRunner
    {
        enum LogicOperType
        {
            None,
            And,
            Or,
        }

        FilterBase _filter;
        LogicOperType _logicOperator = LogicOperType.None;
        private string[] _texts = [];

        internal FilterRunner(FilterBase filter)
        {
            _filter = filter;
            ParseText(filter.FilterText);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool Match(string input, IEnumerable<int>? markers)
        {
            if (_filter.FilterType == FilterType.Marker)
            {
                return markers!.Contains(_filter.Marker);
            }

            if (_filter.IsRegularExpression)
            {
                var options = _filter.IsCaseSensitive
                    ? RegexOptions.None : RegexOptions.IgnoreCase;
                return Regex.IsMatch(input, _filter.FilterText, options);
            }

            StringComparison comparison = _filter.IsCaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            switch (_logicOperator)
            {
                case LogicOperType.None:
                    return input.Contains(_filter.FilterText, comparison);

                case LogicOperType.And:
                    foreach (var txt in _texts)
                    {
                        if (!input.Contains(txt, comparison))
                            return false;
                    }
                    return true;

                case LogicOperType.Or:
                    foreach (var txt in _texts)
                    {
                        if (input.Contains(txt, comparison))
                            return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        void ParseText(string text)
        {
            if (_filter.IsLogicOperation)
            {
                _texts = text.Split(" && ");
                if (_texts.Length > 1)
                {
                    _logicOperator = LogicOperType.And;
                }
                else
                {
                    _texts = text.Split(" || ");
                    if (_texts.Length > 1)
                    {
                        _logicOperator = LogicOperType.Or;
                    }
                    else
                    {
                        _logicOperator = LogicOperType.None;
                    }
                }
            }
            else
            {
                _logicOperator = LogicOperType.None;
            }
        }
    }
}
