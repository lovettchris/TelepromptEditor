using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Teleprompter.Controls;

namespace Teleprompter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TranscriptModel model = new TranscriptModel();
        DelayedActions delayedActions = new DelayedActions();
        bool syncPositions = true;
        double pendingSeek;
        Settings settings;

        public MainWindow()
        {
            UiDispatcher.Initialize();
            InitializeComponent();
            TranscriptView.ItemsSource = model.Entries;
            UpdateButtons();
            this.Visibility = Visibility.Hidden;
            this.model.PropertyChanged += OnModelChanged;

            Task.Run(async () =>
            {
                this.settings = await Settings.LoadAsync();
                this.settings.PropertyChanged += OnSettingsPropertyChanged;
                UiDispatcher.RunOnUIThread(RestoreSettings);
            });
        }

        private void OnModelChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(model.FileName))
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(model.FileName);
                var title = "TelepromptEditor - " + name;
                if (model.Dirty)
                {
                    title += "*";
                }
                this.Title = title;
            }
        }

        private void RestoreSettings()
        {
            if (settings.WindowLocation.X != 0 && settings.WindowSize.Width != 0 && settings.WindowSize.Height != 0)
            {
                // make sure it is visible on the user's current screen configuration.
                var bounds = new System.Drawing.Rectangle(
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowLocation.X),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowLocation.Y),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowSize.Width),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowSize.Height));
                var screen = System.Windows.Forms.Screen.FromRectangle(bounds);
                bounds.Intersect(screen.WorkingArea);

                this.Left = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.X);
                this.Top = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.Y);
                this.Width = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.Width);
                this.Height = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.Height);
            }

            if (string.IsNullOrEmpty(this.SrtFileName.Text))
            {
                this.SrtFileName.Text = this.settings.SrtFile;
                UpdateSrt();
            }
            if (string.IsNullOrEmpty(this.VideoFileName.Text))
            {
                this.VideoFileName.Text = this.settings.VideoFile;
                UpdateVideoLocation(this.settings.Position);
            }

            this.Visibility = Visibility.Visible;
        }


        private void OnSettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.delayedActions.StartDelayedAction("SaveSettings", async () =>
            {
                try
                {
                    await this.settings.SaveAsync();
                } 
                catch
                {
                }
            }, TimeSpan.FromMilliseconds(250));
        }

        private void OnMediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (e.ErrorException != null)
            {
                ShowError(e.ErrorException.Message);
            }
        }

        private void OnBrowseVideo(object sender, RoutedEventArgs e)
        {
            OpenFileDialog od = new OpenFileDialog();
            od.Filter = "Video Files (*.mp4)|*.mp4|Movie Files (*.mov)|*.mov|All Files (*.*)|*.*";
            od.CheckFileExists = true;
            if (od.ShowDialog() == true)
            {
                ShowError("Loading " + od.FileName);
                VideoFileName.Text = od.FileName; 
                UpdateVideoLocation();
            }
        }

        private void OnBrowseSrt(object sender, RoutedEventArgs e)
        {
            OpenFileDialog od = new OpenFileDialog();
            od.Filter = "SRT Files (*.srt)|*.srt";
            od.CheckFileExists = true;
            if (od.ShowDialog() == true)
            {
                ShowError("Loading " + od.FileName);

                SrtFileName.Text = od.FileName;
                UpdateSrt();
            }
        }
        private void OnSaveSrt(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(model.FileName))
            {
                ShowError("no model loaded");
            }
            else
            {
                model.Save(model.FileName);
                ShowError("Saved " + model.FileName);
            }
        }

        private void OnCopy(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(model.CopyText());
            ShowError("srt text copied to clipboard");
        }

        private void OnSrtFileChanged(object sender, RoutedEventArgs e)
        {
            UpdateSrt();
        }

        private void SrtFileNameKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                UpdateSrt();
            }
        }

        void UpdateSrt()
        {
            string filename = SrtFileName.Text.Trim('"').Trim();
            this.settings.SrtFile = filename;
            if (string.IsNullOrEmpty(filename))
            {
                model.Entries.Clear();
            }
            else
            {
                try
                {
                    model.LoadSrt(filename, TranscriptView.ActualWidth > 15 ? TranscriptView.ActualWidth - 15 : TranscriptView.ActualWidth);
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
            }
        }
        private void VideoFileNameKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                UpdateVideoLocation();
            }
        }


        private void OnVideoFileChanged(object sender, RoutedEventArgs e)
        {
            UpdateVideoLocation();
        }

        void UpdateVideoLocation(double position = 0)
        { 
            if (VideoPlayer == null)
            {
                return;
            }
                       
            try
            {
                ShowError("");

                string url = VideoFileName.Text.Trim('"').Trim();
                this.settings.VideoFile = url;
                if (string.IsNullOrEmpty(url))
                {
                    StopClock();
                    VideoPlayer.Source = null;
                    return;
                }

                var fileName = new Uri(url);
                if (VideoPlayer.Source != fileName)
                {
                    StopClock(); 
                    VideoPlayer.Source = fileName;
                    VideoPlayer.Play();
                    this.pendingSeek = position;
                }
            } 
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void ShowError(string message)
        {
            StatusText.Text = message;
        }

        private void StopClock()
        {
            if (syncClockTimer != null)
            {
                syncClockTimer.Stop();
                syncClockTimer = null;
            }
            if (playing)
            {
                VideoPlayer.Stop();
                playing = false;
            }
            UpdateButtons();
        }

        private void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            StopClock();
            playing = true;
            UpdateButtons();
            try
            {
                programmedSync = true;
                PositionSlider.Maximum = VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                if (this.pendingSeek != 0)
                {
                    VideoPlayer.Position = TimeSpan.FromSeconds(this.pendingSeek);
                    this.pendingSeek = 0;
                }
                PositionSlider.Value = VideoPlayer.Position.TotalSeconds;
            } 
            finally
            {
                programmedSync = false;
            }

            syncClockTimer = new DispatcherTimer(TimeSpan.FromSeconds(0.25), DispatcherPriority.Normal,
                OnSyncClock, this.Dispatcher);
            syncClockTimer.Start();
        }

        private void OnSyncClock(object sender, EventArgs e)
        {
            var seconds = VideoPlayer.Position.TotalSeconds;
            try {
                programmedSync = true;
                PositionSlider.Value = seconds;
            }
            finally
            {
                programmedSync = false;
            }
            var entry = model.FindEntry(seconds);
            model.Select(entry);

            if (syncPositions)
            {
                TranscriptView.ScrollIntoView(entry);
            }
        }

        bool playing;
        DispatcherTimer syncClockTimer;

        private void OnPlay(object sender, RoutedEventArgs e)
        {
            playing = true;
            VideoPlayer.Play(); 
            UpdateButtons();
        }

        private void OnPause(object sender, RoutedEventArgs e)
        {
            playing = false;
            VideoPlayer.Pause();
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            if (playing)
            {
                PlayButton.Visibility = Visibility.Collapsed;
                PauseButton.Visibility = Visibility.Visible;
            }
            else
            {
                PlayButton.Visibility = Visibility.Visible;
                PauseButton.Visibility = Visibility.Collapsed;
            }
        }

        bool programmedSync = false;

        private void OnSliderMoved(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!programmedSync)
            {
                // can't just blindly set the position, because it sets it too often while
                // sliding and the MediaElement can't keep up!
                delayedActions.StartDelayedAction("MovePosition", () =>
                {
                    VideoPlayer.Position = TimeSpan.FromSeconds(e.NewValue);
                }, TimeSpan.FromMilliseconds(100));
            }
        }

        private void OnListSizeChanged(object sender, SizeChangedEventArgs e)
        {
            delayedActions.StartDelayedAction("UpdateTextWidth", OnUpdateTextWidth, TimeSpan.FromMilliseconds(250));
        }

        private bool listScrollerVisible;

        private void OnListScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            bool rc = e.ExtentHeight > e.ViewportHeight;
            if (rc != listScrollerVisible)
            {
                listScrollerVisible = rc;
                delayedActions.StartDelayedAction("UpdateTextWidth", OnUpdateTextWidth, TimeSpan.FromMilliseconds(50));
            }
        }

        private void OnUpdateTextWidth()
        {
            double newWidth = TranscriptView.ActualWidth;
            if (newWidth > 15) newWidth -= 15;
            if (listScrollerVisible && newWidth > 20)
            {
                newWidth -= 20;
            }

            foreach (var entry in this.model.Entries)
            {
                entry.Width = newWidth;
            }
        }

        private void OnMediaEnded(object sender, RoutedEventArgs e)
        {
            StopClock();
        }

        private void OnToggleSync(object sender, RoutedEventArgs e)
        {
            syncPositions = !syncPositions;
        }

        private void OnListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                TranscriptEntry entry = (TranscriptEntry)e.AddedItems[0];
                VideoPlayer.Position = TimeSpan.FromSeconds(entry.StartSeconds);
            }
        }

        private void OnItemEditing(object sender, EventArgs e)
        {
            EditableTextBlock block = (EditableTextBlock)sender;
            TranscriptEntry entry = block.DataContext as TranscriptEntry;
            if (entry != null)
            {
                TranscriptView.SelectedItem = entry;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (model.Dirty)
            {
                var rc = MessageBox.Show("You have unsaved changes, would you like to save the SRT file?", "Unsaved Changed", MessageBoxButton.YesNoCancel);
                if (rc == MessageBoxResult.Yes)
                {
                    this.model.Save(this.model.FileName);
                }
                else if (rc == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            if (this.settings != null)
            {
                this.settings.WindowSize = this.RestoreBounds.Size;
                this.settings.WindowLocation = this.RestoreBounds.TopLeft;
                this.settings.Position = VideoPlayer.Position.TotalSeconds;
                this.delayedActions.CancelDelayedAction("SaveSettings");
                ManualResetEvent evt = new ManualResetEvent(false);
                Task.Run(async () =>
                {
                    await this.settings.SaveAsync();
                    evt.Set();
                });

                evt.WaitOne(5000);
                Debug.WriteLine("Settings saved");
            }

            base.OnClosing(e);
        }

        private void OnQuickFilterValueChanged(object sender, string filter)
        {
            this.delayedActions.StartDelayedAction("Filter", () => { model.SetFilter(filter); }, TimeSpan.FromMilliseconds(250));
        }

        private void OnListViewPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                QuickFilter.FocusTextBox();
            }
        }
    }
}
