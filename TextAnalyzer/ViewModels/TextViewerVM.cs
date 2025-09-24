using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

namespace TextAnalyzer.ViewModels
{
    partial class TextViewerVM : ViewModelBase
    {
        Action<string> _addFilterDelegate;

        [ObservableProperty]
        string _text;

        string _textToFind = string.Empty;
        public string TextToFind
        {
            get => _textToFind;
            set
            {
                _textToFind = value;
                FindCommand.NotifyCanExecuteChanged();
            }
        }

        [ObservableProperty]
        int _selectionStart = 0;

        [ObservableProperty]
        int _selectionEnd = 0;

        [ObservableProperty]
        bool _isTextFound = true;

        public IRelayCommand FindCommand { get; private set; }

        public TextViewerVM(string text, Action<string> addFilterDelegate)
        {
            _addFilterDelegate = addFilterDelegate;
            Text = text;
            FindCommand = new RelayCommand<bool?>(
                FindText, (bool? backward) => !string.IsNullOrEmpty(TextToFind));
        }

        public void FormatJson()
        {
            // Assume there is at most one json string

            // Look for well-formed JSON object (starting with { and ending with })
            var jsonStr = ExtractJsonString(Text, '{', '}');
            if (jsonStr == null)
            {
                // Look for JSON array (starting with [ and ending with ])
                jsonStr = ExtractJsonString(Text, '[', ']');
                // ignore the [ number ] pattern
                if (jsonStr == null || jsonStr.Item2.Length < 10)
                    return;
            }

            try
            {
                dynamic? parsedJson = JsonValue.Parse(jsonStr.Item2);
                if (parsedJson != null)
                {
                    var sb = new StringBuilder();
                    var jsonStrStartIdx = jsonStr.Item1 - jsonStr.Item2.Length + 1;
                    if (jsonStrStartIdx > 0)
                        sb.AppendLine(Text.Substring(0, jsonStrStartIdx));

                    sb.AppendLine(parsedJson.ToString());

                    if (jsonStr.Item1 < Text.Length - 1)
                        sb.Append(Text.Substring(jsonStr.Item1 + 1));

                    Text = sb.ToString();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        public void FormatEOL()
        {
            Text = Text.Replace("\\r\\n", Environment.NewLine)
                .Replace("\\n", Environment.NewLine)
                .Replace("\\r", Environment.NewLine);
        }

        public void AddFilter(object param)
        {
            _addFilterDelegate(Text);
        }

        void FindText(bool? backward)
        {
            var idx = -1;
            if (backward == true)
            {
                idx = Text.LastIndexOf(
                     TextToFind, SelectionStart - 1, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                idx = Text.IndexOf(
                     TextToFind, SelectionEnd, StringComparison.OrdinalIgnoreCase);
            }

            if (idx >= 0)
            {
                SelectionStart = idx;
                SelectionEnd = idx + TextToFind.Length;
                IsTextFound = true;
            }
            else if (backward == true && SelectionStart < Text.Length - 1)
            {
                idx = Text.LastIndexOf(
                    TextToFind,
                    Text.Length - 1,
                    Text.Length - SelectionStart,
                    StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    SelectionStart = idx;
                    SelectionEnd = idx + TextToFind.Length;
                    IsTextFound = true;
                }
                else
                {
                    SelectionStart = 0;
                    SelectionEnd = 0;
                    IsTextFound = false;
                }
            }
            else if (backward != true && SelectionEnd > 0)
            {
                idx = Text.IndexOf(
                    TextToFind, 0, SelectionEnd, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    SelectionStart = idx;
                    SelectionEnd = idx + TextToFind.Length;
                    IsTextFound = true;
                }
                else
                {
                    SelectionStart = 0;
                    SelectionEnd = 0;
                    IsTextFound = false;
                }
            }
            else
            {
                SelectionStart = 0;
                SelectionEnd = 0;
                IsTextFound = false;
            }
        }

        Tuple<int, string>? ExtractJsonString(
            string input, char prefix, char suffix)
        {
            int depth = 0;
            int startIndex = -1;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == prefix)
                {
                    if (depth == 0)
                    {
                        startIndex = i;
                    }
                    depth++;
                }
                else if (input[i] == suffix)
                {
                    depth--;
                    if (depth == 0 && startIndex != -1)
                    {
                        string candidate = input.Substring(startIndex, i - startIndex + 1);
                        if (IsValidJson(candidate))
                        {
                            return new Tuple<int, string>(i, candidate);
                        }
                        startIndex = -1;
                    }
                    else if (depth < 0)
                    {
                        depth = 0; // Reset if we have more closing braces or brackets
                    }
                }
            }

            return null;
        }

        bool IsValidJson(string candidate)
        {
            try
            {
                // Try to parse as JsonValue
                JsonValue.Parse(candidate);
                return true;
            }
            catch
            {
                // If parsing fails, it's not valid JSON
                return false;
            }
        }
    }
}
