using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Threading;

namespace X_Ray_Scanner
{
    internal class AsyncTcpClient : UserControl
    {
        private static AsyncTcpClient _instance;
        private string _ipAddress;
        private int _port;
        private static Socket _mySocket;

        //Количество байт для отправки
        private int _bytesToSend;

        //!!!Добавить BackgroundWorker для приема данных.

        public static bool IsConected() {
            if (_mySocket != null)
                return _mySocket.Connected;
            else return false;
        }

        //Вх./Вых. буффер для данных.
        public readonly byte[] InDataBuffer = new byte[1024];
        public byte[] OutDataBuffer = new byte[1024];
        
        //Переменные для запуска отдельных потоков.
        private readonly BackgroundWorker _backConnect = new BackgroundWorker();
        private readonly BackgroundWorker _backSend = new BackgroundWorker();

        //Событие установки TCP соединения.
        private static readonly RoutedEvent ConnectEvent = EventManager.RegisterRoutedEvent("ConnectData",
            RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (AsyncTcpClient));
        public event RoutedEventHandler ConnectData
        {
            add => AddHandler(ConnectEvent, value);
            remove => RemoveHandler(ConnectEvent, value);
        }

        //Событие отправки данных по TCP соединению.
        private static readonly RoutedEvent SendEvent = EventManager.RegisterRoutedEvent("SendData",
            RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (AsyncTcpClient));
        public event RoutedEventHandler SendData
        {
            add => AddHandler(SendEvent, value);
            remove => RemoveHandler(SendEvent, value);
        }

        //Событие получения данных по TCP соединению.
        private static readonly RoutedEvent ReceiveEvent = EventManager.RegisterRoutedEvent("ReceiveData",
            RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (AsyncTcpClient));
        public event RoutedEventHandler ReceiveData
        {
            add => AddHandler(ReceiveEvent, value);
            remove => RemoveHandler(ReceiveEvent, value);
        }

        //Событие разрыва соединения.
        private static readonly RoutedEvent DisconnectEvent = EventManager.RegisterRoutedEvent("DisconnectData",
            RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (AsyncTcpClient));
        public event RoutedEventHandler DisconnectData
        {
            add => AddHandler(DisconnectEvent, value);
            remove => RemoveHandler(DisconnectEvent, value);
        }

        //Конструктор
        private AsyncTcpClient()
        {
            _backConnect.DoWork += ConnectEventHandler;
            _backSend.DoWork += SendEvenHandler;
        }

        public static AsyncTcpClient GetInstance()
        {
            return _instance ?? (_instance = new AsyncTcpClient());
        }

        //Метод для вызова события запуска соединения в отдельном потоке.
        public void ConnectTo(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
            _mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _backConnect.RunWorkerAsync();
        }

        //Метод закрытия соединения.
        public void Close()
        {
            _mySocket.Close();
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { RaiseEvent(new RoutedEventArgs(DisconnectEvent, this)); }));
        }

        //Метод для вызова события отсылки данных в отдельном потоке.
        public void Send(int dataCount)
        {
            _bytesToSend = dataCount;
            _backSend.RunWorkerAsync();
        }

        public void Send(byte[] array)
        {
            _bytesToSend = array.Length;
            OutDataBuffer = array;
            _backSend.RunWorkerAsync();
        }

        //Для делегата обработки события установки соединения.
        private void ConnectEventHandler(object sender, DoWorkEventArgs e)
        {
            var connectionResult = _mySocket.BeginConnect(_ipAddress, _port, null, null);//("192.168.0.47", 9670, null, null);
            if (!connectionResult.AsyncWaitHandle.WaitOne(100, true)) _mySocket.Close();
            else _mySocket.BeginReceive(InDataBuffer, 0, InDataBuffer.Length, 0, ReceiveCallback, null);
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { RaiseEvent(new RoutedEventArgs(ConnectEvent, this)); }));
        }

        //Для делегата обработки события отсылки данных.
        private void SendEvenHandler(object sender, DoWorkEventArgs e)
        {
            var sendResult = _mySocket.BeginSend(OutDataBuffer, 0, _bytesToSend, 0, null, null);
            if (sendResult != null && !sendResult.AsyncWaitHandle.WaitOne(100, true))
            {
                _mySocket.Close();
                Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { RaiseEvent(new RoutedEventArgs(ConnectEvent, this)); }));
            }
            else
            {
                Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { RaiseEvent(new RoutedEventArgs(SendEvent, this)); }));
            }
        }

        //Функция обратного вызова для приема данных по TCP соединению.
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                _mySocket.EndReceive(ar);
                Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { RaiseEvent(new RoutedEventArgs(ReceiveEvent, this)); }));
                _mySocket.BeginReceive(InDataBuffer, 0, InDataBuffer.Length, 0, ReceiveCallback, null);
            }
            catch (Exception ex) {
                MessageBox.Show("Source: "+ ex.Source + ", Message: " + ex.Message);
            }
        }
    }
}
