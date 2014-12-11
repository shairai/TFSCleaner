using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SR.TFSCleaner.Helpers
{
    public class Line
    {
        public readonly int Size;
        public List<string> SubLines { get; set; }
        public Line(int size)
        {
            this.Size = size;
            SubLines = new List<string>();
        }

        public Line(int size, string line)
        {
            this.Size = size;
            SubLines = new List<string> { line };
        }

        public void Add(string value)
        {
            this.SubLines.Add(value);
        }
    }
    public class Header
    {
        public Header()
        {
            Size = 5;
        }
        public Header(string value, int size = 5)
        {
            this.Value = value;
            this.Size = size;
        }

        public string Value { get; set; }
        public int Size { get; set; }

        public int Length
        {
            get
            {
                return Value.Length + Size * 2;
            }
        }
    }

    public class TableBuilder
    {
        private Header[] _headers;
        private readonly StringBuilder _sb;
        private readonly char ClosingValue = '|';
        private readonly char ClosingHeader = '+';
        private string _lineSplit = string.Empty;
        public TableBuilder()
        {
            _sb = new StringBuilder();
        }

        public TableBuilder(char closingHeaderChar, char closingValueChar) : base()
        {
            ClosingValue = closingValueChar;
            ClosingHeader = closingHeaderChar;
        }

        public void AddHeaders(params Header[] _headers)
        {
            if (_headers == null) throw new ArgumentNullException("Header cannot be null");
            this._headers = _headers;
            string headersPer = string.Empty;
            string headersVal = string.Empty;
            foreach (Header header in this._headers)
            {
                headersPer += string.Format("{0}{1}", ClosingHeader, new String('-', header.Size * 2 + header.Value.Length));
                headersVal += string.Format("{0}{1}{2}{3}", ClosingValue, new String(' ', header.Size), header.Value, new String(' ', header.Size));
            }

            headersPer += ClosingHeader;
            headersVal += ClosingValue;
            _lineSplit = new String('-', headersPer.Length);

            _sb.AppendLine(headersPer);
            _sb.AppendLine(headersVal);
            _sb.AppendLine(headersPer);
        }

        public void AddValues(params string[] values)
        {
            if (values.Length != this._headers.Length)
                throw new ArgumentException("Number of values isn't equal to number of columns.");

            List<Line> lines = new List<Line>();

            for (int index = 0; index < values.Length; index++)
            {
                string value = values[index];
                Header header = this._headers[index];

                if (value != null && header.Length < value.Length)
                {
                    int totalLength = value.Length;
                    Line l = new Line(header.Length);
                    while (totalLength > 0)
                    {
                        int remaining = value.Length > header.Length ? header.Length : value.Length;
                        string val = value.Substring(0, remaining);
                        value = value.Substring(remaining);
                        l.Add(string.Format("{0}{1}{2}", ClosingValue, val, new String(' ', header.Length - val.Length)));
                        totalLength = value.Length;
                    }
                    lines.Add(l);
                }
                else
                {
                    if (value != null)
                        lines.Add(new Line(header.Length, string.Format("{0}{1}{2}", ClosingValue, value, new String(' ', header.Length - value.Length))));
                    else
                    {
                        lines.Add(new Line(header.Length, string.Format("{0}{1}", ClosingValue, new String(' ', header.Length))));
                    }
                }
            }

            int maxSubLines = lines.Select(line => line.SubLines.Count).Concat(new[] { 0 }).Max();
            for (int i = 0; i < maxSubLines; i++)
            {
                string lineStr = string.Empty;
                foreach (Line line in lines)
                {
                    lineStr += line.SubLines.Count > i ? line.SubLines[i] : string.Format("|{0}", new String(' ', line.Size));
                }
                _sb.AppendLine(lineStr);
            }
            _sb.AppendLine(_lineSplit);
        }

        public override string ToString()
        {
            return _sb.ToString();
        }

        public void Clean()
        {
            this._sb.Clear();
        }
    }
}
