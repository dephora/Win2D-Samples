// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Devices.Input;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;



namespace ExampleGallery
{
    public sealed partial class BezCreate : UserControl
    {        
        float displayDpi;

        private Vector2 _startPoint;
        private readonly Random _random;
        private readonly Color[] _colors;
        private int _colorIndex = -1;

        private Vector2 _endPoint;
        private bool _isDragging = false;
        private bool _drawSpline = false;
        private bool _showControlPoints = false;

        List<Tuple<Vector2, Vector2, Vector2, Vector2, Color>> _pointData = new List<Tuple<Vector2, Vector2, Vector2, Vector2, Color>>();
        private Vector2 _controlPoint1;
        private Vector2 _controlPoint2;
        private Color _splineColor;

        public BezCreate()
        {
            this.InitializeComponent();

            _random = new Random();
            _colors = new Color[]
            {
                Colors.Crimson,
                Colors.BlueViolet,
                Colors.LightSeaGreen,
                Colors.DeepPink,
                Colors.DimGray,
                Colors.YellowGreen,
                Colors.Blue,
                Colors.DarkRed,
                Colors.DarkGreen
            };
        }

        private void OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var ds = args.DrawingSession;

            foreach (var point in _pointData)
            {
                DrawSpline(sender, ds, point.Item1, point.Item2, point.Item3, point.Item4, point.Item5);
            }

            if (_drawSpline)
            {
                var controlDistance = Math.Abs(_startPoint.X - _endPoint.X) / 2f;
                _controlPoint1 = _startPoint + new Vector2(controlDistance, 0);
                _controlPoint2 = _endPoint - new Vector2(controlDistance, 0);
                DrawSpline(sender, ds, _startPoint, _controlPoint1, _controlPoint2, _endPoint, _splineColor);
            }
        }

        private void DrawSpline(CanvasControl sender, CanvasDrawingSession ds,
            Vector2 startPoint, Vector2 controlPoint1, Vector2 controlPoint2, Vector2 endPoint,
            Color color)
        {
            var strokeThickness = 2f;

            // Draw the spline
            using (var pathBuilder = new CanvasPathBuilder(sender))
            {
                pathBuilder.BeginFigure(startPoint);
                pathBuilder.AddCubicBezier(controlPoint1, controlPoint2, endPoint);
                pathBuilder.EndFigure(CanvasFigureLoop.Open);

                var geometry = CanvasGeometry.CreatePath(pathBuilder);
                ds.DrawGeometry(geometry, Vector2.Zero, color, strokeThickness);
            }

            // Draw Control Points
            if (_showControlPoints)
            {
                var strokeStyle = new CanvasStrokeStyle() { DashStyle = CanvasDashStyle.Dot };
                ds.DrawLine(startPoint, controlPoint1, color, strokeThickness, strokeStyle);
                var rect1 = new Rect(controlPoint1.X - 3, controlPoint1.Y - 3, 6, 6);
                ds.FillRectangle(rect1, Colors.Beige);
                ds.DrawRectangle(rect1, color, strokeThickness);

                ds.DrawLine(endPoint, controlPoint2, color, strokeThickness, strokeStyle);
                var rect2 = new Rect(controlPoint2.X - 3, controlPoint2.Y - 3, 6, 6);
                ds.FillRectangle(rect2, Colors.Beige);
                ds.DrawRectangle(rect2, color, strokeThickness);
            }

            // Draw EndPoints
            ds.DrawCircle(startPoint, 5, color, strokeThickness);
            ds.FillCircle(startPoint, 5, Colors.Beige);
            ds.DrawCircle(endPoint, 5, color, strokeThickness);
            ds.FillCircle(endPoint, 5, Colors.Beige);

        }
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var width = e.NewSize.Width;
            var height = e.NewSize.Height;

            _startPoint = new Vector2((float)width / 2f, (float)height / 2f);
            canvas.Invalidate();
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isDragging = true;
            _startPoint = e.GetCurrentPoint(canvas).Position.ToVector2();
            _endPoint = _startPoint;
            _colorIndex = (_colorIndex + 1) % _colors.Length;

            _splineColor = _colors[_colorIndex];
            canvas.Invalidate();
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging)
            {
                return;
            }

            _drawSpline = true;
            _endPoint = e.GetCurrentPoint(canvas).Position.ToVector2();
            canvas.Invalidate();
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isDragging = false;
            _drawSpline = false;
            _pointData.Add(
                new Tuple<Vector2, Vector2, Vector2, Vector2, Color>(
                    _startPoint, _controlPoint1, _controlPoint2, _endPoint, _splineColor));
            _startPoint = Vector2.Zero;
            _endPoint = Vector2.Zero;
            _controlPoint1 = Vector2.Zero;
            _controlPoint2 = Vector2.Zero;
        }

        private void OnShowControlPoints(object sender, RoutedEventArgs e)
        {
            _showControlPoints = true;
            canvas.Invalidate();
        }

        private void OnHideControlPoints(object sender, RoutedEventArgs e)
        {
            _showControlPoints = false;
            canvas.Invalidate();
        }

        private void OnClearSplines(object sender, RoutedEventArgs e)
        {
            _pointData.Clear();
            canvas.Invalidate();
        }

        /*
        void Canvas_CreateResources(CanvasVirtualControl sender, CanvasCreateResourcesEventArgs args)
        {
            // Don't bother reloading our shaders if it is only the DPI that changed.
            // That happens all the time due to ScrollViewer_ViewChanged adjusting canvas.DpiScale.
            if (args.Reason == CanvasCreateResourcesReason.DpiChanged)
                return;

            args.TrackAsyncAction(Canvas_CreateResourcesAsync(sender).AsAsyncAction());
        }


        async Task Canvas_CreateResourcesAsync(CanvasVirtualControl sender)
        {
            mandelbrotEffect = new PixelShaderEffect(await Utils.ReadAllBytes("Shaders/Mandelbrot.bin"));

            // The Mandelbrot pixel shader outputs grayscale values. To make the result more interesting,
            // we run it through a TableTransferEffect. This applies a color gradient that goes from black
            // through blue, cyan, green, yellow, red, magenta, blue again, and finally back toward cyan.

            colorizeEffect = new TableTransferEffect
            {
                Source = mandelbrotEffect,

                RedTable   = new float[] { 0, 0, 0, 0, 1, 1, 0.67f, 0, 0    },
                GreenTable = new float[] { 0, 0, 1, 1, 1, 0, 0,     0, 0.5f },
                BlueTable  = new float[] { 0, 1, 1, 0, 0, 0, 1,     1, 1    },
            };
        }


        void Canvas_RegionsInvalidated(CanvasVirtualControl sender, CanvasRegionsInvalidatedEventArgs args)
        {
            // Configure the Mandelbrot effect to position and scale its output. 
            const float baseScale = 0.005f;
            float scale = baseScale * 96 / sender.Dpi;
            Vector2 translate = baseScale * sender.Size.ToVector2() * new Vector2(-0.75f, -0.5f);

            mandelbrotEffect.Properties["scale"] = scale;
            mandelbrotEffect.Properties["translate"] = translate;

            // Draw the effect to whatever regions of the CanvasVirtualControl have been invalidated.
            foreach (var region in args.InvalidatedRegions)
            {
                using (var drawingSession = sender.CreateDrawingSession(region))
                {
                    drawingSession.DrawImage(colorizeEffect);
                }
            }
        }
        */


        // When the ScrollViewer zooms in or out, we update DpiScale on our CanvasVirtualControl
        // to match. This adjusts its pixel density to match the current zoom level. But its size
        // in dips stays the same, so layout and scroll position are not affected by the zoom.
        void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            // Cancel out the display DPI, so our fractal always renders at 96 DPI regardless of display
            // configuration. This boosts performance on high DPI displays, at the cost of visual quality.
            // For even better performance (but lower quality) this value could be further reduced.
            float dpiAdjustment = 96 / displayDpi;

            // Adjust DPI to match the current zoom level.
            float dpiScale = dpiAdjustment * scrollViewer.ZoomFactor;

            // To boost performance during pinch-zoom manipulations, we only update DPI when it has
            // changed by more than 20%, or at the end of the zoom (when e.IsIntermediate reports false).
            // Smaller changes will just scale the existing bitmap, which is much faster than recomputing
            // the fractal at a different resolution. To trade off between zooming perf vs. smoothness,
            // adjust the thresholds used in this ratio comparison.
            var ratio = canvas.DpiScale / dpiScale;

            if (e == null || !e.IsIntermediate || ratio <= 0.8 || ratio >= 1.25)
            {
                canvas.DpiScale = dpiScale;
            }
            
        }


        void Display_DpiChanged(DisplayInformation sender, object args)
        {
            displayDpi = sender.LogicalDpi;

            // Manually call the ViewChanged handler to update DpiScale.
            ScrollViewer_ViewChanged(null, null);
        }


        // Adjust zoom level in response to the A and Z keys (for those without touch input or mouse wheels).
        void Canvas_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.A || e.Key == VirtualKey.Z)
            {
                var currentZoom = scrollViewer.ZoomFactor;
                var newZoom = currentZoom;

                if (e.Key == VirtualKey.A)
                    newZoom /= 0.9f;
                else
                    newZoom *= 0.9f;

                newZoom = Math.Max(newZoom, scrollViewer.MinZoomFactor);
                newZoom = Math.Min(newZoom, scrollViewer.MaxZoomFactor);

                var currentPan = new Vector2((float)scrollViewer.HorizontalOffset,
                                             (float)scrollViewer.VerticalOffset);

                var centerOffset = new Vector2((float)scrollViewer.ViewportWidth,
                                               (float)scrollViewer.ViewportHeight) / 2;

                var newPan = ((currentPan + centerOffset) * newZoom / currentZoom) - centerOffset;

                scrollViewer.ChangeView(newPan.X, newPan.Y, newZoom);

                e.Handled = true;
            }
        }


        void control_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize the display DPI, and listen for events in case this changes.
            var display = DisplayInformation.GetForCurrentView();
            display.DpiChanged += Display_DpiChanged;
            Display_DpiChanged(display, null);

            // Initialize the help text depending on what input devices are available.
            var toZoom = new List<string>();

            if (new TouchCapabilities().TouchPresent > 0)
            {
                toZoom.Add("Pinch");
            }

            if (new KeyboardCapabilities().KeyboardPresent > 0)
            {
                toZoom.Add("A/Z");

                if (new MouseCapabilities().VerticalWheelPresent > 0)
                {
                    toZoom.Add("Ctrl+Wheel");
                }
            }

            helpText.Text = "To zoom:\n  " + string.Join("\n  ", toZoom);

            // Set focus to our control, so it will receive keyboard input.
            canvas.Focus(FocusState.Programmatic);
        }


        void control_Unloaded(object sender, RoutedEventArgs e)
        {
            // Explicitly remove references to allow the Win2D controls to get garbage collected
            canvas.RemoveFromVisualTree();
            canvas = null;

            DisplayInformation.GetForCurrentView().DpiChanged -= Display_DpiChanged;
        }

        
    }
}
