using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace MiniPacman
{
    public partial class MainWindow : Window
    {
        // Pacman
        private PacmanPlayer pacman;
        private Vector inputDir = new Vector(0, 0);

        // Timing
        private Stopwatch stopwatch = new Stopwatch();
        private long lastTicks = 0;

        // SerialPort
        private SerialPort serialPort;

        // Rode cirkels
        private List<Ellipse> redCircles = new List<Ellipse>();
        private const int RedCircleRadius = 20;
        private const int RedCircleCount = 10;

        // Groene vlag
        private const double GreenFlagRadius = 20;
        private Ellipse greenFlag;

        private bool gameOver = false;

        private const double PacmanRadius = 30;
        private const double PacmanSpeed = 220;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GameCanvas.Width = ActualWidth;
            GameCanvas.Height = ActualHeight;

            SizeChanged += (s, ev) =>
            {
                GameCanvas.Width = ActualWidth;
                GameCanvas.Height = ActualHeight;
            };

            // Pacman instantie
            pacman = new PacmanPlayer(PacmanRadius, PacmanRadius, PacmanRadius, PacmanSpeed);
            GameCanvas.Children.Add(pacman.PacmanShape);

            stopwatch.Start();
            lastTicks = stopwatch.ElapsedTicks;
            CompositionTarget.Rendering += CompositionTarget_Rendering;

            InitializeSerialPort("COM4", 9600);

            // Rode cirkels + groene vlag
            CreateRedCircles();
            CreateGreenFlag();
        }

        private void InitializeSerialPort(string portName, int baudRate)
        {
            try
            {
                serialPort = new SerialPort(portName, baudRate)
                {
                    Parity = Parity.None,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Encoding = Encoding.ASCII,
                    NewLine = "\n",
                    ReadTimeout = 500
                };
                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.Open();
                Debug.WriteLine($"Serial port {portName} opened at {baudRate} baud.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open serial port {portName}: {ex.Message}");
                MessageBox.Show($"Unable to open {portName}: {ex.Message}", "Serial Port Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                serialPort = null;
            }
        }

        private void SerialPort_DataReceived(object? sender, SerialDataReceivedEventArgs e)
        {
            if (serialPort == null) return;

            try
            {
                string data = serialPort.ReadExisting();
                if (string.IsNullOrEmpty(data)) return;

                foreach (char ch in data)
                {
                    if (char.IsWhiteSpace(ch)) continue;

                    Vector newInput = new Vector(0, 0);
                    switch (ch)
                    {
                        case 'U': newInput = new Vector(0, -1); break;
                        case 'R': newInput = new Vector(1, 0); break;
                        case 'D': newInput = new Vector(0, 1); break;
                        case 'L': newInput = new Vector(-1, 0); break;
                        case '0': newInput = new Vector(0, 0); break;
                        default: continue;
                    }

                    Dispatcher.BeginInvoke(() =>
                    {
                        inputDir = newInput;
                        if (inputDir.Length > 0.001) inputDir.Normalize();
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading serial port: {ex.Message}");
            }
        }

        private void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            if (gameOver) return;

            long now = stopwatch.ElapsedTicks;
            double dt = (now - lastTicks) / (double)Stopwatch.Frequency;
            lastTicks = now;

            pacman.Move(inputDir, dt);
            pacman.KeepInsideCanvas(GameCanvas.ActualWidth, GameCanvas.ActualHeight);
            pacman.UpdateShape(stopwatch.Elapsed.TotalSeconds);

            CheckRedCollisions();
            CheckGreenFlag();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                case Key.A: inputDir.X = -1; break;
                case Key.Right:
                case Key.D: inputDir.X = 1; break;
                case Key.Up:
                case Key.W: inputDir.Y = -1; break;
                case Key.Down:
                case Key.S: inputDir.Y = 1; break;
            }
            if (Math.Abs(inputDir.X) > 0.5 && Math.Abs(inputDir.Y) > 0.5) inputDir.Normalize();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                case Key.A: if (inputDir.X < 0) inputDir.X = 0; break;
                case Key.Right:
                case Key.D: if (inputDir.X > 0) inputDir.X = 0; break;
                case Key.Up:
                case Key.W: if (inputDir.Y < 0) inputDir.Y = 0; break;
                case Key.Down:
                case Key.S: if (inputDir.Y > 0) inputDir.Y = 0; break;
            }
        }

        // --------------------- Rode cirkels ---------------------
        private void CreateRedCircles()
        {
            Random rnd = new Random();
            redCircles.Clear();
            int attempts = 0;
            const int MaxAttempts = 2000;

            while (redCircles.Count < RedCircleCount && attempts < MaxAttempts)
            {
                attempts++;
                double cx = rnd.Next(50, (int)(GameCanvas.ActualWidth - 50));
                double cy = rnd.Next(50, (int)(GameCanvas.ActualHeight - 50));

                bool valid = true;

                foreach (var existing in redCircles)
                {
                    double ex = Canvas.GetLeft(existing) + RedCircleRadius;
                    double ey = Canvas.GetTop(existing) + RedCircleRadius;
                    double dist = Math.Sqrt((cx - ex) * (cx - ex) + (cy - ey) * (cy - ey));
                    if (dist < 2 * PacmanRadius) { valid = false; break; }
                }

                double distStart = Math.Sqrt((cx - pacman.X) * (cx - pacman.X) + (cy - pacman.Y) * (cy - pacman.Y));
                if (distStart < 3 * PacmanRadius) valid = false;

                if (!valid) continue;

                Ellipse circle = new Ellipse
                {
                    Width = RedCircleRadius * 2,
                    Height = RedCircleRadius * 2,
                    Fill = Brushes.Red
                };
                Canvas.SetLeft(circle, cx - RedCircleRadius);
                Canvas.SetTop(circle, cy - RedCircleRadius);
                redCircles.Add(circle);
                GameCanvas.Children.Add(circle);
            }
        }

        private void CheckRedCollisions()
        {
            foreach (var red in redCircles)
            {
                double rx = Canvas.GetLeft(red) + RedCircleRadius;
                double ry = Canvas.GetTop(red) + RedCircleRadius;
                double dist = Math.Sqrt((pacman.X - rx) * (pacman.X - rx) + (pacman.Y - ry) * (pacman.Y - ry));
                if (dist < pacman.Radius + RedCircleRadius)
                {
                    gameOver = true;
                    MessageBox.Show("Game Over! Je raakte een rode cirkel.", "Game Over", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
        }

        // --------------------- Groene vlag ---------------------
        private void CreateGreenFlag()
        {
            if (greenFlag != null) GameCanvas.Children.Remove(greenFlag);

            greenFlag = new Ellipse
            {
                Width = GreenFlagRadius * 2,
                Height = GreenFlagRadius * 2,
                Fill = Brushes.Green
            };

            Random rnd = new Random();
            double fx = 0, fy = 0;
            int attempts = 0;
            const int MaxAttempts = 500;

            while (true && attempts < MaxAttempts)
            {
                attempts++;
                fx = rnd.Next((int)(GameCanvas.ActualWidth / 2), (int)(GameCanvas.ActualWidth - GreenFlagRadius));
                fy = rnd.Next((int)(GameCanvas.ActualHeight / 2), (int)(GameCanvas.ActualHeight - GreenFlagRadius));

                bool valid = true;
                foreach (var red in redCircles)
                {
                    double rx = Canvas.GetLeft(red) + RedCircleRadius;
                    double ry = Canvas.GetTop(red) + RedCircleRadius;
                    double dist = Math.Sqrt((fx - rx) * (fx - rx) + (fy - ry) * (fy - ry));
                    if (dist < 2 * PacmanRadius) { valid = false; break; }
                }
                if (valid) break;
            }

            Canvas.SetLeft(greenFlag, fx - GreenFlagRadius);
            Canvas.SetTop(greenFlag, fy - GreenFlagRadius);
            GameCanvas.Children.Add(greenFlag);
        }

        private void CheckGreenFlag()
        {
            if (greenFlag == null) return;

            double fx = Canvas.GetLeft(greenFlag) + GreenFlagRadius;
            double fy = Canvas.GetTop(greenFlag) + GreenFlagRadius;
            double dist = Math.Sqrt((pacman.X - fx) * (pacman.X - fx) + (pacman.Y - fy) * (pacman.Y - fy));

            if (dist < pacman.Radius + GreenFlagRadius)
            {
                gameOver = true;
                MessageBox.Show("Gefeliciteerd! Je hebt de vlag bereikt!", "Win", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // --------------------- Restart ---------------------
        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            gameOver = false;
            pacman.X = PacmanRadius;
            pacman.Y = PacmanRadius;
            inputDir = new Vector(0, 0);

            foreach (var red in redCircles) GameCanvas.Children.Remove(red);
            redCircles.Clear();
            if (greenFlag != null) GameCanvas.Children.Remove(greenFlag);

            CreateRedCircles();
            CreateGreenFlag();
        }
    }
}
