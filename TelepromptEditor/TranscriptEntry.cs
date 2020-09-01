using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Teleprompter
{
    class TranscriptEntry : INotifyPropertyChanged
    {
        private int index;
        private string range;
        private TimeSpan start;
        private TimeSpan end;
        private string prompt;
        private double width;
        private bool selected;

        public TranscriptEntry(int index, string range, string prompt)
        {
            this.index = index;
            this.prompt = prompt;
            this.range = range;
            ParseRange(range);
        }

        void ParseRange(string range)
        {
            int pos = range.IndexOf("-->");
            if (pos > 0)
            {
                string start = range.Substring(0, pos).Trim().Replace(",", ".");
                string end = range.Substring(pos + 3).Trim().Replace(",", ".");
                this.start = TimeSpan.Parse(start);
                this.end = TimeSpan.Parse(end);
            }
        }

        public int Index
        {
            get { return this.index; }
            set { this.index = value; NotifyChanged("Index"); }
        }

        public string Range
        {
            get { return this.range; }
            set { this.range = value; NotifyChanged("Range"); }
        }

        public string Prompt
        {
            get { return this.prompt; }
            set { this.prompt = value; NotifyChanged("Prompt"); }
        }

        public double StartSeconds
        {
            get
            {
                return start.TotalSeconds;
            }
        }

        public bool Contains(double seconds)
        {
            return seconds >= start.TotalSeconds && seconds <= end.TotalSeconds;
        }

        public bool Selected
        {
            get { return this.selected; }
            set { this.selected = value; NotifyChanged("Selected"); }
        }


        public double Width
        {
            get { return this.width; }
            set { this.width = value; NotifyChanged("Width"); }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
