using System;
using System.Windows;
using System.Windows.Threading;

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

    public MainWindow()
    {
        InitializeComponent();
        _initialTime = TimeSpan.FromMinutes(5); // Default to 5 minutes
        _timeRemaining = _initialTime;
        UpdateTimerDisplay();
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Timer_Tick;
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
            MessageBox.Show("時間になりました！", "KefuTimer", MessageBoxButton.OK, MessageBoxImage.Information);
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
            StartPauseButton.Content = "開始";
        }
        else
        {
            _timer.Start();
            StartPauseButton.Content = "一時停止";
        }
        _timerRunning = !_timerRunning;
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        _timerRunning = false;
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
}