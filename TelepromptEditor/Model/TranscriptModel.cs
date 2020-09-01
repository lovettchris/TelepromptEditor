using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Teleprompter
{
    class TranscriptModel : INotifyPropertyChanged
    {
        string fileName;
        TranscriptEntry selected;
        bool dirty;
        List<TranscriptEntry> _data; // the unfiltered data.
        ObservableCollection<TranscriptEntry> _filtered = new ObservableCollection<TranscriptEntry>();

        public string FileName {  get { return this.fileName; } }

        public ObservableCollection<TranscriptEntry> Entries
        {
            get { return _filtered; }
            set { _filtered = value; NotifyChanged("Entries"); }
        }

        public bool Dirty
        {
            get { return dirty; }
            set { this.dirty = value; NotifyChanged("Dirty"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        internal TranscriptEntry FindEntry(double seconds)
        {
            // binary search for item containing this time.
            if (_filtered.Count == 0)
            {
                return null;
            }

            return BinaryFind(seconds, 0, _filtered.Count);
        }

        private TranscriptEntry BinaryFind(double seconds, int start, int end)
        {
            if (_filtered.Count == 0)
            {
                return null;
            }
            if (start == end)
            {
                return _filtered[start];
            }
            if (start + 1 == end)
            {
                var e = _filtered[start];
                if (e.Contains(seconds))
                {
                    return e;
                }
                if (end < _filtered.Count)
                {
                    var f = _filtered[end];
                    if (f.Contains(seconds))
                    {
                        return f;
                    }
                }
            }
            else
            {
                int mid = (start + end) / 2;
                var x = _filtered[mid];
                if (seconds < x.StartSeconds)
                {
                    // must be in first half then.
                    return BinaryFind(seconds, start, mid);
                }

                // try the second half
                return BinaryFind(seconds, mid, end);
            }

            return null;
        }

        public void LoadSrt(string fileName, double width)
        {
            this.fileName = fileName;
            string[] lines = System.IO.File.ReadAllLines(fileName);
            var entries = new List<TranscriptEntry>();
            var pos = 0;
            var count = lines.Length;

            while (pos < count)
            {
                var index = int.Parse(lines[pos++]);
                if (pos >= count)
                {
                    break;
                }
                var range = lines[pos++].Trim();
                if (pos >= count)
                {
                    break;
                }
                // consume the entire prompt up to next blank line.
                var prompt = lines[pos++].Trim();
                while (pos < count && !string.IsNullOrEmpty(lines[pos].Trim()))
                {
                    prompt += '\n' + lines[pos++].Trim();
                }

                // skip blank lines.
                while (pos < count && string.IsNullOrEmpty(lines[pos].Trim()))
                {
                    pos++;
                }

                var e = new TranscriptEntry(index, range, prompt) { Width = width };
                e.PropertyChanged += OnEntryChanged;
                entries.Add(e);
            }

            this._data = entries;

            this.Entries.Clear();
            foreach (var e in entries)
            {
                this.Entries.Add(e);
            }

            this.Dirty = false;
            selected = null;
        }

        private void OnEntryChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Prompt")
            {
                this.Dirty = true;
            }
        }

        internal void Save(string filename)
        {
            using (StreamWriter writer = new StreamWriter(filename, false, Encoding.UTF8))
            {
                Save(writer);
            }
            this.fileName = filename;
            this.Dirty = false;
        }

        internal void Save(TextWriter writer)
        { 
            int index = 1;
            foreach(var e in this._filtered)
            {
                writer.Write(index.ToString());
                writer.Write('\n');
                writer.Write(e.Range);
                writer.Write('\n');
                writer.Write(e.Prompt);
                writer.Write('\n'); 
                writer.Write('\n');
                index++;
            }
        }

        internal string CopyText()
        {
            using (StringWriter w = new StringWriter())
            {
                Save(w);
                return w.ToString();
            }
        }

        internal void Select(TranscriptEntry entry)
        {
            if (selected != entry)
            {
                if (selected != null)
                {
                    selected.Selected = false;
                }
                selected = entry;
                if (entry != null)
                {
                    entry.Selected = true;
                }
            }
        }

        internal void SetFilter(string filter)
        {
            this.Entries.Clear();
            foreach (var e in _data)
            {
                if (e.Prompt != null && e.Prompt.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    this.Entries.Add(e);
                }
            }
        }
    }
}
