using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MiniPacman
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double x = 100;
        private double y = 100;
        private readonly double radius = 30;
        private Vector direction = new Vector(1, 0); // initially naar rechts
        private double speed = 220; // pixels per seconde


        // shape
        private Path pacmanPath;


        // input
        private Vector inputDir = new Vector(0, 0);


        // timing
        private Stopwatch stopwatch = new Stopwatch();
        private long lastTicks = 0;
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Zorg dat canvas de volledige client size gebruikt
            GameCanvas.Width = ActualWidth;
            GameCanvas.Height = ActualHeight;


            SizeChanged += (s, ev) =>
            {
                GameCanvas.Width = ActualWidth;
                GameCanvas.Height = ActualHeight;
            };


            // Maak Pacman shape
            pacmanPath = new Path { Fill = Brushes.Gold, Stroke = Brushes.Orange, StrokeThickness = 1 };
            GameCanvas.Children.Add(pacmanPath);


            // start timer
            stopwatch.Start();
            lastTicks = stopwatch.ElapsedTicks;
            CompositionTarget.Rendering += CompositionTarget_Rendering;


            // initial draw
            UpdatePacmanShape(0);
        }


        private void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            long now = stopwatch.ElapsedTicks;
            double dt = (now - lastTicks) / (double)Stopwatch.Frequency;
            lastTicks = now;


            // If there's input, update direction (instant turn)
            if (inputDir.Length > 0.001)
            {
                direction = inputDir;
                direction.Normalize();
            }


            // Move
            x += direction.X * speed * dt;
            y += direction.Y * speed * dt;


            // Keep Pacman inside the canvas (wrap-around)
            double w = Math.Max(1, GameCanvas.ActualWidth);
            double h = Math.Max(1, GameCanvas.ActualHeight);


            if (x < -radius) x = w + radius;
            if (x > w + radius) x = -radius;
            if (y < -radius) y = h + radius;
            if (y > h + radius) y = -radius;


            UpdatePacmanShape(stopwatch.Elapsed.TotalSeconds);
        }

        private void UpdatePacmanShape(double timeSeconds)
        {
            // mouth oscillation: tussen ~10 en ~70 graden
            double mouthBase = 20.0; // minimale helft van de mondhoek (in graden)
            double mouthAmplitude = 35.0; // extra oscillatie
            double mouth = mouthBase + Math.Abs(Math.Sin(timeSeconds * 8.0)) * mouthAmplitude; // in degrees (half-angle)


            // richting in graden (0 = rechts, 90 = onder op scherm y-as)
            double angle = Math.Atan2(direction.Y, direction.X); // radians
            double angleDeg = angle * 180.0 / Math.PI;


            // bouw pad: een 'taartpunt' (pie) met center -> start -> arc -> center
            Point center = new Point(x, y);
            double startAngle = angle - DegreeToRadian(mouth);
            double endAngle = angle + DegreeToRadian(mouth);


            Point p1 = PointOnCircle(center, radius, startAngle);
            Point p2 = PointOnCircle(center, radius, endAngle);


            bool largeArc = (2 * mouth) > 180.0; // meestal false here


            // Maak geometry
            var fig = new PathFigure { StartPoint = center, IsClosed = true };
            fig.Segments.Add(new LineSegment(p1, true));
            fig.Segments.Add(new ArcSegment
            {
                Point = p2,
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = largeArc
            });
            fig.Segments.Add(new LineSegment(center, true));


            var geo = new PathGeometry();
            geo.Figures.Add(fig);


            pacmanPath.Data = geo;


            // plaats stroke/rough offset
            Canvas.SetLeft(pacmanPath, 0);
            Canvas.SetTop(pacmanPath, 0);
        }


        private static Point PointOnCircle(Point center, double r, double angleRadians)
        {
            return new Point(center.X + r * Math.Cos(angleRadians), center.Y + r * Math.Sin(angleRadians));
        }


        private static double DegreeToRadian(double deg) => deg * Math.PI / 180.0;


        // Input handlers: pijltoetsen of WASD
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                case Key.A:
                    inputDir.X = -1; break;
                case Key.Right:
                case Key.D:
                    inputDir.X = 1; break;
                case Key.Up:
                case Key.W:
                    inputDir.Y = -1; break;
                case Key.Down:
                case Key.S:
                    inputDir.Y = 1; break;
            }


            // normalize diagonal input
            if (Math.Abs(inputDir.X) > 0.5 && Math.Abs(inputDir.Y) > 0.5)
            {
                inputDir.Normalize();
            }
        }


        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                case Key.A:
                    if (inputDir.X < 0) inputDir.X = 0; break;
                case Key.Right:
                case Key.D:
                    if (inputDir.X > 0) inputDir.X = 0; break;
                case Key.Up:
                case Key.W:
                    if (inputDir.Y < 0) inputDir.Y = 0; break;
                case Key.Down:
                case Key.S:
                    if (inputDir.Y > 0) inputDir.Y = 0; break;
            }
        }
    }
}
