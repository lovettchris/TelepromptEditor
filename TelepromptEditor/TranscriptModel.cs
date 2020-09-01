﻿using System;
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
        ObservableCollection<TranscriptEntry> _entries = new ObservableCollection<TranscriptEntry>();

        public string FileName {  get { return this.fileName; } }

        public ObservableCollection<TranscriptEntry> Entries
        {
            get { return _entries; }
            set { _entries = value; NotifyChanged("Entries"); }
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
            if (_entries.Count == 0)
            {
                return null;
            }

            return BinaryFind(seconds, 0, _entries.Count);
        }

        private TranscriptEntry BinaryFind(double seconds, int start, int end)
        {
            if (_entries.Count == 0)
            {
                return null;
            }
            if (start == end)
            {
                return _entries[start];
            }
            if (start + 1 == end)
            {
                var e = _entries[start];
                if (e.Contains(seconds))
                {
                    return e;
                }
                if (end < _entries.Count)
                {
                    var f = _entries[end];
                    if (f.Contains(seconds))
                    {
                        return f;
                    }
                }
            }
            else
            {
                int mid = (start + end) / 2;
                var x = _entries[mid];
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

                entries.Add(new TranscriptEntry(index, range, prompt) { Width = width });
            }

            this.Entries.Clear();
            foreach (var e in entries)
            {
                this.Entries.Add(e);
            }

            selected = null;
        }

        internal void Save(string filename)
        {
            using (StreamWriter writer = new StreamWriter(filename, false, Encoding.UTF8))
            {
                Save(writer);
            }
            this.fileName = filename;
        }

        internal void Save(TextWriter writer)
        { 
            int index = 1;
            foreach(var e in this._entries)
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
    }
}