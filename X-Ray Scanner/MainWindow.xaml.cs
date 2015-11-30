using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using ZoomAndPan;
using Brush = System.Windows.Media.Brush;

namespace X_Ray_Scanner
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        #region Свойства для Зума.
        /// <summary>
        /// Specifies the current state of the mouse handling logic.
        /// </summary>
        private MouseHandlingMode mouseHandlingMode = MouseHandlingMode.None;

        /// <summary>
        /// The point that was clicked relative to the ZoomAndPanControl.
        /// </summary>
        private Point origZoomAndPanControlMouseDownPoint;

        /// <summary>
        /// The point that was clicked relative to the content that is contained within the ZoomAndPanControl.
        /// </summary>
        private Point origContentMouseDownPoint;

        /// <summary>
        /// Records which mouse button clicked during mouse dragging.
        /// </summary>
        private MouseButton mouseButtonDown;

        /// <summary>
        /// Saves the previous zoom rectangle, pressing the backspace key jumps back to this zoom rectangle.
        /// </summary>
        private Rect prevZoomRect;

        /// <summary>
        /// Save the previous content scale, pressing the backspace key jumps back to this scale.
        /// </summary>
        private double prevZoomScale;

        /// <summary>
        /// Set to 'true' when the previous zoom rect is saved.
        /// </summary>
        private bool prevZoomRectSet = false;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            ImageConnection.Data = Geometry.Parse(CustomTemplateImage.ImageDisconnect);
            ImageConnection.Fill = (Brush)FindResource("DisabledMenuItemForeground"); 
        }

        /// <summary>
        /// Событие по нажатию кнопки Открыть, выводит диалоговое окно для 
        /// выбора изображения.н
        /// </summary>
        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg";

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                string fileName = dlg.FileName;
                Label1.Content = fileName;
                try
                {
                    BitmapSource btm = new BitmapImage(new Uri(fileName, UriKind.Absolute));
                    zoomAndPanControl.Background = Brushes.LightGray;
                    ThumbImage.Source = btm;
                    content.Source = btm;
                    ZoomControlsEnable();
                }
                catch (System.NotSupportedException)
                {
                    MessageBox.Show("Невозможно открыть изображение, возможно файл поврежден или имеет неизвестный формат.");
                }
            }
        }
        
        /// <summary>
        /// Событие закрытия главного окна, закрывает TCP соединение если было открыто.
        /// </summary>
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            DisconnectEthernet();
        }

        /// <summary>
        /// Делает кнопки и слайдер зума доступными для использования.
        /// </summary>
        private void ZoomControlsEnable()
        {
            ZoomSlider.IsEnabled = true;
            ZoomPlusButton.IsEnabled = true;
            ZoomRefreshButton.IsEnabled = true;
            ZoomExpandButton.IsEnabled = true;
            ZoomMinusButton.IsEnabled = true;
        }
        
        /// <summary>
        /// Запрещает использование кнопок и слайдера зума.
        /// </summary>
        private void ZoomControlsDisable()
        {
            ZoomSlider.IsEnabled = false;
            ZoomPlusButton.IsEnabled = false;
            ZoomRefreshButton.IsEnabled = false;
            ZoomExpandButton.IsEnabled = false;
            ZoomMinusButton.IsEnabled = false;
        }

        /// <summary>
        /// Событие переключатель показать/закрыть место для графиков.
        /// </summary>
        private void IsCheckedChange_SwitchGamma(object sender, EventArgs e)
        {
            if (EnabledSwitchGamma.IsChecked == true)
            {
                GraphicsColumnDefinition.Width = new GridLength(110);
                GraphicsRowDefinition.Height = new GridLength(110);
            }
            else
            {
                GraphicsColumnDefinition.Width = new GridLength(0);
                GraphicsRowDefinition.Height = new GridLength(0);
            }

        }

        /// <summary>
        /// Закрыть изображение
        /// </summary>
        private void CloseImageButton_Click(object sender, RoutedEventArgs e)
        {
            ThumbImage.Source = null;
            content.Source = null;
            zoomAndPanControl.Background = null;
            ZoomControlsDisable();
            zoomAndPanControl.AnimatedZoomTo(1.0); //Установить начальный зум.
        }

        #region Собития для подсказки
        private void MouseLeaveInfo(object sender, MouseEventArgs e)
        {
            StatusInfo.Content = "Готов";
        }

        private void SendDataTextBox_MouseMove(object sender, MouseEventArgs e)
        {
            StatusInfo.Content = "Данные для отправки в контроллер";
        }

        private void IpAddressTextBox_MouseMove(object sender, MouseEventArgs e)
        {
            StatusInfo.Content = "IP адрес для установки TCP соединения";
        }

        private void PortTextBox_MouseMove(object sender, MouseEventArgs e)
        {
            StatusInfo.Content = "Порт для установки TCP соединения";
        }
        private void ZoomSlider_MouseMove(object sender, MouseEventArgs e)
        {
            StatusInfo.Content = "Переместить для увеличения/уменьшения изображения";
        }

        private void ZoomPlusButton_MouseMove(object sender, MouseEventArgs e)
        {
            StatusInfo.Content = "Увеличить изображение";
        }

        private void ZoomRefreshButton_MouseMove(object sender, MouseEventArgs e)
        {
            StatusInfo.Content = "Оригинальный размер изображения";
        }

        private void TextBlock_MouseMove(object sender, MouseEventArgs e)
        {
            StatusInfo.Content = "Размер изображения";
        }

        private void ZoomExpandButton_MouseMove(object sender, MouseEventArgs e)
        {
            StatusInfo.Content = "Растянуть изображение";
        }

        private void ZoomMinusButton_MouseMove(object sender, MouseEventArgs e)
        {
            StatusInfo.Content = "Уменьшить изображение";
        }
        private void ThumbImage_MouseMove(object sender, MouseEventArgs e)
        {
            StatusInfo.Content = "Эскиз изображения";
        }
        #endregion

        #region События для зума изображения
        /// <summary>
        /// Event raised on mouse down in the ZoomAndPanControl.
        /// </summary>
        private void zoomAndPanControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            content.Focus();
            Keyboard.Focus(content);

            mouseButtonDown = e.ChangedButton;
            origZoomAndPanControlMouseDownPoint = e.GetPosition(zoomAndPanControl);
            origContentMouseDownPoint = e.GetPosition(content);

            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0 &&
                (e.ChangedButton == MouseButton.Left ||
                 e.ChangedButton == MouseButton.Right))
            {
                // Shift + left- or right-down initiates zooming mode.
                mouseHandlingMode = MouseHandlingMode.Zooming;
            }
            else if (mouseButtonDown == MouseButton.Left)
            {
                // Just a plain old left-down initiates panning mode.
                mouseHandlingMode = MouseHandlingMode.Panning;
            }

            if (mouseHandlingMode != MouseHandlingMode.None)
            {
                // Capture the mouse so that we eventually receive the mouse up event.
                zoomAndPanControl.CaptureMouse();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Event raised on mouse up in the ZoomAndPanControl.
        /// </summary>
        private void zoomAndPanControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mouseHandlingMode != MouseHandlingMode.None)
            {
                if (mouseHandlingMode == MouseHandlingMode.Zooming)
                {
                    if (mouseButtonDown == MouseButton.Left)
                    {
                        // Shift + left-click zooms in on the content.
                        ZoomIn(origContentMouseDownPoint);
                    }
                    else if (mouseButtonDown == MouseButton.Right)
                    {
                        // Shift + left-click zooms out from the content.
                        ZoomOut(origContentMouseDownPoint);
                    }
                }
                else if (mouseHandlingMode == MouseHandlingMode.DragZooming)
                {
                    // When drag-zooming has finished we zoom in on the rectangle that was highlighted by the user.
                    ApplyDragZoomRect();
                }

                zoomAndPanControl.ReleaseMouseCapture();
                mouseHandlingMode = MouseHandlingMode.None;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Event raised on mouse move in the ZoomAndPanControl.
        /// </summary>
        private void zoomAndPanControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseHandlingMode == MouseHandlingMode.Panning)
            {
                //
                // The user is left-dragging the mouse.
                // Pan the viewport by the appropriate amount.
                //
                Point curContentMousePoint = e.GetPosition(content);
                Vector dragOffset = curContentMousePoint - origContentMouseDownPoint;

                zoomAndPanControl.ContentOffsetX -= dragOffset.X;
                zoomAndPanControl.ContentOffsetY -= dragOffset.Y;

                e.Handled = true;
            }
            else if (mouseHandlingMode == MouseHandlingMode.Zooming)
            {
                Point curZoomAndPanControlMousePoint = e.GetPosition(zoomAndPanControl);
                Vector dragOffset = curZoomAndPanControlMousePoint - origZoomAndPanControlMouseDownPoint;
                double dragThreshold = 10;
                if (mouseButtonDown == MouseButton.Left &&
                    (Math.Abs(dragOffset.X) > dragThreshold ||
                     Math.Abs(dragOffset.Y) > dragThreshold))
                {
                    //
                    // When Shift + left-down zooming mode and the user drags beyond the drag threshold,
                    // initiate drag zooming mode where the user can drag out a rectangle to select the area
                    // to zoom in on.
                    //
                    mouseHandlingMode = MouseHandlingMode.DragZooming;
                    Point curContentMousePoint = e.GetPosition(content);
                    InitDragZoomRect(origContentMouseDownPoint, curContentMousePoint);
                }

                e.Handled = true;
            }
            else if (mouseHandlingMode == MouseHandlingMode.DragZooming)
            {
                //
                // When in drag zooming mode continously update the position of the rectangle
                // that the user is dragging out.
                //
                Point curContentMousePoint = e.GetPosition(content);
                SetDragZoomRect(origContentMouseDownPoint, curContentMousePoint);

                e.Handled = true;
            }
        }

        /// <summary>
        /// Event raised by rotating the mouse wheel
        /// </summary>
        private void zoomAndPanControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            if (e.Delta > 0)
            {
                Point curContentMousePoint = e.GetPosition(content);
                ZoomIn(curContentMousePoint);
            }
            else if (e.Delta < 0)
            {
                Point curContentMousePoint = e.GetPosition(content);
                ZoomOut(curContentMousePoint);
            }
        }

        /// <summary>
        /// The 'ZoomIn' command (bound to the plus key) was executed.
        /// </summary>
        private void ZoomIn_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ZoomIn(new Point(zoomAndPanControl.ContentZoomFocusX, zoomAndPanControl.ContentZoomFocusY));
        }

        /// <summary>
        /// The 'ZoomOut' command (bound to the minus key) was executed.
        /// </summary>
        private void ZoomOut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ZoomOut(new Point(zoomAndPanControl.ContentZoomFocusX, zoomAndPanControl.ContentZoomFocusY));
        }

        /// <summary>
        /// The 'JumpBackToPrevZoom' command was executed.
        /// </summary>
        private void JumpBackToPrevZoom_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            JumpBackToPrevZoom();
        }

        /// <summary>
        /// Determines whether the 'JumpBackToPrevZoom' command can be executed.
        /// </summary>
        private void JumpBackToPrevZoom_CanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = prevZoomRectSet;
        }

        /// <summary>
        /// The 'Fill' command was executed.
        /// </summary>
        private void Fill_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SavePrevZoomRect();

            zoomAndPanControl.AnimatedScaleToFit();
        }

        /// <summary>
        /// The 'OneHundredPercent' command was executed.
        /// </summary>
        private void OneHundredPercent_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SavePrevZoomRect();

            zoomAndPanControl.AnimatedZoomTo(1.0);
        }

        /// <summary>
        /// Jump back to the previous zoom level.
        /// </summary>
        private void JumpBackToPrevZoom()
        {
            zoomAndPanControl.AnimatedZoomTo(prevZoomScale, prevZoomRect);

            ClearPrevZoomRect();
        }

        /// <summary>
        /// Zoom the viewport out, centering on the specified point (in content coordinates).
        /// </summary>
        private void ZoomOut(Point contentZoomCenter)
        {
            zoomAndPanControl.ZoomAboutPoint(zoomAndPanControl.ContentScale - 0.1, contentZoomCenter);
        }

        /// <summary>
        /// Zoom the viewport in, centering on the specified point (in content coordinates).
        /// </summary>
        private void ZoomIn(Point contentZoomCenter)
        {
            zoomAndPanControl.ZoomAboutPoint(zoomAndPanControl.ContentScale + 0.1, contentZoomCenter);
        }

        /// <summary>
        /// Initialise the rectangle that the use is dragging out.
        /// </summary>
        private void InitDragZoomRect(Point pt1, Point pt2)
        {
            SetDragZoomRect(pt1, pt2);

            dragZoomCanvas.Visibility = Visibility.Visible;
            dragZoomBorder.Opacity = 0.5;
        }

        /// <summary>
        /// Update the position and size of the rectangle that user is dragging out.
        /// </summary>
        private void SetDragZoomRect(Point pt1, Point pt2)
        {
            double x, y, width, height;

            //
            // Deterine x,y,width and height of the rect inverting the points if necessary.
            // 

            if (pt2.X < pt1.X)
            {
                x = pt2.X;
                width = pt1.X - pt2.X;
            }
            else
            {
                x = pt1.X;
                width = pt2.X - pt1.X;
            }

            if (pt2.Y < pt1.Y)
            {
                y = pt2.Y;
                height = pt1.Y - pt2.Y;
            }
            else
            {
                y = pt1.Y;
                height = pt2.Y - pt1.Y;
            }

            //
            // Update the coordinates of the rectangle that is being dragged out by the user.
            // The we offset and rescale to convert from content coordinates.
            //
            Canvas.SetLeft(dragZoomBorder, x);
            Canvas.SetTop(dragZoomBorder, y);
            dragZoomBorder.Width = width;
            dragZoomBorder.Height = height;
        }

        /// <summary>
        /// When the user has finished dragging out the rectangle the zoom operation is applied.
        /// </summary>
        private void ApplyDragZoomRect()
        {
            //
            // Record the previous zoom level, so that we can jump back to it when the backspace key is pressed.
            //
            SavePrevZoomRect();

            //
            // Retreive the rectangle that the user draggged out and zoom in on it.
            //
            double contentX = Canvas.GetLeft(dragZoomBorder);
            double contentY = Canvas.GetTop(dragZoomBorder);
            double contentWidth = dragZoomBorder.Width;
            double contentHeight = dragZoomBorder.Height;
            zoomAndPanControl.AnimatedZoomTo(new Rect(contentX, contentY, contentWidth, contentHeight));

            FadeOutDragZoomRect();
        }

        //
        // Fade out the drag zoom rectangle.
        //
        private void FadeOutDragZoomRect()
        {
            AnimationHelper.StartAnimation(dragZoomBorder, Border.OpacityProperty, 0.0, 0.1,
                delegate (object sender, EventArgs e)
                {
                    dragZoomCanvas.Visibility = Visibility.Collapsed;
                });
        }

        //
        // Record the previous zoom level, so that we can jump back to it when the backspace key is pressed.
        //
        private void SavePrevZoomRect()
        {
            prevZoomRect = new Rect(zoomAndPanControl.ContentOffsetX, zoomAndPanControl.ContentOffsetY, zoomAndPanControl.ContentViewportWidth, zoomAndPanControl.ContentViewportHeight);
            prevZoomScale = zoomAndPanControl.ContentScale;
            prevZoomRectSet = true;
        }

        /// <summary>
        /// Clear the memory of the previous zoom level.
        /// </summary>
        private void ClearPrevZoomRect()
        {
            prevZoomRectSet = false;
        }

        /// <summary>
        /// Event raised when the user has double clicked in the zoom and pan control.
        /// </summary>
        private void zoomAndPanControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                Point doubleClickPoint = e.GetPosition(content);
                zoomAndPanControl.AnimatedSnapTo(doubleClickPoint);
            }
        }

        #endregion
        
    }
}
