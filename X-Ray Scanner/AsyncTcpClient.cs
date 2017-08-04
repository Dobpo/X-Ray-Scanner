using System;
using System.ComponentModel;
using System.Net;
using System.Windows.Controls;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace X_Ray_Scanner
{
    internal class RpiConnect : UserControl
    {
        private static RpiConnect _instance;

        const int ImageLenghtByte = 10616832;
        const int ImageLenghtShort = 5308416;
        
        private byte[] _inBuffer = new byte[ImageLenghtByte];
        public short[] ImageBuffer = new short[ImageLenghtShort];
        private readonly byte[] _outComandToTakeData = new byte[] { 1, 2, 3, 4 };

        private int _offset, _bytesReadFromStream, _bytesReadCount;

        private NetworkStream _networkStream;
        private TcpClient _client;
        private readonly IPEndPoint _ipEndPoint = new IPEndPoint(IPAddress.Parse("192.168.0.53"), 9734);

        public static RpiConnect GetInstance()
        {
            return _instance ?? (_instance = new RpiConnect());
        }

        private RpiConnect()
        {   
        }

        public void RebuiltResivedData()
        {
            int delta = 1152;
            short[] DataBuffer = new short[ImageLenghtShort];

            for (int i = 0; i < ImageLenghtShort; i++)
            {
                DataBuffer[i] = (short)(((_inBuffer[i * 2 + 1] - 32) << 8 + _inBuffer[i * 2]) * 8);
            }

            for (int v = 0; v < ImageLenghtShort; v += delta * 2)
            {
                for (int j = 0; j < delta; j += 128)
                {
                    for (int i = 0; i < 128; i++)
                    {
                        ImageBuffer[i + j + v] = DataBuffer[i + j + v];
                        ImageBuffer[i + j + v + delta] = DataBuffer[i + j + v + delta];
                    }
                }
            }
        }

        public void TakeData()
        {
            try
            {
                _client = new TcpClient();
                _client.Connect(_ipEndPoint);
                _networkStream = _client.GetStream();
                _networkStream.Write(_outComandToTakeData, 0, 4);
                _bytesReadFromStream = 0;
                _offset = 0;
                _bytesReadCount = 1296;
                do
                {
                    if (ImageLenghtByte - _offset < 1296) _bytesReadCount = ImageLenghtByte - _offset;
                    _bytesReadFromStream = _networkStream.Read(_inBuffer, _offset, _bytesReadCount);
                    _offset += _bytesReadFromStream;
                } while (_bytesReadFromStream != 0);
                Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { RaiseEvent(new RoutedEventArgs(GotDataEvent, this)); }));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
            finally
            {
                _networkStream.Close();
                _client.Close();
                _client = null;
            }
        }

        private static readonly RoutedEvent GotDataEvent = EventManager.RegisterRoutedEvent("GotDataFromRPi",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RpiConnect));
        public event RoutedEventHandler GotDataFromRPi
        {
            add => AddHandler(GotDataEvent, value);
            remove => RemoveHandler(GotDataEvent, value);
        }
    }

    internal class AsyncTcpClient : UserControl, IDisposable
    {
        private static AsyncTcpClient _instance;
        private string _ipAddress;
        private int _port;
        private static Socket _mySocket;

        //Количество байт для отправки
        private int _bytesToSend;

        //!!!Добавить BackgroundWorker для приема данных.

        public static bool IsConected()
        {
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
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AsyncTcpClient));
        public event RoutedEventHandler ConnectData
        {
            add => AddHandler(ConnectEvent, value);
            remove => RemoveHandler(ConnectEvent, value);
        }

        //Событие отправки данных по TCP соединению.
        private static readonly RoutedEvent SendEvent = EventManager.RegisterRoutedEvent("SendData",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AsyncTcpClient));
        public event RoutedEventHandler SendData
        {
            add => AddHandler(SendEvent, value);
            remove => RemoveHandler(SendEvent, value);
        }

        //Событие получения данных по TCP соединению.
        private static readonly RoutedEvent ReceiveEvent = EventManager.RegisterRoutedEvent("ReceiveData",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AsyncTcpClient));
        public event RoutedEventHandler ReceiveData
        {
            add => AddHandler(ReceiveEvent, value);
            remove => RemoveHandler(ReceiveEvent, value);
        }

        //Событие разрыва соединения.
        private static readonly RoutedEvent DisconnectEvent = EventManager.RegisterRoutedEvent("DisconnectData",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AsyncTcpClient));
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
            var connectionResult = _mySocket.BeginConnect(_ipAddress, _port, null, null);
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
            catch (Exception ex)
            {
                MessageBox.Show("Source: " + ex.Source + ", Message: " + ex.Message);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).

                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                if (_instance != null)
                {
                    try
                    {
                        _instance.Close();
                    }
                    catch { }
                    _instance = null;
                }

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SimpleTcpClient() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
