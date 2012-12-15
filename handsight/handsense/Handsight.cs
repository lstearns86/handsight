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

        public delegate void UpdateDelegate(int mode, int[] values, char[] types, string text);
        public event UpdateDelegate Update;
        private void OnUpdate(int mode, int[] values, char[] types, string text) { if (Update != null) Update(mode, values, types, text); }

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
                            string modeString = line.Substring(0, line.IndexOf(':'));
                            string valueString = line.Substring(line.IndexOf(':')+1, line.IndexOf('|')-2);
                            string textString = line.Substring(line.IndexOf('|') + 1, line.IndexOf('\n') - line.IndexOf('|') - 1);
                            
                            int mode = 0;
                            int.TryParse(modeString, out mode);

                            string[] parts = valueString.Split(',');
                            int[] values = new int[6];
                            char[] types = new char[4];
                            for (int i = 0; i < Math.Min(6, parts.Length); i++)
                            {
                                if (i < 4)
                                {
                                    char type = parts[i][0];
                                    types[i] = type;
                                    int value = 0;
                                    int.TryParse(parts[i].Substring(1), out value);
                                    values[i] = value;
                                }
                                else
                                {
                                    int value = 0;
                                    int.TryParse(parts[i].Substring(1), out value);
                                    values[i] = value;
                                }
                            }

                            OnUpdate(mode, values, types, textString);
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

        public async void SetMode(int mode)
        {
            if (_socket == null || _dataWriter == null)
            {
                throw new InvalidOperationException("Handsense not connected");
            }

            WriteCommand(_dataWriter, (byte)mode);

            await _dataWriter.StoreAsync();
        }

        public async void Calibrate(int type)
        {
            if (_socket == null || _dataWriter == null)
            {
                throw new InvalidOperationException("Handsense not connected");
            }

            WriteCommand(_dataWriter, (byte)type);

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
        private void WriteCommand(DataWriter dataWriter, byte command)
        {
            if (_socket == null || _dataWriter == null)
            {
                throw new InvalidOperationException("Handsense not connected");
            }

            dataWriter.WriteByte(command);
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
