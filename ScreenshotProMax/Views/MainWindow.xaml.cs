using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenshotProMax.Models;
using ScreenshotProMax.Services;
using ScreenshotProMax.ViewModels;
using MahApps.Metro.Controls;
using System.Windows.Controls.Primitives;

namespace ScreenshotProMax.Views;

public partial class MainWindow : MetroWindow
{
	private AnnotationModel? _activeAnnotation;
	private bool _isDrawing;
	private bool _isDragging;
	private bool _isResizing;
	private bool _isErasing;
	private Point _lastMousePosition;
	private HotkeyService? _hotkeyService;
	private Cursor? _eraserCursor;

	private MainViewModel ViewModel => (MainViewModel)DataContext;

	public MainWindow()
	{
		InitializeComponent();
		Loaded += MainWindow_Loaded;
		Closed += MainWindow_Closed;
		PreviewKeyDown += MainWindow_PreviewKeyDown;
		CreateEraserCursor();

		// Find the radio button by name to avoid referencing generated field directly
		var arrow = FindName("ArrowRadio") as RadioButton;
		if (arrow != null)
		{
			arrow.IsChecked = true; // set after initialization so OverlayCanvas exists
		}
	}

	private void CreateEraserCursor()
	{
		// Erstelle einen benutzerdefinierten Radiergummi-Cursor
		try
		{
			// Erstelle einen visuellen Radiergummi
			var drawingVisual = new DrawingVisual();
			using (var drawingContext = drawingVisual.RenderOpen())
			{
				// Zeichne einen Kreis mit X als Radiergummi-Symbol
				var center = new Point(16, 16);
				var radius = 12.0;

				// Äußerer Kreis
				drawingContext.DrawEllipse(
						Brushes.Transparent,
						new Pen(Brushes.Red, 2),
						center,
						radius,
						radius
				);

				// Inneres X
				var offset = radius * 0.5;
				drawingContext.DrawLine(
						new Pen(Brushes.Red, 2),
						new Point(center.X - offset, center.Y - offset),
						new Point(center.X + offset, center.Y + offset)
				);
				drawingContext.DrawLine(
						new Pen(Brushes.Red, 2),
						new Point(center.X + offset, center.Y - offset),
						new Point(center.X - offset, center.Y + offset)
				);
			}

			var renderTargetBitmap = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
			renderTargetBitmap.Render(drawingVisual);

			// Konvertiere zu Cursor (verwende das Zentrum als Hotspot)
			var encoder = new PngBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
			using (var stream = new System.IO.MemoryStream())
			{
				encoder.Save(stream);
				stream.Position = 0;

				// Cursor aus Stream erstellen - Fallback zu Cross wenn es nicht funktioniert
				try
				{
					var iconHandle = System.Runtime.InteropServices.Marshal.GetHINSTANCE(typeof(MainWindow).Module);
					_eraserCursor = Cursors.Cross; // Fallback
				}
				catch
				{
					_eraserCursor = Cursors.Cross;
				}
			}
		}
		catch
		{
			// Fallback auf Cross-Cursor
			_eraserCursor = Cursors.Cross;
		}
	}

	//private System.IO.MemoryStream BitmapToCursor(RenderTargetBitmap bitmap, int hotX, int hotY)
	//{
	//    var encoder = new PngBitmapEncoder();
	//    encoder.Frames.Add(BitmapFrame.Create(bitmap));
	//    var stream = new System.IO.MemoryStream();
	//    encoder.Save(stream);
	//    stream.Position = 0;
	//    return stream;
	//}

	private void MainWindow_Loaded(object sender, RoutedEventArgs e)
	{
		// Register global hotkey Ctrl+D
		_hotkeyService = new HotkeyService();
		var handle = new WindowInteropHelper(this).Handle;

		if (_hotkeyService.RegisterHotkey(handle, ModifierKeys.Control, Key.D))
		{
			_hotkeyService.HotkeyPressed += HotkeyService_HotkeyPressed;
		}
		else
		{
			Console.WriteLine("Hotkey Strg+D konnte nicht registriert werden.");
		}
	}

	private void MainWindow_Closed(object? sender, EventArgs e)
	{
		_hotkeyService?.Dispose();
	}

	private async void HotkeyService_HotkeyPressed(object? sender, EventArgs e)
	{
		// Minimize window before capture
		WindowState = WindowState.Minimized;
		await System.Threading.Tasks.Task.Delay(200);

		// Show region selector
		await ViewModel.CaptureRegionCommand.ExecuteAsync(null);

		// Restore window if capture was successful
		if (ViewModel.HasImage)
		{
			WindowState = WindowState.Normal;
			Activate();
		}
	}

	private async void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		// Strg+C für Copy to Clipboard
		if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control && ViewModel.HasImage)
		{
			await ViewModel.CopyToClipboardCommand.ExecuteAsync(null);
			e.Handled = true;
		}
		// Strg+Z für Undo
		else if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control && ViewModel.CanUndo)
		{
			ViewModel.UndoCommand.Execute(null);
			e.Handled = true;
		}
		// Strg+Y für Redo
		else if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control && ViewModel.CanRedo)
		{
			ViewModel.RedoCommand.Execute(null);
			e.Handled = true;
		}
		// Delete-Taste zum Löschen der ausgewählten Annotation
		else if (e.Key == Key.Delete && ViewModel.SelectedAnnotation != null)
		{
			ViewModel.Annotations.Remove(ViewModel.SelectedAnnotation);
			ViewModel.SelectedAnnotation = null;
			e.Handled = true;
		}
		// Escape zum Abbrechen der Auswahl
		else if (e.Key == Key.Escape)
		{
			ViewModel.DeselectAll();
			e.Handled = true;
		}
	}

	private void OverlayCanvas_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!ViewModel.HasImage)
		{
			return;
		}

		var position = e.GetPosition(OverlayCanvas);

		// Radierer-Werkzeug
		if (ViewModel.CurrentTool == AnnotationType.Eraser)
		{
			_isErasing = true;
			var annotation = ViewModel.FindAnnotationAt(position);
			if (annotation != null)
			{
				ViewModel.EraseAnnotation(annotation);
			}
			return;
		}

		// Auswahl-Werkzeug
		if (ViewModel.CurrentTool == AnnotationType.Selection)
		{
			var selected = ViewModel.SelectAnnotationAt(position);
			if (selected != null)
			{
				_isDragging = true;
				_lastMousePosition = position;

				// Wenn es ein Text-Element ist, fokussiere die TextBox für Bearbeitung
				if (selected.Type == AnnotationType.Text)
				{
					// Gib der UI Zeit, die Auswahl zu verarbeiten, dann fokussiere
					Dispatcher.BeginInvoke(new Action(() =>
					{
						// Finde die TextBox im Visual Tree und fokussiere sie
						var container = FindTextBoxForAnnotation(selected);
						if (container != null)
						{
							container.Focus();
							container.SelectAll();
						}
					}), System.Windows.Threading.DispatcherPriority.Loaded);
				}

				// Prüfe ob auf Resize-Handle geklickt wurde (nicht für Text-Elemente)
				if (selected.Type != AnnotationType.Text)
				{
					var bounds = selected.GetBounds();
					var handleRect = new Rect(
							bounds.Right - 5,
							bounds.Bottom - 5,
							10,
							10
					);

					if (handleRect.Contains(position))
					{
						_isResizing = true;
						_isDragging = false;
						Mouse.Capture(OverlayCanvas);
					}
					else
					{
						Mouse.Capture(OverlayCanvas);
					}
				}
				else
				{
					Mouse.Capture(OverlayCanvas);
				}
			}
			else
			{
				ViewModel.DeselectAll();
			}
			return;
		}

		// Normale Zeichenwerkzeuge
		_activeAnnotation = ViewModel.BeginAnnotation();
		_activeAnnotation.Points.Add(position);

		switch (ViewModel.CurrentTool)
		{
			case AnnotationType.Line:
			case AnnotationType.Arrow:
			case AnnotationType.Rectangle:
			case AnnotationType.Ellipse:
				_activeAnnotation.Points.Add(position);
				_isDrawing = true;
				break;
			case AnnotationType.Text:
				// Text wird platziert und sofort aktiviert für Bearbeitung
				_isDrawing = false;

				// Automatisch das Text-Element auswählen und TextBox fokussieren
				_activeAnnotation.IsSelected = true;
				ViewModel.SelectedAnnotation = _activeAnnotation;

				// Gib der UI Zeit, das neue Element zu rendern, dann fokussiere
				Dispatcher.BeginInvoke(new Action(() =>
				{
					var textBox = FindTextBoxForAnnotation(_activeAnnotation);
					if (textBox != null)
					{
						textBox.Focus();
						textBox.SelectAll();
					}
				}), System.Windows.Threading.DispatcherPriority.Loaded);
				break;
			case AnnotationType.Number:
				// Number is placed at clicked position
				_isDrawing = false;
				break;
		}
	}

	// Hilfsmethode zum Finden der TextBox für eine Annotation
	private TextBox? FindTextBoxForAnnotation(AnnotationModel annotation)
	{
		// Durchsuche den Visual Tree nach der TextBox, die zu dieser Annotation gehört
		return FindVisualChild<TextBox>(OverlayCanvas, tb =>
		{
			var dataContext = (tb.Parent as FrameworkElement)?.DataContext;
			return dataContext == annotation;
		});
	}

	// Generische Methode zum Durchsuchen des Visual Trees
	private T? FindVisualChild<T>(DependencyObject parent, Func<T, bool>? predicate = null) where T : DependencyObject
	{
		if (parent == null)
			return null;

		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);

			if (child is T typedChild && (predicate == null || predicate(typedChild)))
			{
				return typedChild;
			}

			var result = FindVisualChild(child, predicate);
			if (result != null)
				return result;
		}

		return null;
	}

	private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
	{
		var position = e.GetPosition(OverlayCanvas);

		// Radierer-Werkzeug - Löschen beim Überfahren
		if (_isErasing && e.LeftButton == MouseButtonState.Pressed)
		{
			var annotation = ViewModel.FindAnnotationAt(position);
			if (annotation != null)
			{
				ViewModel.EraseAnnotation(annotation);
			}
			return;
		}

		// Verschieben einer ausgewählten Annotation
		if (_isDragging && ViewModel.SelectedAnnotation != null)
		{
			var offset = position - _lastMousePosition;
			ViewModel.MoveSelectedAnnotation(offset);
			_lastMousePosition = position;
			return;
		}

		// Skalieren einer ausgewählten Annotation
		if (_isResizing && ViewModel.SelectedAnnotation != null)
		{
			var bounds = ViewModel.SelectedAnnotation.GetBounds();
			var center = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);

			var oldDistance = (_lastMousePosition - center).Length;
			var newDistance = (position - center).Length;

			if (oldDistance > 0)
			{
				var scaleFactor = newDistance / oldDistance;
				ViewModel.ScaleSelectedAnnotation(scaleFactor, center);
			}

			_lastMousePosition = position;
			return;
		}

		// Normales Zeichnen
		if (!_isDrawing || _activeAnnotation == null)
		{
			return;
		}

		switch (ViewModel.CurrentTool)
		{
			case AnnotationType.Line:
			case AnnotationType.Arrow:
			case AnnotationType.Rectangle:
			case AnnotationType.Ellipse:
				if (_activeAnnotation.Points.Count >= 2)
				{
					_activeAnnotation.Points[1] = position;
				}
				break;
		}
	}

	private void OverlayCanvas_MouseUp(object sender, MouseButtonEventArgs e)
	{
		_isDrawing = false;
		_isDragging = false;
		_isResizing = false;
		_isErasing = false;
		_activeAnnotation = null;
		Mouse.Capture(null);
	}

	private void OverlayCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
	{
		// Nur zoomen wenn Strg gedrückt ist
		if (Keyboard.Modifiers == ModifierKeys.Control && ViewModel.HasImage)
		{
			if (e.Delta > 0)
			{
				ViewModel.IncreaseZoom();
			}
			else
			{
				ViewModel.DecreaseZoom();
			}
			e.Handled = true;
		}
	}

	private void ToolButton_Click(object sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is string tag && Enum.TryParse<AnnotationType>(tag, out var tool))
		{
			ViewModel.CurrentTool = tool;
			if (tool == AnnotationType.Eraser)
				OverlayCanvas.Cursor = _eraserCursor ?? Cursors.Cross;
			else
				OverlayCanvas.Cursor = Cursors.Arrow;
			if (tool != AnnotationType.Selection)
				ViewModel.DeselectAll();
		}
	}

	// Öffnet das ContextMenu des Mehr-Buttons bei Linksklick
	private void MoreButton_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement fe)
		{
			var cm = fe.ContextMenu;
			if (cm != null)
			{
				cm.PlacementTarget = fe;
				cm.Placement = PlacementMode.Bottom;
				cm.DataContext = DataContext; // damit Bindings funktionieren
				cm.IsOpen = true;
			}
		}
	}
}
