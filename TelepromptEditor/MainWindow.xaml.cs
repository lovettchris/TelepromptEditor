using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Teleprompter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TranscriptModel model = new TranscriptModel();
        DelayedActions delayedActions = new DelayedActions();

        public MainWindow()
        {
            UiDispatcher.Initialize();
            InitializeComponent();
            TranscriptView.ItemsSource = model.Entries;
            UpdateButtons();
        }

        private void OnMediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
        }

        private void OnOpenFile(object sender, RoutedEventArgs e)
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

        private void OnSaveFile(object sender, RoutedEventArgs e)
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
            if (string.IsNullOrEmpty(filename))
            {
                model.Entries.Clear();
            }
            else
            {
                try
                {
                    model.LoadSrt(filename, TranscriptView.ActualWidth - 15);
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

        void UpdateVideoLocation()
        { 
            if (VideoPlayer == null)
            {
                return;
            }
                       
            try
            {
                ShowError("");

                string url = VideoFileName.Text.Trim('"').Trim();
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
            double newWidth = TranscriptView.ActualWidth - 15;
            if (listScrollerVisible)
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
    }
}
