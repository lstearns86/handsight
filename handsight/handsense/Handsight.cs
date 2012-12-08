using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace handsight
{
    class Handsight
    {
        private StreamSocket _socket = null;
        private DataWriter _dataWriter = null;
        private DataReader _dataReader = null;

        public Handsight(StreamSocket socket)
        {
            _socket = socket;
            _dataWriter = new DataWriter(_socket.OutputStream);
            _dataReader = new DataReader(_socket.InputStream);
            
            StartListening();
        }

        public delegate void UpdateDelegate(int[] values);
        public event UpdateDelegate Update;
        private void OnUpdate(int[] values) { if (Update != null) Update(values); }

        private char[] buffer = new char[256];
        int bufferIndex = 0;
        private void StartListening()
        {
            Task.Factory.StartNew(async () =>
            {
                while (_socket != null)
                {
                    try
                    {

                        uint count = await _dataReader.LoadAsync(1);
                        buffer[bufferIndex] = (char)_dataReader.ReadByte();
                        if (buffer[bufferIndex] == '\n')
                        {
                            // process line
                            buffer[bufferIndex-1] = '\n';
                            string line = new string(buffer);
                            string[] parts = line.Split(',');
                            int[] values = new int[5];
                            for (int i = 0; i < Math.Min(5, parts.Length); i++)
                            {
                                int value = 0;
                                int.TryParse(parts[i], out value);
                                values[i] = value;
                            }
                            OnUpdate(values);
                            bufferIndex = 0;
                        }
                        else
                        {
                            bufferIndex++;
                            if (bufferIndex >= 256) bufferIndex = 0;
                        }
                    }
                    catch { }
                }
            });
        }

        public async void ToggleVibrate()
        {
            if (_socket == null || _dataWriter == null)
            {
                throw new InvalidOperationException("Handsense not connected");
            }

            WriteCommand(_dataWriter, "V");

            await _dataWriter.StoreAsync();
        }

        private void WriteCommand(DataWriter dataWriter, string command)
        {
            if (_socket == null || _dataWriter == null)
            {
                throw new InvalidOperationException("Handsense not connected");
            }

            dataWriter.WriteString(command);
        }

        public void Stop(Dispatcher dispatcher)
        {
            try
            {
                if (_socket != null)
                {
                    _socket.Dispose();
                    _socket = null;
                }
            }
            catch (Exception f)
            {
                dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(String.Format("Error closing socket: = {0}", f.Message));
                });
            }
        }

        public bool IsConnected
        {
            get
            {
                return (_socket != null && _dataWriter != null);
            }
        }
    }
}
