using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;


namespace TesteListagemPortas.ViewModel
{
    public class MainPageViewModel : ViewModelBase
    {
        private CancellationTokenSource _readCancellationTokenSource;
        private DataWriter _dataWriter;
        private DataReader _dataReader;
        private SerialDevice _serialPort;
        private DeviceInformation _selectedDevice;
        private string _comando;
        private string _comandoRecebido;
        private string _id;
        private ObservableCollection<DeviceInformation> _listOfDevices = new ObservableCollection<DeviceInformation>();

        public ICommand Refresh { get; set; }
        public ICommand ConnectCommand { get; set; }
        public ICommand FecharPortaCommand { get; set; }
        public ICommand EnviarComandoCommand { get; set; }


        public ObservableCollection<DeviceInformation> Devices
        {
            get { return _listOfDevices; }
            set
            {
                _listOfDevices = value;
                RaisePropertyChanged();                
            }
        }

        public DeviceInformation SelectedDevice
        {
            get { return _selectedDevice; }
            set
            {
                _selectedDevice = value;
                RaisePropertyChanged();
                ConnectCommand.CanExecute(null);
                EnviarComandoCommand.CanExecute(null);
            }
        }

        public string Comando
        {
            get { return _comando; }
            set
            {
                _comando = value;
                RaisePropertyChanged();
            }
        }

        public string ComandoRecebido
        {
            get { return _comandoRecebido; }
            set
            {
                _comandoRecebido = value;
                RaisePropertyChanged();
                CancelReadTask();
            }
        }

        public string Id
        {
            get { return _id; }
            set
            {
                _id = value;
                RaisePropertyChanged();                     
            }
        }


        public MainPageViewModel()
        {
            Refresh = new RelayCommand(CarregarListaUsb);
            ConnectCommand = new RelayCommand(AbrirPorta, PodeAbrirPorta);
            FecharPortaCommand = new RelayCommand(FecharPorta, PodeFecharPorta);
            EnviarComandoCommand = new RelayCommand(EnviarComando, PodeEnviarComando);
            CarregarListaUsb();
        }

        private async void EnviarComando()
        {
            // Create the DataWriter object and attach to OutputStream   
            _dataWriter = new DataWriter(_serialPort.OutputStream);

            //Launch the WriteAsync task to perform the write
            await WriteAsync();

            // ..

            _dataWriter.DetachStream();
            _dataWriter = null;

            EscutarPorta();
        }

        private async Task WriteAsync()
        {
            Task<UInt32> storeAsyncTask;

            _dataWriter.WriteString(Comando);
            storeAsyncTask = _dataWriter.StoreAsync().AsTask();
        }

        private bool PodeEnviarComando()
        {
            //return _serialPort != null;
            return true;
        }

        private void FecharPorta()
        {
            try
            {
                CancelReadTask();
                CloseDevice();
                CarregarListaUsb();
            }
            catch (Exception ex)
            {
            }

        }

        private void CloseDevice()
        {
            if (_serialPort != null)
            {
                _serialPort.Dispose();
            }
        }

        private bool PodeFecharPorta()
        {
            //return _serialPort != null;
            return true;
        }

        private async void AbrirPorta()
        {
            try
            {
                _serialPort = await SerialDevice.FromIdAsync(SelectedDevice.Id);

                // Configure serial settings
                _serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                _serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                _serialPort.BaudRate = 115200;
                _serialPort.Parity = SerialParity.None;
                _serialPort.StopBits = SerialStopBitCount.One;
                _serialPort.DataBits = 8;                
                EscutarPorta();
            }
            catch (Exception ex)
            {
                // ...
            }
        }

        private async void EscutarPorta()
        {
            try
            {
                if (_serialPort != null)
                {                    
                    _dataReader = new DataReader(_serialPort.InputStream);
                    _readCancellationTokenSource = new CancellationTokenSource();
                    // keep reading the serial input
                    while (true)
                    {
                        await ReadAsync(_readCancellationTokenSource.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.WriteLine("Entrou");
            }
        }

        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 1024;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
            _dataReader.InputStreamOptions = InputStreamOptions.Partial;

            // Create a task object to wait for data on the serialPort.InputStream
            loadAsyncTask = _dataReader.LoadAsync(ReadBufferLength).AsTask(cancellationToken);

            // Launch the task and wait
            UInt32 bytesRead = await loadAsyncTask;
            if (bytesRead > 0)
            {
                ComandoRecebido = _dataReader.ReadString(bytesRead);                
            }
        }

        private bool PodeAbrirPorta()
        {
            //return SelectedDevice != null;
            return true;
        }

        private async void CarregarListaUsb()
        {
            try
            {
                var myDevices = SerialDevice.GetDeviceSelector("UART0");
                var a = await DeviceInformation.FindAllAsync(myDevices);

                var aqs = SerialDevice.GetDeviceSelector();
                var dis = await DeviceInformation.FindAllAsync(aqs);

                Devices.Clear();

                foreach (var device in dis)
                    Devices.Add(device);
            }
            catch (Exception ex)
            {
                //status.Text = ex.Message;
            }
        }

        private void CancelReadTask()
        {
            if (_readCancellationTokenSource != null)
            {
                if (!_readCancellationTokenSource.IsCancellationRequested)
                {
                    _readCancellationTokenSource.Cancel();
                }
            }
        }
    }
}
