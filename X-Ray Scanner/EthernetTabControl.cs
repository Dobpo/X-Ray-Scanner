using System;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;



namespace X_Ray_Scanner
{
    /// Логика взаимодействия с Ethernet подкючением
    /// и вкладкой Настройки Ethernet.
    public partial class MainWindow 
    {
        private NetworkStream netStream; 
        private TcpClient tcpClient;
        byte[] ethInBuffer = new Byte[2048];
        private int byteCount;

        /// <summary>
        /// Событие по клику, попытатся установить соединение.
        /// </summary>
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectEthernet())
            {
                StatusTextBox.Text += "Установлено соединение IP: " + IpAddressTextBox.Text + " Port: " +
                                      PortTextBox.Text + "\n";
                ConnectionInfoLabel.Content = "Соединение установлено";
                ImageConnection.Data = Geometry.Parse(CustomTemplateImage.ImageConnect);
                ImageConnection.Fill = (Brush)FindResource("HighlightBrush");
            }
            else
            {
                StatusTextBox.Text += "Неудачная попытка установить соединеие.\n";
                ConnectionInfoLabel.Content = "Соединение не установлено";
                ImageConnection.Data = Geometry.Parse(CustomTemplateImage.ImageDisconnect);
                ImageConnection.Fill = (Brush)FindResource("DisabledMenuItemForeground");
            }
        }
        
        /// <summary>
        /// Событие по клику, разорвать TCP соединение.
        /// </summary>
        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (DisconnectEthernet())
            {
                StatusTextBox.Text += "Соединение разорвано.\n";
                ConnectionInfoLabel.Content = "Соединение не установлено";
                ImageConnection.Data = Geometry.Parse(CustomTemplateImage.ImageDisconnect);
                ImageConnection.Fill = (Brush)FindResource("DisabledMenuItemForeground");
            }
        }

        /// <summary>
        /// Событие по клику, отправить данные по установленому TCP слединению.
        /// </summary>
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendDataEthernet(SendDataTextBox.Text);
        }

        /// <summary>
        /// Событие по изминению текста, перемещает скрол в конец текстбокса.
        /// </summary>
        private void StatusTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            StatusTextBox.ScrollToEnd();
        }

        /// <summary>
        /// Событие по клику, очищает содержимое текстбокса.
        /// </summary>
        private void CleanButton_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBox.Text = "";
        }

        #region "<<<<<<<<<< Ethernet event's >>>>>>>>>>"
        /// <summary>
        /// Устанавливает TCP соединение, по заданому IP адресу и порту(из тект боксов на форме).
        /// </summary>
        private bool ConnectEthernet()
        {
            try
            {
                tcpClient = new TcpClient(IpAddressTextBox.Text, Convert.ToInt32(PortTextBox.Text));
                netStream = tcpClient.GetStream(); netStream.Flush();
                netStream.BeginRead(ethInBuffer, 0, ethInBuffer.Length, ReadCallBackEthernet, ethInBuffer);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            
        }

        /// <summary>
        /// Функиця обратного вызова для приема данных по TCP соединению.
        /// </summary>
        private void ReadCallBackEthernet(IAsyncResult streamResult)
        {
            try
            {
                int ToRead = netStream.EndRead(streamResult);
                byteCount += ToRead;
                byte[] tmpBufferBytes = streamResult.AsyncState as byte[];
                string TempString = null;
                int TempVar = 0;
                for (int i = 0; i < ToRead; i+=2)
                {
                    TempVar = ethInBuffer[i + 2] << 8 | ethInBuffer[i];
                    TempString += TempVar.ToString() + " | ";
                    StatusTextBox.Dispatcher.Invoke(new Action(() => StatusTextBox.Text+= "Принятые данные :" + TempString + "\n"));
                    netStream.BeginRead(ethInBuffer, 0, ethInBuffer.Length, ReadCallBackEthernet, ethInBuffer);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.Source);
            }
        }

        /// <summary>
        /// Закрывает установленое TCP соединение.
        /// </summary>
        private bool DisconnectEthernet()
        {
            try
            {
                netStream.Flush();
                netStream.Close();
                tcpClient.Close();
                return true;
            }
            catch (NullReferenceException)
            {
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Отправляет данные по установленому TCP соединению.
        /// </summary>
        private bool SendDataEthernet(string DataString)
        {
            DataString = DataString.Trim();
            if (string.IsNullOrEmpty(DataString)) return false;
            try
            {
                byte[] sendBytes = Encoding.ASCII.GetBytes(DataString);
                netStream.Write(sendBytes, 0, sendBytes.Length);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        #endregion
    }
}
