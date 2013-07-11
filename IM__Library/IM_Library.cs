using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using Protocol;

namespace IM___Library
{
    #region Type Class
    public class Type
    {
        public delegate void MessageSendCallback(object obj);
        public const int SEND = 0,
                         RECEIVE = 1;
    }
    #endregion

    #region ClientList Collection
    public class ClientList : CollectionBase
    {
        public ClientList() { }

        public Client this[int index]
        {
            get
            {
                return (Client)List[index];
            }
            set
            {
                List[index] = value;
            }
        }

        public Client this[string name]
        {
            get
            {
                return this.WithName(name);
            }
        }

        public int Add(Client c)
        {
            return List.Add(c);
        }

        public void AddRange(Client[] obj)
        {
            foreach (Client c in obj)
            {
                Add(c);
            }
        }

        public void Remove(Client c)
        {
            List<Client> temp = new List<Client>();
            foreach (Client x in this)
            {
                if (x.Name != c.Name)
                {
                    temp.Add(x);                    
                }
            }
            this.List.Clear();
            AddRange(temp.ToArray<Client>());
        }

        public new void RemoveAt(int index)
        {
            List.RemoveAt(index);
        }

        public Client WithName(string name)
        {
            foreach (Client cl in List)
            {
                if (cl.Name == name)
                    return cl;
            }

            return null;
        }

    }
    #endregion

    #region Client Class
    public class Client
    {
        //Public Members
        public delegate void SendMessageDelegate(IM_Message message);
        public delegate void iAsyncCallBackDelegate(IAsyncResult ar);
        public delegate void ProcessReceivedMessagesDelegate(IM_Message msg);
        
        //Private Members
        //private StreamWriter writer;
        //private StreamReader reader;
        private delegate string ReceiveMessageDelegate();
        private BackgroundWorker background_worker = new BackgroundWorker();
        private ProcessReceivedMessagesDelegate MessageCallbackDelegate;

        private RijndaelManaged rijn;
        private Rfc2898DeriveBytes rfc_derived;
        private CryptoStream crypto_stream_reader, crypto_stream_writer;
        private ICryptoTransform encryptor, decryptor;

        private const int BUFFER_SIZE = 1024;



        public Client(TcpClient client, ProcessReceivedMessagesDelegate MessageCallback)
        {
            IntializeClient(client);
            MessageCallbackDelegate = MessageCallback;
        }

        /// <summary>
        /// Prepares Client for reading and writing to stream asychronously
        /// </summary>
        /// <param name="client">TcpClient </param>
        private void IntializeClient(TcpClient client)
        {
            tcp = client;
            //binary_formatter = new BinaryFormatter();

            if (tcp.Connected)
            {
                //writer = new StreamWriter(tcp.GetStream());
                //reader = new StreamReader(tcp.GetStream());

                //This will generate the key used to encrypt and decrypt stream
                rfc_derived = new Rfc2898DeriveBytes("TEMPORARY PASSWORD", new byte[] { 15, 23, 48, 100, 44, 50, 2, 10 }, 1500);
                //config Rijndael here
                rijn = new RijndaelManaged()
                {
                    BlockSize = 128,
                    KeySize = 256,
                    Key = rfc_derived.GetBytes(32),
                    IV = rfc_derived.GetBytes(16),
                    Mode = CipherMode.ECB,
                    Padding = PaddingMode.Zeros
                };

                encryptor = rijn.CreateEncryptor();
                decryptor = rijn.CreateDecryptor();

                //crypto_stream_writer = new CryptoStream(tcp.GetStream(), encryptor, CryptoStreamMode.Write);
                //crypto_stream_reader = new CryptoStream(tcp.GetStream(), decryptor, CryptoStreamMode.Read);

                background_worker.WorkerReportsProgress = true;
                background_worker.WorkerSupportsCancellation = true;
                background_worker.DoWork += new DoWorkEventHandler(background_worker_StartListening);
                background_worker.ProgressChanged += new ProgressChangedEventHandler(background_worker_ProgressChanged);

                try
                {
                    if (!background_worker.IsBusy)
                    {
                        background_worker.RunWorkerAsync();
                    }
                }
                catch (Exception) { }
            }
            else
                throw new InvalidOperationException("Client cannot connect to host");
        }

        public override bool Equals(object obj)
        {
            Client cl_temp = obj as Client;
            if (obj == null)
                return false;
            else
                return cl_temp.Name == Name;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public TcpClient tcp
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public void Close()
        {
            //writer.Close();
            //reader.Close();

            try
            {
                crypto_stream_writer.Close();
                crypto_stream_reader.Close();
            }
            catch (Exception) { }

            tcp.Close();
        }

        public void Send(IM_Message message)
        {
            SendMessageDelegate send = new SendMessageDelegate(SendMessage);

            IAsyncResult iRes = send.BeginInvoke(message, new AsyncCallback(iAsyncCallbackMethod), null);

            while (!iRes.AsyncWaitHandle.WaitOne(100, true)) ;


        }

        /************Background Worker Methods*************/
        #region Background Worker Threads
        public void background_worker_StartListening(object sender, DoWorkEventArgs e)
        {
            while (!background_worker.CancellationPending)
            {
                try
                {
                    IFormatter binary_formatter = new BinaryFormatter();
                    MemoryStream mem_str = new MemoryStream();
                    crypto_stream_reader = new CryptoStream(mem_str, decryptor, CryptoStreamMode.Write);

                    NetworkStream network_stream = new NetworkStream(tcp.Client);

                    byte[] size = new byte[4];
                    network_stream.Read(size, 0, 4);

                    int length = BitConverter.ToInt32(size, 0);

                    byte[] buffer = new byte[BUFFER_SIZE];
                    int offset = 0, bytes_read = 0;

                    while (length > 0)
                    {
                        bytes_read = network_stream.Read(buffer, 0, BUFFER_SIZE);
                        crypto_stream_reader.Write(buffer, 0, bytes_read);

                        offset += bytes_read;
                        length -= bytes_read;
                    }

                    crypto_stream_reader.Flush();
                    mem_str.Seek(0, SeekOrigin.Begin);

                    IM_Message im = (IM_Message)binary_formatter.Deserialize(mem_str);
                    if (im != null)
                        background_worker.ReportProgress(0, im);
                    
                }
                catch (Exception ex)
                {
                    //File.AppendAllText("log.txt", "Decrypting: " + ex.Message + Environment.NewLine);
                    //background_worker.CancelAsync();
                    //Close();
                }
            }
        }

        private void background_worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState != null)
                MessageCallbackDelegate((IM_Message)e.UserState);
        }
        #endregion
        /**************************************************/

        private void SendMessage(IM_Message message)
        {
            try
            {
                IFormatter binary_formatter = new BinaryFormatter();
                MemoryStream mem_str = new MemoryStream();
                NetworkStream network_stream = new NetworkStream(tcp.Client);

                crypto_stream_writer = new CryptoStream(mem_str, encryptor, CryptoStreamMode.Write);
                binary_formatter.Serialize(crypto_stream_writer, message);

                crypto_stream_writer.FlushFinalBlock();

                byte[] buffer = mem_str.GetBuffer();

                //Send the length
                network_stream.Write(BitConverter.GetBytes(buffer.Length), 0, 4);
                //Send the data
                network_stream.Write(buffer, 0, buffer.Length);
                network_stream.Flush();
                
            }
            catch (Exception ex)
            {
                //File.AppendAllText("log.txt", "Encrypting: " + ex.Message + Environment.NewLine);
                //Close();
            }
        }

        private void iAsyncCallbackMethod(IAsyncResult iAsync)
        {
            //
        }
    }
#endregion

    #region NetApi32
    public class NetApi32
    {
        // constants
        public const uint ERROR_SUCCESS = 0;
        public const uint ERROR_MORE_DATA = 234;
        public enum SV_101_TYPES : uint
        {
            SV_TYPE_WORKSTATION = 0x00000001,
            SV_TYPE_SERVER = 0x00000002,
            SV_TYPE_SQLSERVER = 0x00000004,
            SV_TYPE_DOMAIN_CTRL = 0x00000008,
            SV_TYPE_DOMAIN_BAKCTRL = 0x00000010,
            SV_TYPE_TIME_SOURCE = 0x00000020,
            SV_TYPE_AFP = 0x00000040,
            SV_TYPE_NOVELL = 0x00000080,
            SV_TYPE_DOMAIN_MEMBER = 0x00000100,
            SV_TYPE_PRINTQ_SERVER = 0x00000200,
            SV_TYPE_DIALIN_SERVER = 0x00000400,
            SV_TYPE_XENIX_SERVER = 0x00000800,
            SV_TYPE_SERVER_UNIX = 0x00000800,
            SV_TYPE_NT = 0x00001000,
            SV_TYPE_WFW = 0x00002000,
            SV_TYPE_SERVER_MFPN = 0x00004000,
            SV_TYPE_SERVER_NT = 0x00008000,
            SV_TYPE_POTENTIAL_BROWSER = 0x00010000,
            SV_TYPE_BACKUP_BROWSER = 0x00020000,
            SV_TYPE_MASTER_BROWSER = 0x00040000,
            SV_TYPE_DOMAIN_MASTER = 0x00080000,
            SV_TYPE_SERVER_OSF = 0x00100000,
            SV_TYPE_SERVER_VMS = 0x00200000,
            SV_TYPE_WINDOWS = 0x00400000,
            SV_TYPE_DFS = 0x00800000,
            SV_TYPE_CLUSTER_NT = 0x01000000,
            SV_TYPE_TERMINALSERVER = 0x02000000,
            SV_TYPE_CLUSTER_VS_NT = 0x04000000,
            SV_TYPE_DCE = 0x10000000,
            SV_TYPE_ALTERNATE_XPORT = 0x20000000,
            SV_TYPE_LOCAL_LIST_ONLY = 0x40000000,
            SV_TYPE_DOMAIN_ENUM = 0x80000000,
            SV_TYPE_ALL = 0xFFFFFFFF
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct SERVER_INFO_101
        {
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)]
            public UInt32 sv101_platform_id;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
            public string sv101_name;

            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)]
            public UInt32 sv101_version_major;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)]
            public UInt32 sv101_version_minor;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)]
            public UInt32 sv101_type;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
            public string sv101_comment;
        };
        public enum PLATFORM_ID
        {
            PLATFORM_ID_DOS = 300,
            PLATFORM_ID_OS2 = 400,
            PLATFORM_ID_NT = 500,
            PLATFORM_ID_OSF = 600,
            PLATFORM_ID_VMS = 700
        }

        [DllImport("netapi32.dll", EntryPoint = "NetServerEnum")]
        public static extern int NetServerEnum([MarshalAs(UnmanagedType.LPWStr)]string servername,
           int level,
           out IntPtr bufptr,
           int prefmaxlen,
           ref int entriesread,
           ref int totalentries,
           SV_101_TYPES servertype,
           [MarshalAs(UnmanagedType.LPWStr)]string domain,
           IntPtr resume_handle);

        [DllImport("netapi32.dll", EntryPoint = "NetApiBufferFree")]
        public static extern int
            NetApiBufferFree(IntPtr buffer);

        [DllImport("Netapi32", CharSet = CharSet.Unicode)]
        private static extern int NetMessageBufferSend(
            string servername,
            string msgname,
            string fromname,
            string buf,
            int buflen);

        public static int NetMessageSend(string serverName, string messageName, string fromName, string strMsgBuffer, int iMsgBufferLen)
        {
            return NetMessageBufferSend(serverName, messageName, fromName, strMsgBuffer, iMsgBufferLen * 2);
        }

        public static ArrayList GetServerList(NetApi32.SV_101_TYPES ServerType)
        {
            int entriesread = 0, totalentries = 0;
            ArrayList alServers = new ArrayList();

            do
            {
                // Buffer to store the available servers
                // Filled by the NetServerEnum function
                IntPtr buf = new IntPtr();

                SERVER_INFO_101 server;
                int ret = NetServerEnum(null, 101, out buf, -1,
                    ref entriesread, ref totalentries,
                    ServerType, null, IntPtr.Zero);

                // if the function returned any data, fill the tree view
                if (ret == ERROR_SUCCESS ||
                    ret == ERROR_MORE_DATA ||
                    entriesread > 0)
                {
                    IntPtr ptr = buf;

                    for (int i = 0; i < entriesread; i++)
                    {
                        // cast pointer to a SERVER_INFO_101 structure
                        server = (SERVER_INFO_101)Marshal.PtrToStructure(ptr, typeof(SERVER_INFO_101));

                        //Cast the pointer to a ulong so this addition will work on 32-bit or 64-bit systems.
                        ptr = (IntPtr)((ulong)ptr + (ulong)Marshal.SizeOf(server));

                        // add the machine name and comment to the arrayList.
                        //You could return the entire structure here if desired
                        alServers.Add(server);
                    }
                }

                // free the buffer
                NetApiBufferFree(buf);

            }
            while
                (
                entriesread < totalentries &&
                entriesread != 0
                );

            return alServers;
        }
    }
#endregion

    #region Settings
    [Serializable()]
    public class Settings : ISerializable
    {
        public const int SERVER = 0,
                         CLIENT = 1;

        public bool AutoConnect
        {
            get;
            set;
        }
        public bool AutoLocateHost
        {
            get;
            set;
        }
        public string Host
        {
            get;
            set;
        }
        public int Port
        {
            get;
            set;
        }
        public string Username
        {
            get;
            set;
        }
        public int Type
        {
            get;
            set;
        }

        public Settings(SerializationInfo info, StreamingContext context)
        {

        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {

        }
    }

    public class SettingsManager
    {

    }
    #endregion
}