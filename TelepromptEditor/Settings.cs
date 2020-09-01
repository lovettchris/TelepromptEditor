using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Teleprompter
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    public class Settings : INotifyPropertyChanged
    {
        const string SettingsFileName = "settings.xml";
        string videoFileName;
        string srtFileName;
        double position;
        Point windowLocation;
        Size windowSize;
        AppTheme theme = AppTheme.Dark;

        static Settings _instance;

        public Settings()
        {
            _instance = this;
        }

        public static string SettingsFolder
        {
            get
            {
                string appSetttingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\TelepromptEditor");
                Directory.CreateDirectory(appSetttingsPath);
                return appSetttingsPath;
            }
        }

        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    return new Settings();
                }
                return _instance;
            }
        }

        public Point WindowLocation
        {
            get { return this.windowLocation; }
            set { this.windowLocation = value; }
        }

        public Size WindowSize
        {
            get { return this.windowSize; }
            set { this.windowSize = value; }
        }

        public AppTheme Theme
        {
            get { return this.theme; }
            set { this.theme = value; }
        }

        public string VideoFile
        {
            get
            {
                return this.videoFileName;
            }
            set
            {
                if (this.videoFileName != value)
                {
                    this.videoFileName = value;
                    OnPropertyChanged("VideFile");
                }
            }
        }

        public string SrtFile
        {
            get
            {
                return this.srtFileName;
            }
            set
            {
                if (this.srtFileName != value)
                {
                    this.srtFileName = value;
                    OnPropertyChanged("SrtFile");
                }
            }
        }

        public double Position
        {
            get
            {
                return this.position;
            }
            set
            {
                if (this.position != value)
                {
                    this.position = value;
                    OnPropertyChanged("Position");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                UiDispatcher.RunOnUIThread(() =>
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                });
            }
        }

        public static async Task<Settings> LoadAsync()
        {
            var store = new IsolatedStorage<Settings>();
            Settings result = null;
            try
            {
                Debug.WriteLine("Loading settings from : " + SettingsFolder);
                result = await store.LoadFromFileAsync(SettingsFolder, SettingsFileName);
            }
            catch
            {
            }
            if (result == null)
            {
                result = new Settings();
                await result.SaveAsync();
            }
            return result;
        }

        bool saving;

        public async Task SaveAsync()
        {
            var store = new IsolatedStorage<Settings>();
            if (!saving)
            {
                saving = true;
                try
                {
                    Debug.WriteLine("Saving settings to : " + SettingsFolder);
                    await store.SaveToFileAsync(SettingsFolder, SettingsFileName, this);
                }
                finally
                {
                    saving = false;
                }
            }
        }

    }


}
