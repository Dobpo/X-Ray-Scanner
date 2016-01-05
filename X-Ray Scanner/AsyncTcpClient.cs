using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Net.Sockets;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace X_Ray_Scanner
{
    class AsyncTcpClient : UserControl
    {
        //Количество байт для отправки
        private int _bytesToSend;

        //IpAddress и Port
        private string _ipAddress;
        private int _port;
        
        //Вх./Вых. буффер для данных.
        public readonly byte[] InDataBuffer = new byte[1024];
        public readonly byte[] OutDataBuffer = new byte[1024];
        
        //Socet для подключения TCP соединения.
        public Socket MySocket;

        //Добавить BackgroundWorker для приема данных.

        //Переменные для запуска отдельных потоков.
        private readonly BackgroundWorker _backConnect = new BackgroundWorker();
        private readonly BackgroundWorker _backSend = new BackgroundWorker();

        //Событие установки TCP соединения.
        private static readonly RoutedEvent ConnectEvent = EventManager.RegisterRoutedEvent("ConnectData",
            RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (AsyncTcpClient));
        public event RoutedEventHandler ConnectData
        {
            add { AddHandler(ConnectEvent, value); }
            remove { RemoveHandler(ConnectEvent, value); }
        }

        //Событие отправки данных по TCP соединению.
        private static readonly RoutedEvent SendEvent = EventManager.RegisterRoutedEvent("SendData",
            RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (AsyncTcpClient));
        public event RoutedEventHandler SendData
        {
            add { AddHandler(SendEvent, value); }
            remove { RemoveHandler(SendEvent, value); }
        }

        //Событие получения данных по TCP соединению.
        private static readonly RoutedEvent ReceiveEvent = EventManager.RegisterRoutedEvent("ReceiveData",
            RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (AsyncTcpClient));
        public event RoutedEventHandler ReceiveData
        {
            add { AddHandler(ReceiveEvent, value); }
            remove { RemoveHandler(ReceiveEvent, value); }
        }

        //Событие разрыва соединения.
        private static readonly RoutedEvent DisconnectEvent = EventManager.RegisterRoutedEvent("DisconnectData",
            RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (AsyncTcpClient));
        public event RoutedEventHandler DisconnectData
        {
            add { AddHandler(DisconnectEvent, value);}
            remove { RemoveHandler(DisconnectEvent, value);}
        }

        //Конструктор
        public AsyncTcpClient()
        {
            _backConnect.DoWork += ConnectEventHandler;
            _backSend.DoWork += SendEvenHandler;
        }

        //Метод для вызова события запуска соединения в отдельном потоке.
        public void Connect()
        {
            MySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _backConnect.RunWorkerAsync();
        }

        //Метод закрытия соединения.
        public void Close()
        {
            MySocket.Close();
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { RaiseEvent(new RoutedEventArgs(DisconnectEvent, this)); }));
        }

        //Метод для вызова события отсылки данных в отдельном потоке.
        public void Send(int dataCount)
        {
            _bytesToSend = dataCount;
            _backSend.RunWorkerAsync();
        }

        //Задаем IpAddress и Port
        public void SetIpAndPort(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        //Для делегата обработки события установки соединения.
        private void ConnectEventHandler(object sender, DoWorkEventArgs e)
        {
            var connectionResult = MySocket.BeginConnect(_ipAddress, _port, null, null);//("192.168.0.47", 9670, null, null);
            if (!connectionResult.AsyncWaitHandle.WaitOne(100, true)) MySocket.Close();
            else MySocket.BeginReceive(InDataBuffer, 0, InDataBuffer.Length, 0, ReceiveCallback, null);
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { RaiseEvent(new RoutedEventArgs(ConnectEvent, this)); }));
        }

        //Для делегата обработки события отсылки данных.
        private void SendEvenHandler(object sender, DoWorkEventArgs e)
        {
            var sendResult = MySocket.BeginSend(OutDataBuffer, 0, _bytesToSend, 0, null, null);
            if (sendResult != null && !sendResult.AsyncWaitHandle.WaitOne(100, true))
            {
                MySocket.Close();
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
            MySocket.EndReceive(ar);
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { RaiseEvent(new RoutedEventArgs(ReceiveEvent, this)); }));
            MySocket.BeginReceive(InDataBuffer, 0, InDataBuffer.Length, 0, ReceiveCallback, null);
        }
    }
}
