using System;
using System.Collections.Generic; // Added for List
using System.IO;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls; // Added for ComboBox
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;

namespace KefuTimer
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private TimeSpan _timeRemaining;
        private TimeSpan _initialTime;
        private bool _timerRunning;
        private bool _isOvertime = false; // Add this line
        private MediaPlayer _mediaPlayer;
        private MediaPlayer _timeUpMediaPlayer;
        private List<string> _bgmFiles = new List<string>(); // Store full paths
        private List<string> _chimeFiles = new List<string>(); // Store full paths
        private double _initialWidth;
        private double _initialHeight;
        private double _initialFontSize;

        public MainWindow()
        {
            InitializeComponent();
            _initialTime = TimeSpan.FromMinutes(5);
            _timeRemaining = _initialTime;
            UpdateTimerDisplay();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;

            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

            _timeUpMediaPlayer = new MediaPlayer();

            this.Closing += MainWindow_Closing;

            LoadAndBindAudioFiles();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Set the minimum size to the initial size
            this.MinWidth = this.ActualWidth;
            this.MinHeight = this.ActualHeight;

            // Store initial values for scaling
            _initialWidth = this.ActualWidth;
            _initialHeight = this.ActualHeight;
            _initialFontSize = TimerDisplay.FontSize;

            // Stop auto-sizing to prevent feedback loops.
            this.SizeToContent = SizeToContent.Manual;
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_initialWidth > 0) // Ensure initial values are set
            {
                // Calculate the ratio of size change from the initial size
                double widthRatio = e.NewSize.Width / _initialWidth;
                double heightRatio = e.NewSize.Height / _initialHeight;

                // Use the smaller ratio to ensure the text fits within the new window dimensions
                double ratio = Math.Min(widthRatio, heightRatio);

                // Calculate the new font size
                double newFontSize = _initialFontSize * ratio;

                // Set the new font size, ensuring it's a positive value
                TimerDisplay.FontSize = newFontSize > 1 ? newFontSize : 1;
            }
        }

        private void LoadAndBindAudioFiles()
        {
            string baseDirectory = GetApplicationBaseDirectory();

            // Load BGM files
            string bgmDirectory = Path.Combine(baseDirectory, "Music", "BGM");
            _bgmFiles = LoadAudioFiles(bgmDirectory);
            BGMComboBox.ItemsSource = _bgmFiles.Select(Path.GetFileName).ToList();
            if (_bgmFiles.Any())
            {
                BGMComboBox.SelectedIndex = 0;
                _mediaPlayer.Open(new Uri(_bgmFiles[0]));
            }

            // Load Time-Up Sound files
            string chimeDirectory = Path.Combine(baseDirectory, "Music", "CHIME");
            _chimeFiles = LoadAudioFiles(chimeDirectory);
            TimeUpComboBox.ItemsSource = _chimeFiles.Select(Path.GetFileName).ToList();
            if (_chimeFiles.Any())
            {
                TimeUpComboBox.SelectedIndex = 0;
                _timeUpMediaPlayer.Open(new Uri(_chimeFiles[0]));
            }
        }

        private string GetApplicationBaseDirectory()
        {
            return AppContext.BaseDirectory;
        }

        private List<string> LoadAudioFiles(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                return Directory.EnumerateFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly)
                                      .Where(s => s.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                                                  s.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                                                  s.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
                                      .OrderBy(s => s)
                                      .ToList();
            }
            return new List<string>();
        }

        private void BGMComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BGMComboBox.SelectedIndex >= 0 && BGMComboBox.SelectedIndex < _bgmFiles.Count)
            {
                string selectedFile = _bgmFiles[BGMComboBox.SelectedIndex];
                _mediaPlayer.Open(new Uri(selectedFile));
                if (_timerRunning) // If timer is running, immediately play the new song
                {
                    _mediaPlayer.Play();
                }
            }
        }

        private void TimeUpComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TimeUpComboBox.SelectedIndex >= 0 && TimeUpComboBox.SelectedIndex < _chimeFiles.Count)
            {
                string selectedFile = _chimeFiles[TimeUpComboBox.SelectedIndex];
                _timeUpMediaPlayer.Open(new Uri(selectedFile));
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Close();
            _timeUpMediaPlayer.Stop();
            _timeUpMediaPlayer.Close();
        }

        private void MediaPlayer_MediaEnded(object? sender, EventArgs e)
        {
            _mediaPlayer.Position = TimeSpan.Zero;
            _mediaPlayer.Play();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            // Normal countdown
            if (_timeRemaining.TotalSeconds > 0 && !_isOvertime)
            {
                _timeRemaining = _timeRemaining.Subtract(TimeSpan.FromSeconds(1));
                if (_initialTime.TotalSeconds > 0)
                {
                    taskbarItemInfo.ProgressValue = _timeRemaining.TotalSeconds / _initialTime.TotalSeconds;
                }
            }
            // Time's up or overtime logic
            else
            {
                // Transition to overtime
                if (!_isOvertime)
                {
                    PlayTimeUpSound(); // Play sound at the moment it hits zero
                    if (OvertimeModeCheckBox.IsChecked == true)
                    {
                        _isOvertime = true;
                        _mediaPlayer.Stop(); // Stop BGM when entering overtime
                        TimerDisplay.Foreground = Brushes.Red;
                    }
                    else
                    {
                        StopTimer(resetContent: true);
                        return;
                    }
                }

                // Overtime countdown
                if (_isOvertime)
                {
                    if (_timeRemaining.TotalMinutes < 99 || (_timeRemaining.TotalMinutes == 99 && _timeRemaining.Seconds < 59))
                    {
                        _timeRemaining = _timeRemaining.Add(TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        StopTimer(resetContent: true);
                    }
                }
            }
            UpdateTimerDisplay();
        }

        private void StopTimer(bool resetContent)
        {
            _timer.Stop();
            _timerRunning = false;
            _mediaPlayer.Stop();
            if (resetContent)
            {
                StartPauseButton.Content = "開始";
            }
            taskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
        }

        private void PlayTimeUpSound()
        {
            if (_timeUpMediaPlayer.Source != null)
            {
                _timeUpMediaPlayer.Stop();
                _timeUpMediaPlayer.Position = TimeSpan.Zero;
                _timeUpMediaPlayer.Play();
            }
            else
            {
                SystemSounds.Exclamation.Play();
            }
        }


        private void UpdateTimerDisplay()
        {
            if (_isOvertime)
            {
                TimerDisplay.Text = "+" + _timeRemaining.ToString(@"mm\:ss");
            }
            else
            {
                TimerDisplay.Text = _timeRemaining.ToString(@"mm\:ss");
            }
        }

        private void StartPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_timerRunning)
            {
                _timer.Stop();
                _mediaPlayer.Pause();
                StartPauseButton.Content = "開始";
                taskbarItemInfo.ProgressState = TaskbarItemProgressState.Paused;
            }
            else
            {
                if (_initialTime.TotalSeconds <= 0 && !_isOvertime)
                {
                    return; // Do not start if time is zero and not in overtime
                }
                _timer.Start();
                if (_mediaPlayer.Source != null && !_isOvertime) // Don't restart BGM in overtime
                {
                    _mediaPlayer.Play();
                }
                StartPauseButton.Content = "一時停止";
                taskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            }
            _timerRunning = !_timerRunning;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            StopTimer(resetContent: true);
            _isOvertime = false;
            TimerDisplay.Foreground = SystemColors.ControlTextBrush; // Reset to default color
            _timeRemaining = _initialTime;
            UpdateTimerDisplay();
        }

        private void SetTimeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_timerRunning)
            {
                MessageBox.Show("タイマー実行中は時間を変更できません。", "KefuTimer", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (int.TryParse(MinutesInput.Text, out int minutes) &&
                int.TryParse(SecondsInput.Text, out int seconds))
            {
                if (minutes >= 0 && seconds >= 0 && seconds < 60)
                {
                    _initialTime = new TimeSpan(0, minutes, seconds);
                    ResetToInitialTime();
                }
                else
                {
                    MessageBox.Show("無効な時間です。分は0以上、秒は0から59の範囲で入力してください。", "KefuTimer", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("無効な入力です。分と秒には数値を入力してください。", "KefuTimer", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (_timerRunning)
            {
                MessageBox.Show("タイマー実行中は時間を変更できません。", "KefuTimer", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (sender is Button button && int.TryParse(button.Tag.ToString(), out int minutes))
            {
                _initialTime = new TimeSpan(0, minutes, 0);
                MinutesInput.Text = minutes.ToString("D2");
                SecondsInput.Text = "00";
                ResetToInitialTime();
            }
        }
        
        private void ResetToInitialTime()
        {
            StopTimer(resetContent: true);
            _isOvertime = false;
            TimerDisplay.Foreground = SystemColors.ControlTextBrush;
            _timeRemaining = _initialTime;
            UpdateTimerDisplay();
        }

        private void UpdateMinutes(int change)
        {
            if (int.TryParse(MinutesInput.Text, out int minutes))
            {
                minutes += change;
                if (minutes < 0) minutes = 0;
                MinutesInput.Text = minutes.ToString("D2");
            }
        }

        private void UpdateSeconds(int change)
        {
            if (int.TryParse(SecondsInput.Text, out int seconds))
            {
                seconds += change;
                if (seconds >= 60)
                {
                    UpdateMinutes(1);
                    seconds = 0;
                }
                else if (seconds < 0)
                {
                    UpdateMinutes(-1);
                    seconds = 59;
                }
                SecondsInput.Text = seconds.ToString("D2");
            }
        }

        private void MinutesIncrement_Click(object sender, RoutedEventArgs e) => UpdateMinutes(1);
        private void MinutesDecrement_Click(object sender, RoutedEventArgs e) => UpdateMinutes(-1);
        private void SecondsIncrement_Click(object sender, RoutedEventArgs e) => UpdateSeconds(1);
        private void SecondsDecrement_Click(object sender, RoutedEventArgs e) => UpdateSeconds(-1);

        private void AlwaysOnTopCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
        }

        private void AlwaysOnTopCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
        }

        private void MuteBGMCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.IsMuted = true;
            }
        }

        private void MuteBGMCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.IsMuted = false;
            }
        }
    }
}
