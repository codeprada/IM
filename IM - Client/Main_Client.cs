using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.IO;

using IM___Library;
using Protocol;

namespace IM___Client
{
    public partial class MainClient : Form
    {
        private TcpClient tcp_client;
        private Client owner_client;
        private IPAddress ip { get; set; }
        private int Port { get; set; }
        private Client.ProcessReceivedMessagesDelegate ProcRMD;
        private string TempName;
        private bool NameSet = false;
        public static Dictionary<string, IPAddress> hostToIP;
        private bool messageReceived = false;
        private IPAddress server = null;
        private delegate void SetTextBoxText(string text);
        private object ListLock = new object();

        public struct MessengerObjects
        {
            //Messenger Window's Method to Receive new messages
            public Client.ProcessReceivedMessagesDelegate rm;

            //This window's method to send messages
            public Client.SendMessageDelegate sm;

            public Client.ProcessReceivedMessagesDelegate MainReceiver;
        }

        private Dictionary<string, MessengerObjects> MessengerStructure = new Dictionary<string,MessengerObjects>(); 

        public MainClient()
        {
            InitializeComponent();
            ProcRMD = new Client.ProcessReceivedMessagesDelegate(ProcessReceivedMessages);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            if (errorProvider1.GetError(usernameTxtBx) != String.Empty)
                return;

            connectBtn.Enabled = false;

            //if the server isn't found you want to try searching for it again
            //limiting rounds to 2. don't want an infinite loop here
            //of course we'll make a setting to increase or decrease the value
            if(server == null)
                StartSearching();

            if (server == null)
            {
                SetStatus("Cannot find server. Aborting Connection");
                connectBtn.Enabled = true;
                return;
            }

            int count = 0;
            while(true)
            {
                if (Connect(server, (int)ipPortTextBox.Value))
                {
                    SetStatus("Connecting to server. Please wait.");
                    break;
                }
                else
                {
                    count++;
                    SetStatus("Error connecting to server");
                    if (count >= 2)
                    {
                        SetStatus("Cannot find server");
                        connectBtn.Enabled = true;
                        break;
                    }
                }
            }
            
            
        }
        

        private bool Connect(string ip_addr, int port)
        {
            return Connect(ip_addr, port);
        }

        private bool Connect(IPAddress ip, int port)
        {
            try
            {
                Port = port;
                tcp_client = new TcpClient();
                IPEndPoint ipend = new IPEndPoint(ip, port);
                tcp_client.Connect(ipend);

                owner_client = new Client(tcp_client, ProcRMD);
                owner_client.Name = usernameTxtBx.Text;
                this.Text = owner_client.Name;
                NameSet = false;

                //connectBtn.Enabled = true;

                return tcp_client.Connected;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            connectBtn.Enabled = true;
            return false;
        }

        public void Send(IM_Message message)
        {
            if(NameSet)
                message.From = owner_client.Name; //It's from this username
            owner_client.Send(message);
        }

        private void ProcessReceivedMessages(object msg)
        {
            IM_Message message = (IM_Message)msg;

            //SetText("***************\nMessage Received: " + message + "\n***************");

            switch (message.Type)
            {
                case IM_Message.MESSAGE_TYPE_GETNAME:
                    Send(new IM_Message(owner_client.Name, "SERVER", IM_Message.MESSAGE_TYPE_SETNAME, owner_client.Name));
                    //Initialize(message);
                    break;
                case IM_Message.MESSAGE_TYPE_SETNAME:
                    //This is our temporary name
                    SetText("Temp Name: " + message);
                    TempName = message; //Implicit conversion here
                    Initialize(message);
                    break;
                case IM_Message.MESSAGE_TYPE_SETNAME_CONFIRMATION_OK:
                    //Only acknowledge name if it matches the one requested
                    if (message == owner_client.Name)
                    {
                        //Everything seemed to have went OK in the Name Acknowledgement
                        NameSet = true;
                        SetText("Name set to " + owner_client.Name);
                        SetStatus("Status: Connected");
                        EnableDisableControls(false);
                    }
                    else
                    {
                        SetStatus("Name could not be verified. Shutting down.");
                        SetText("Name could not be verified");
                        ShuttingDown();
                        EnableDisableControls(true);
                    }
                    break;
                case IM_Message.MESSAGE_TYPE_SETNAME_CONFIRMATION_NO:
                    //YOU MUST REQUEST ANOTHER NAME HERE
                    NameSet = false;
                    SetText("Name rejected");
                    ShuttingDown(); //disconnect from server
                    EnableDisableControls(true);
                    SetStatus("Request another name!");
                    break;
                case IM_Message.MESSAGE_TYPE_MSG:
                    MessageWindowRouter(message);
                    break;
                case IM_Message.MESSAGE_TYPE_CLIENT_LIST:
                    //Only will be called when the client connects first
                    SetText("Client List: " + (string)message);
                    UpdateClientList(((string)message).Split(new char[] { '*' }));
                    Send(new IM_Message(owner_client.Name, String.Empty, IM_Message.MESSAGE_TYPE_CLIENT_LIST_CONFIRMATION, String.Empty));
                    break;
                case IM_Message.MESSAGE_TYPE_FORM_CLOSING:
                    MessengerStructure.Remove(message);
                    break;
                case IM_Message.MESSAGE_TYPE_BROADCAST:
                    MessageBox.Show(message, "Broadcast From " + message.From, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case IM_Message.MESSAGE_TYPE_CLIENT_CONNECTED:
                    SetText(message + " connected.");
                    MessageWindowRouter(message);
                    ClientConnected((string)message);
                    break;
                case IM_Message.MESSAGE_TYPE_CLIENT_DISCONNECTED:
                    SetText(message + " disconnected.");
                    MessageWindowRouter(message);
                    ClientDisconnected((string)message);
                    break;
                case IM_Message.MESSAGE_TYPE_SERVER_DISCONNECTING:
                    //ShuttingDown();

                    SetText("Server disconnecting.");
                    SetStatus("Server Disconnected");
                    clientsListBox.Items.Clear();
                    EnableDisableControls(true);
                    break;
            }
        }

        private void Initialize(IM_Message message)
        {
            SetText("Initializing...");
            Send(new IM_Message(TempName, "SERVER", IM_Message.MESSAGE_TYPE_SETNAME, owner_client.Name));

        }

        private void MessageWindowRouter(IM_Message message)
        {
            try
            {
                if(MessengerStructure.ContainsKey(message.From))
                    MessengerStructure[message.From].rm(message);
                //we only want to Create a new Client if we are to receive anything the user must see
                else if(message.Type == IM_Message.MESSAGE_TYPE_MSG || message.Type == IM_Message.MESSAGE_TYPE_FILE)
                {
                    CreateClient(message.From, message);
                }
                else if (message.Type == IM_Message.MESSAGE_TYPE_CLIENT_CONNECTED || message.Type == IM_Message.MESSAGE_TYPE_CLIENT_DISCONNECTED)
                {
                    if (MessengerStructure.ContainsKey((string)message))
                        MessengerStructure[(string)message].rm(message);
                }
            }
            catch (Exception)
            {
                //MessageBox.Show(e.Message);
            }
        }

        private void SetText(string text)
        {
            if (this.outputTextBox.InvokeRequired)
            {
                SetTextBoxText s = new SetTextBoxText(SetText);
                this.Invoke(s, new object[] { text });
            }
            else
            {
                outputTextBox.AppendText(text + Environment.NewLine);
            }
        }

        private void UpdateClientList(string[] client_list)
        {
            Monitor.Enter(ListLock);

            clientsListBox.Items.Clear();
            if (client_list.Length == 1 && client_list[0] == String.Empty)
                return;


            clientsListBox.Items.AddRange(
                new List<string>(client_list).Cast<string>().Distinct().Where(
                    x => x != owner_client.Name 
                        && 
                    x != String.Empty).ToArray()
            );

            Monitor.Exit(ListLock);
        }

        private void ClientConnected(string client_name)
        {
            if(!clientsListBox.Items.Contains(client_name))
                clientsListBox.Items.Add(client_name);
        }

        private void ClientDisconnected(string client_name)
        {
            clientsListBox.Items.Remove(client_name);
        }

        private void SetStatus(string status)
        {
            toolStripStatusLabel1.Text = status;
        }

        private void clientsListBox_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                if (clientsListBox.SelectedItem.ToString() != "")
                    CreateClient(clientsListBox.SelectedItem.ToString());
            }
            catch (Exception)
            {
 
            }
        }

        private void CreateClient(string username, string messages = "")
        {
            Client.SendMessageDelegate _sm = new Client.SendMessageDelegate(Send);
            MessengerObjects obj = new MessengerObjects()
            {
                sm = _sm,
                MainReceiver = new Client.ProcessReceivedMessagesDelegate(ProcessReceivedMessages)
            };
            WindowMessenger winMess = new WindowMessenger(username, owner_client.Name, obj, messages);
            obj.rm = winMess.MessageRecieved;

            MessengerStructure.Add(username, obj);

            winMess.Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ShuttingDown();
            EnableDisableControls(true);
        }

        private void EnableDisableControls(bool status)
        {
            connectBtn.Enabled = status;
            ipPortTextBox.Enabled = status;
            usernameTxtBx.Enabled = status;
            disconnectBtn.Enabled = !status;
        }

        private void ShuttingDown()
        {
            if (owner_client != null)
            {
                string name;
                if (!NameSet)
                    name = TempName;
                else
                    name = owner_client.Name;


                usernameTxtBx.CausesValidation = false;
                Send(new IM_Message(name, "SERVER", IM_Message.MESSAGE_TYPE_CLIENT_SHUTTING_DOWN, "SHUTTING DOWN"));
                owner_client.tcp.Close();
            }

            NameSet = false;
            SetStatus("Disconnected From Server");
            EnableDisableControls(false);
            clientsListBox.Items.Clear();
        }

        private void MainClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                ShuttingDown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }        

        public static IPAddress GetIP(string host)
        {
            IPAddress[] addresses = Array.FindAll(Dns.GetHostEntry(host).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
            
            return addresses[0];
            
        }

        private void MainClient_Shown(object sender, EventArgs e)
        {
            Loading load = new Loading();
            load.ShowDialog();
        }

        private void usernameTxtBx_Validating(object sender, CancelEventArgs e)
        {
            if (usernameTxtBx.Text == String.Empty)
                errorProvider1.SetError(usernameTxtBx, "We need to know your name first");
            else
                errorProvider1.SetError(usernameTxtBx, String.Empty);
        }

        private void StartSearching()
        {
            SetStatus("Searching... Be patient");
            Thread t = new Thread(new ThreadStart(Search));
            t.Start();
            messageReceived = false;
            server = null;
            int counter = 0;
            while (!messageReceived && server == null)
            {
                Thread.Sleep(250);
                if (counter++ >= 20)
                    break;
            }
        }

        private void Search()
        {


            /*UdpClient udp_client = new UdpClient(); 
            IPEndPoint ip_endpoint = new IPEndPoint(IPAddress.Parse(Protocol.IM_Message.MULTI_CAST_ADDRESS), Protocol.IM_Message.UDP_PORT);
            udp_client.JoinMulticastGroup(IPAddress.Parse(Protocol.IM_Message.MULTI_CAST_ADDRESS));



            byte[] data = ASCIIEncoding.ASCII.GetBytes(IM_Message.MESSAGE_TYPE_HELLO);
            udp_client.Send(data, data.Length, ip_endpoint);

            //NOTE close socket in the Closing Event
            string received_data = ASCIIEncoding.ASCII.GetString(udp_client.Receive(ref ip_endpoint));
            if (received_data == IM_Message.MESSAGE_TYPE_CONFIRMATION)
            {
                server = ip_endpoint.Address;
                messageReceived = true;
            }
            else
            {
                server = IPAddress.Parse("127.0.0.1");
            }*/




            List<WaitHandle> wait_handles = new List<WaitHandle>();

            string ip_base = String.Join(".", GetIP(Dns.GetHostName()).ToString().Split(new char[] { '.' }).Take(3)) + ".";

            PingOptions po = new PingOptions();
            po.Ttl = 64;
            po.DontFragment = true;

            for (int i = 1; i < 256; i++)
            {
                Ping p = new Ping();
                p.PingCompleted += p_PingCompleted;

                AutoResetEvent r_event = new AutoResetEvent(false);
                wait_handles.Add(r_event);

                p.SendAsync(ip_base + i, 500, ASCIIEncoding.ASCII.GetBytes(IM_Message.MESSAGE_TYPE_HELLO), r_event);
            }


            WaitHandle.WaitAll(wait_handles.ToArray());
            
            
        }

        void p_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                if (ASCIIEncoding.ASCII.GetString(e.Reply.Buffer) == IM_Message.MESSAGE_TYPE_CONFIRMATION)
                {
                    server = e.Reply.Address;
                }
            }

            ((AutoResetEvent)e.UserState).Set();
        }

        
    }
}
