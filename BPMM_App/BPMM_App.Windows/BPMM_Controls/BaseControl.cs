﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace BPMM_App
{
    public enum Category
    {
        VISION, GOAL, OBJECTIVE, MISSION, STRATEGY, TACTIC, BUSINESS_POLICY, BUSINESS_RULE, INFLUENCER, ASSESSMENT, NOTE
    }
    public abstract class BaseControl : UserControl
    {
        private const int MIN_SIZE = 100;
        private static int max_id = 0;

        public int id;
        public Category category;

        protected Grid frame;
        protected Grid contentGrid;
        private Grid container;
        private Rectangle anchor;
        private Thumb topLeftThumb;
        private Thumb topRightThumb;
        private Thumb bottomLeftThumb;
        private Thumb bottomRightThumb;

        protected bool isDragging;
        private PointerPoint offset;

        public event PointerEventHandler MovedEvent;
        public event PointerEventHandler MoveEndEvent;
        public event PointerEventHandler AssociationStartEvent;
        public event EventHandler AssociationEndEvent;
        public event EventHandler DeleteEvent;

        public BaseControl(Category category)
        {
            id = ++max_id;
            this.category = category;
            RightTapped += BaseControl_RightTapped;

            frame = new Grid() { Width = 200, Height = 200, Background = new SolidColorBrush(Colors.LightBlue) };
            frame.PointerPressed += UserControl_PointerPressed;
            frame.PointerMoved += UserControl_PointerMoved;
            frame.PointerReleased += UserControl_PointerReleased;
            frame.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(10) });
            frame.RowDefinitions.Add(new RowDefinition());
            frame.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(10) });

            topLeftThumb = new Thumb()
            {
                Height = 10, Width = 10,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(Colors.White)
            };
            topRightThumb = new Thumb()
            {
                Height = 10, Width = 10,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = new SolidColorBrush(Colors.White)
            };
            bottomLeftThumb = new Thumb()
            {
                Height = 10, Width = 10,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(Colors.White)
            };
            bottomRightThumb = new Thumb()
            {
                Height = 10,
                Width = 10,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = new SolidColorBrush(Colors.White)
            };
            topLeftThumb.DragDelta += ThumbTopLeft_DragDelta;
            topRightThumb.DragDelta += ThumbTopRight_DragDelta;
            bottomLeftThumb.DragDelta += ThumbBottomLeft_DragDelta;
            bottomRightThumb.DragDelta += ThumbBottomRight_DragDelta;

            contentGrid = new Grid();
            Grid.SetRow(topLeftThumb, 0);
            Grid.SetRow(topRightThumb, 0);
            Grid.SetRow(contentGrid, 1);
            Grid.SetRow(bottomLeftThumb, 2);
            Grid.SetRow(bottomRightThumb, 2);
            frame.Children.Add(topLeftThumb);
            frame.Children.Add(topRightThumb);
            frame.Children.Add(bottomLeftThumb);
            frame.Children.Add(bottomRightThumb);
            frame.Children.Add(contentGrid);

            anchor = new Rectangle()
            {
                Height = 15,
                Width = 15,
                VerticalAlignment = VerticalAlignment.Top,
                Stroke = new SolidColorBrush(Colors.Black),
                Fill = new SolidColorBrush(Colors.White)
            };
            anchor.PointerPressed += anchor_PointerPressed;

            container = new Grid();
            container.ColumnDefinitions.Add(new ColumnDefinition());
            container.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(frame, 0);
            Grid.SetColumn(anchor, 1);
            container.Children.Add(frame);
            container.Children.Add(anchor);

            Canvas canvas = new Canvas();
            canvas.Children.Add(container);
            Content = canvas;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            base.MeasureOverride(availableSize);
            Size desiredSize = new Size();
            container.Measure(availableSize);
            desiredSize = container.DesiredSize;
            return desiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var size = base.ArrangeOverride(finalSize);
            frame.Width = finalSize.Width * 0.93;
            frame.Height = finalSize.Height * 0.93;
            frame.Arrange(new Rect(0, 0, finalSize.Width * 0.93, finalSize.Height * 0.93));
            anchor.Width = finalSize.Width * 0.07;
            anchor.Height = finalSize.Width * 0.07;
            anchor.Arrange(new Rect(frame.ActualWidth, 0, finalSize.Width * 0.07, finalSize.Width * 0.07));
            container.Width = finalSize.Width;
            container.Height = finalSize.Height;
            container.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            return size;
        }

        public abstract void UpdateFontSize(double scale);

        public virtual JsonObject serialize()
        {
            var controlEntry = new JsonObject();
            controlEntry.Add("category", JsonValue.CreateNumberValue((int)category));
            controlEntry.Add("x", JsonValue.CreateNumberValue(Canvas.GetLeft(this)));
            controlEntry.Add("y", JsonValue.CreateNumberValue(Canvas.GetTop(this)));
            controlEntry.Add("width", JsonValue.CreateNumberValue(ActualWidth));
            controlEntry.Add("height", JsonValue.CreateNumberValue(ActualHeight));
            return controlEntry;
        }

        public static BaseControl deserialize(JsonObject input)
        {
            var value = input.GetNamedNumber("category", -1);
            if (value == -1)
            {
                return null;
            }
            try
            {
                Category newType = (Category)value;
                var control =
                    (newType == Category.NOTE) ? (BaseControl)new NoteControl() :
                    (newType == Category.BUSINESS_RULE) ? (BaseControl)new BusinessRuleControl() :
                    (newType == Category.INFLUENCER) ? (BaseControl)new InfluencerControl() :
                    (newType == Category.ASSESSMENT) ? (BaseControl)new AssessmentControl() :
                    new BPMMControl(newType);
                Canvas.SetLeft(control, input.GetNamedNumber("x", 0));
                Canvas.SetTop(control, input.GetNamedNumber("y", 0));
                return control;
            }
            catch (InvalidCastException)
            {
                return null;
            }
        }

        protected void setContent(FrameworkElement element)
        {
            Grid.SetRow(element, 1);
            element.Margin = new Thickness(10, 0, 10, 0);
            frame.Children.Add(element);
        }

        public static void resetIds() {
            max_id = 0;
        }

        private async void BaseControl_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var menu = new PopupMenu();
            menu.Commands.Add(new UICommand("Delete BPMM Object"));

            var response = await menu.ShowForSelectionAsync(MenuPos());
            if (response != null && response.Label == "Delete BPMM Object")
            {
                MessageDialog affirmationPopup = new MessageDialog("", string.Format("Really Delete this?"));
                affirmationPopup.Commands.Add(new UICommand("Ok"));
                affirmationPopup.Commands.Add(new UICommand("Cancel"));
                var response2 = await affirmationPopup.ShowAsync();
                if (response2 != null && response2.Label == "Ok")
                {
                    if (DeleteEvent != null)
                    {
                        DeleteEvent(this, EventArgs.Empty);
                    }
                }
            }
        }

        private Rect MenuPos()
        {
            GeneralTransform transform = TransformToVisual(null);
            Point pointTransformed = transform.TransformPoint(new Point(0, 0));
            return new Rect(pointTransformed.X, pointTransformed.Y, ActualWidth, ActualHeight);
        }

        #region dragging
        private void UserControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            isDragging = true;
            offset = e.GetCurrentPoint(this);
            frame.CapturePointer(e.Pointer);
            e.Handled = true;
        }

        private void UserControl_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isDragging)
            {
                PointerPoint currPos = e.GetCurrentPoint(Parent as UIElement);
                var prevPosX = Canvas.GetLeft(this);
                var prevPosY = Canvas.GetTop(this);

                var newPosX = currPos.Position.X - offset.Position.X;
                var newPosY = currPos.Position.Y - offset.Position.Y;
                Canvas.SetLeft(this, newPosX);
                Canvas.SetTop(this, newPosY);
                if (MovedEvent != null)
                {
                    MovedEvent(this, e);
                }
            }
            e.Handled = true;
        }

        private void UserControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                if (MoveEndEvent != null)
                {
                    MoveEndEvent(this, e);
                }
                frame.ReleasePointerCapture(e.Pointer);
            }
            else
            {
                if (AssociationEndEvent != null)
                {
                    AssociationEndEvent(this, EventArgs.Empty);
                }
            }
            e.Handled = true;
        }
        #endregion

        #region resize
        private void ThumbTopLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double xChange = frame.Width - e.HorizontalChange > MIN_SIZE ? e.HorizontalChange : 0;
            double yChange = frame.Height - e.VerticalChange > MIN_SIZE ? e.VerticalChange : 0;
            frame.Width -= xChange;
            frame.Height -= yChange;
            Canvas.SetLeft(frame, Canvas.GetLeft(frame) + xChange);
            Canvas.SetTop(frame, Canvas.GetTop(frame) + yChange);
        }

        private void ThumbTopRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double xChange = frame.Width + e.HorizontalChange > MIN_SIZE ? e.HorizontalChange : 0;
            double yChange = frame.Height - e.VerticalChange > MIN_SIZE ? e.VerticalChange : 0;
            frame.Width += xChange;
            frame.Height -= yChange;
            Canvas.SetTop(frame, Canvas.GetTop(frame) + yChange);
        }

        private void ThumbBottomLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double xChange = frame.Width - e.HorizontalChange > MIN_SIZE ? e.HorizontalChange : 0;
            double yChange = frame.Height + e.VerticalChange > MIN_SIZE ? e.VerticalChange : 0;
            frame.Width -= xChange;
            frame.Height += yChange;
            Canvas.SetLeft(frame, Canvas.GetLeft(frame) + xChange);
        }

        private void ThumbBottomRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double xChange = frame.Width + e.HorizontalChange > MIN_SIZE ? e.HorizontalChange : 0;
            double yChange = frame.Height + e.VerticalChange > MIN_SIZE ? e.VerticalChange : 0;
            frame.Width += xChange;
            frame.Height += yChange;
        }
        #endregion

        private void anchor_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (AssociationStartEvent != null)
            {
                AssociationStartEvent(this, e);
                e.Handled = true;
            }
        }
    }
}
