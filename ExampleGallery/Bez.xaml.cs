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
using Windows.Devices.Input;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace ExampleGallery
{
    public sealed partial class Bez : UserControl
    {
        

        float displayDpi;


        public Bez()
        {
            this.InitializeComponent();
        }


        void Canvas_CreateResources(CanvasVirtualControl sender, CanvasCreateResourcesEventArgs args)
        {
            // Don't bother reloading our shaders if it is only the DPI that changed.
            // That happens all the time due to ScrollViewer_ViewChanged adjusting canvas.DpiScale.
            if (args.Reason == CanvasCreateResourcesReason.DpiChanged)
                return;

            //args.TrackAsyncAction(Canvas_CreateResourcesAsync(sender).AsAsyncAction());
        }


        //async Task Canvas_CreateResourcesAsync(CanvasVirtualControl sender)
        //{
            
        //}


        void Canvas_RegionsInvalidated(CanvasVirtualControl sender, CanvasRegionsInvalidatedEventArgs args)
        {
            

            // Draw the effect to whatever regions of the CanvasVirtualControl have been invalidated.
            foreach (var region in args.InvalidatedRegions)
            {
                using (var drawingSession = sender.CreateDrawingSession(region))
                {
                    //drawingSession.DrawImage(colorizeEffect);
                }
            }
        }


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
