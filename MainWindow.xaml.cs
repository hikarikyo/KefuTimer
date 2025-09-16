using System;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media; // Added for MediaPlayer
using Microsoft.Win32; // Added for OpenFileDialog
using System.IO; // Added for Path.GetFileName
using System.Media; // Added for SystemSounds.Exclamation.Play()
using System.Linq; // Added for LINQ operations

namespace KefuTimer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private DispatcherTimer _timer;
    private TimeSpan _timeRemaining;
    private TimeSpan _initialTime; // Store the initial time set by the user
    private bool _timerRunning;
    private MediaPlayer _mediaPlayer; // Added for BGM playback
    private MediaPlayer _timeUpMediaPlayer; // Added for time-up sound playback

    public MainWindow()
    {
        InitializeComponent();
        _initialTime = TimeSpan.FromMinutes(5); // Default to 5 minutes
        _timeRemaining = _initialTime;
        UpdateTimerDisplay();
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Timer_Tick;

        _mediaPlayer = new MediaPlayer();
        _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded; // Loop BGM
        
        _timeUpMediaPlayer = new MediaPlayer(); // Initialize time-up media player

        this.Closing += MainWindow_Closing; // Dispose MediaPlayers on close

        string projectRoot = GetProjectRootDirectory();

        // Automatic BGM selection
        string bgmDirectory = Path.Combine(projectRoot, "Music", "BGM");
        string? bgmFile = LoadFirstAudioFile(bgmDirectory);
        if (bgmFile != null)
        {
            _mediaPlayer.Open(new Uri(bgmFile));
            BGMFileName.Text = Path.GetFileName(bgmFile);
        }

        // Automatic Time-Up Sound selection
        string chimeDirectory = Path.Combine(projectRoot, "Music", "CHIME");
        string? chimeFile = LoadFirstAudioFile(chimeDirectory);
        if (chimeFile != null)
        {
            _timeUpMediaPlayer.Open(new Uri(chimeFile));
            TimeUpSoundFileName.Text = Path.GetFileName(chimeFile);
        }
    }

    private string GetProjectRootDirectory()
    {
        string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        DirectoryInfo? directory = new DirectoryInfo(currentDirectory);

        while (directory != null && !directory.GetFiles("KefuTimer.csproj").Any() && !directory.GetFiles("KefuTimer.sln").Any())
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;
    }

    private string? LoadFirstAudioFile(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            var audioFiles = Directory.EnumerateFiles(directoryPath,
                                                       "*.*",
                                                       SearchOption.TopDirectoryOnly)
                                      .Where(s => s.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                                                  s.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                                                  s.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
                                      .OrderBy(s => s) // Order by name to get a consistent "first" file
                                      .FirstOrDefault();
            return audioFiles;
        }
        return null;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _mediaPlayer.Stop();
        _mediaPlayer.Close();
        _timeUpMediaPlayer.Stop(); // Stop and close time-up media player
        _timeUpMediaPlayer.Close();
    }

    private void MediaPlayer_MediaEnded(object? sender, EventArgs e)
    {
        _mediaPlayer.Position = TimeSpan.Zero; // Loop BGM
        _mediaPlayer.Play();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_timeRemaining.TotalSeconds > 0)
        {
            _timeRemaining = _timeRemaining.Subtract(TimeSpan.FromSeconds(1));
            UpdateTimerDisplay();
        }
        else
        {
            _timer.Stop();
            _timerRunning = false;
            StartPauseButton.Content = "開始";
            _mediaPlayer.Stop(); // Stop BGM when timer finishes
            
            if (_timeUpMediaPlayer.Source != null)
            {
                _timeUpMediaPlayer.Position = TimeSpan.Zero; // Reset position to play again
                _timeUpMediaPlayer.Play(); // Play time-up sound
            }
            else
            {
                System.Media.SystemSounds.Exclamation.Play(); // Fallback to system sound
            }
        }
    }

    private void UpdateTimerDisplay()
    {
        TimerDisplay.Text = _timeRemaining.ToString(@"mm\:ss");
    }

    private void StartPauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_timerRunning)
        {
            _timer.Stop();
            _mediaPlayer.Pause(); // Pause BGM
            StartPauseButton.Content = "開始";
        }
        else
        {
            _timer.Start();
            if (_mediaPlayer.Source != null)
            {
                _mediaPlayer.Play(); // Play BGM
            }
            StartPauseButton.Content = "一時停止";
        }
        _timerRunning = !_timerRunning;
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        _timerRunning = false;
        _mediaPlayer.Stop(); // Stop BGM
        _timeRemaining = _initialTime; // Reset to the initial set time
        UpdateTimerDisplay();
        StartPauseButton.Content = "開始";
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
                _timeRemaining = _initialTime;
                UpdateTimerDisplay();
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

    private void UpdateMinutes(int change)
    {
        if (int.TryParse(MinutesInput.Text, out int minutes))
        {
            minutes += change;
            if (minutes < 0) minutes = 0; // Don't go below 0 minutes
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

    private void SelectBGMButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Audio files (*.mp3;*.wav;*.ogg)|*.mp3;*.wav;*.ogg|All files (*.*)|*.*";
        if (openFileDialog.ShowDialog() == true)
        {
            _mediaPlayer.Open(new Uri(openFileDialog.FileName));
            BGMFileName.Text = Path.GetFileName(openFileDialog.FileName);
        }
    }

    private void SelectTimeUpSoundButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Audio files (*.mp3;*.wav;*.ogg)|*.mp3;*.wav;*.ogg|All files (*.*)|*.*";
        if (openFileDialog.ShowDialog() == true)
        {
            _timeUpMediaPlayer.Open(new Uri(openFileDialog.FileName));
            TimeUpSoundFileName.Text = Path.GetFileName(openFileDialog.FileName);
        }
    }

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