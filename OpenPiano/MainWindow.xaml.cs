using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using OpenPiano.Audio;

namespace OpenPiano;

public sealed partial class MainWindow : Window
{
    // Two octaves starting at C4 (MIDI 60).
    private const int FirstNote = 60;
    private const int Octaves = 2;
    private const double WhiteW = 56, WhiteH = 220, BlackW = 34, BlackH = 135;
    private static readonly int[] WhiteOffsets = { 0, 2, 4, 5, 7, 9, 11 };
    private static readonly (int Offset, int AfterWhiteIndex)[] BlackKeys =
        { (1, 0), (3, 1), (6, 3), (8, 4), (10, 5) };
    private static readonly string[] NoteNames =
        { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    private readonly PianoEngine _engine = new();
    private readonly Dictionary<uint, int> _activePointers = new(); // pointerId -> midi note

    public MainWindow()
    {
        InitializeComponent();
        BuildKeyboard();
        Closed += (_, _) => _engine.Dispose();
    }

    private void BuildKeyboard()
    {
        // White keys first, then black keys so they render on top.
        for (int o = 0; o < Octaves; o++)
            for (int i = 0; i < WhiteOffsets.Length; i++)
                AddKey(FirstNote + o * 12 + WhiteOffsets[i],
                       (o * 7 + i) * WhiteW, WhiteW, WhiteH);

        for (int o = 0; o < Octaves; o++)
            foreach (var (offset, after) in BlackKeys)
                AddKey(FirstNote + o * 12 + offset,
                       (o * 7 + after + 1) * WhiteW - BlackW / 2, BlackW, BlackH);

        KeyboardCanvas.Width = Octaves * 7 * WhiteW;
        KeyboardCanvas.Height = WhiteH;
    }

    private static bool IsBlack(int note) => note % 12 is 1 or 3 or 6 or 8 or 10;

    private static string NoteName(int note) => NoteNames[note % 12] + (note / 12 - 1);

    private void AddKey(int midiNote, double x, double w, double h)
    {
        bool black = IsBlack(midiNote);

        var key = new Rectangle
        {
            Width = w,
            Height = h,
            Fill = new SolidColorBrush(black ? Colors.Black : Colors.White),
            Stroke = new SolidColorBrush(Colors.Gray),
            StrokeThickness = 1,
            Tag = midiNote,
        };
        Canvas.SetLeft(key, x);
        key.PointerPressed += OnKeyDown;
        key.PointerReleased += OnKeyUp;
        key.PointerCanceled += OnKeyUp;
        key.PointerCaptureLost += OnKeyUp;
        KeyboardCanvas.Children.Add(key);

        // Note name near the bottom of the key; clicks pass through to the key underneath.
        var label = new TextBlock
        {
            Text = NoteName(midiNote),
            Width = w,
            TextAlignment = TextAlignment.Center,
            FontSize = 11,
            Foreground = new SolidColorBrush(black ? Colors.White : Colors.Black),
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(label, x);
        Canvas.SetTop(label, h - 24);
        KeyboardCanvas.Children.Add(label);
    }

    private static void SetKeyColor(Rectangle key, bool down)
    {
        int n = (int)key.Tag;
        key.Fill = new SolidColorBrush(down ? Colors.LightGreen
                                            : (IsBlack(n) ? Colors.Black : Colors.White));
    }

    private void OnKeyDown(object sender, PointerRoutedEventArgs e)
    {
        var key = (Rectangle)sender;
        SetKeyColor(key, true);
        key.CapturePointer(e.Pointer); // keeps multi-touch pointers independent
        int note = (int)key.Tag;
        _activePointers[e.Pointer.PointerId] = note;
        _engine.NoteOn(note);
        e.Handled = true;
    }

    private void OnKeyUp(object sender, PointerRoutedEventArgs e)
    {
        var key = (Rectangle)sender;
        SetKeyColor(key, false);
        if (_activePointers.Remove(e.Pointer.PointerId, out int note))
            _engine.NoteOff(note);
        key.ReleasePointerCaptures();
    }
}