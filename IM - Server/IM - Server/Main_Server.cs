using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

using IM___Library;
using Protocol;


namespace IM___Server
{
    public partial class Main_Server : Form
    {
        private TcpListener listener;
        private Random random;
        private delegate void SetTextBoxText(string text);
        private delegate void SetClientListBox();
        
        private bool stop_listening = true;
                
        private ClientList clients = new ClientList();
        private UdpClient udp_client;
        private Thread udp_thread;

        public Client.ProcessReceivedMessagesDelegate MessageCallback;

        private struct ClientUpdateStruct
        {
            public string name;
            public int type;
            public string extra;
        }
        
        public Main_Server()
        {
            InitializeComponent();

            MessageCallback = new Client.ProcessReceivedMessagesDelegate(ProcessReceivedMessages);

            tcpPortNumeric.Value = 8080;
            udpPortNumeric.Value = 9999;

            random = new Random(DateTime.Now.Millisecond);
        }

        private void Initialize_Server(int port)
        {
            try
            {
                //IPAddress ip = IPAddress.Parse(ip_addr);
                listener = new TcpListener(IPAddress.Any, port);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            stop_listening = false;
            Initialize_Server((int)tcpPortNumeric.Value);

            if (listener == null) return;
            Thread tcp = new Thread(new ThreadStart(StartListening));
            tcp.Start();

            udp_thread = new Thread(new ThreadStart(StartListeningIdentity));
            udp_thread.Start();

            SetText("Listening...");
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

        private void ReloadClients()
        {
            foreach (Client c in clients)
            {
                if (c.tcp.Client != null && !c.tcp.Client.Poll(100, SelectMode.SelectWrite))
                {
                    DisconnectFrom(c.Name);
                }
            }
            
        }

        private void LoadListToBox()
        {
            if (this.clientsListBox.InvokeRequired)
            {
                SetClientListBox s = new SetClientListBox(LoadListToBox);
                this.Invoke(s);
            }
            else
            {
                clientsListBox.Items.Clear();
                foreach (Client c in clients)
                {
                    clientsListBox.Items.Add(c.Name);
                }
            }
        }

        /*******************************************************************/
        /// <summary>
        /// Brain of Server
        /// </summary>
        /// <param name="message"></param>
        private void ProcessReceivedMessages(IM_Message message)
        {
            Client sender = clients.WithName(message.From);
            Client receiver = clients.WithName(message.To);

            //if (sender == null || receiver == null) return;
            //SetText("Message Received from " + message.From + ": " + message.Msg);
            switch (message.Type)
            {
                case IM_Message.MESSAGE_TYPE_SETNAME:
                    //Client will not use the name requested until the server has confirmed they can
                    int type;
                    //check to see if requested name is available
                    if (clients.WithName(message) == null || ((string)message == String.Empty))
                    {
                        SetText(sender.Name + ": SetName: '" + message + "' approved");
                        type = IM_Message.MESSAGE_TYPE_SETNAME_CONFIRMATION_OK;
                        sender.Send(new IM_Message("SERVER", String.Empty, type, message.Data));
                        
                        //Thread.Sleep(1000); //slow things down
                        SynchronizeClient(message.From, message);
                    }
                    else
                    {
                        type = IM_Message.MESSAGE_TYPE_SETNAME_CONFIRMATION_NO;
                        sender.Send(new IM_Message("SERVER", String.Empty, type, message.Data));
                        SetText(sender.Name + ": SetName: '" + message + "' REJECTED");
                    }

                    //Send back name confirmation to Client
                    
                    break;

                case IM_Message.MESSAGE_TYPE_MSG:
                    receiver.Send(new IM_Message(sender.Name, receiver.Name, IM_Message.MESSAGE_TYPE_MSG, message.Data));
                    break;

                case IM_Message.MESSAGE_TYPE_CLIENT_SHUTTING_DOWN:
                    DisconnectFrom(message.From);        
                    break;

                default:
                    break;
            }
        }

        private void DisconnectFrom(string name)
        {
            try
            {
                Client c;
                if ((c = clients.WithName(name)) != null)
                {
                    c.Send(new IM_Message("SERVER", String.Empty, IM_Message.MESSAGE_TYPE_SERVER_DISCONNECTING, String.Empty));
                    c.tcp.Close();
                    clients.Remove(c);
                    LoadListToBox();
                }

                ClientUpdateStruct custruct = new ClientUpdateStruct
                {
                    name = name,
                    type = IM_Message.MESSAGE_TYPE_CLIENT_DISCONNECTED
                };

                //Send a CLIENT DISCONNECTED message to all clients
                new Thread(new ParameterizedThreadStart(SendClientListUpdateToAll)).Start(custruct);

                SetText(name + " disconnected");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void SynchronizeClient(string old_name, string new_name)
        {
            try
            {

                Client client = clients.WithName(old_name);
                client.Name = new_name;
               
                //Must send the entire list to the new client
                SendClientList(client);

                //Give client 2 seconds before sending out list
                Thread.Sleep(2000);

                ClientUpdateStruct custruct = new ClientUpdateStruct
                {
                    name = new_name,
                    type = IM_Message.MESSAGE_TYPE_CLIENT_CONNECTED
                };

                
                //Send an update to the other clients
                Thread t2 = new Thread(new ParameterizedThreadStart(SendClientListUpdateToAll));
                t2.Start(custruct);
                
                LoadListToBox();
                SetText(String.Format("{0} connected", new_name));
                //SetText("Set client name to " + new_name);
            }
            catch (Exception)
            {

            }
        }

        private void RequestClientName(object client)
        {
            Client t = client as Client;
            //SetText("Requesting name from " + t.Name);
            t.Send(new IM_Message("SERVER", t.Name, IM_Message.MESSAGE_TYPE_GETNAME, ""));
        }

        private void SendClientListUpdateToAll(object custruct)
        {
            ClientUpdateStruct cu_struct = (ClientUpdateStruct)custruct;
            IM_Message message = null;

            switch (cu_struct.type)
            {
                case IM_Message.MESSAGE_TYPE_CLIENT_CONNECTED:
                    message = new IM_Message("SERVER", String.Empty, IM_Message.MESSAGE_TYPE_CLIENT_CONNECTED, cu_struct.name);
                    break;
                case IM_Message.MESSAGE_TYPE_CLIENT_DISCONNECTED:
                    message = new IM_Message("SERVER", String.Empty, IM_Message.MESSAGE_TYPE_CLIENT_DISCONNECTED, cu_struct.name);
                    break;
                case IM_Message.MESSAGE_TYPE_CLIENT_NAME_CHANGE:
                    //Message will be intercepted so that it's not sent to the client in the TO field
                    //NAME_CHANGE messages will not be forwarded
                    message = new IM_Message(cu_struct.name, String.Empty, IM_Message.MESSAGE_TYPE_CLIENT_NAME_CHANGE, cu_struct.extra);
                    break;
                default:
                    break;
            }

            List<Client> buffer = new List<Client>(clients.Cast<Client>());
            foreach (Client c in buffer)
            {
                if (cu_struct.name != c.Name)
                {
                    //Set the TO field here
                    message.To = c.Name;
                    c.Send(message);
                }
            }
            
        }

        /// <summary>
        /// Sends list to newly connected Client
        /// </summary>
        /// <param name="client"></param>
        private void SendClientList(Client client)
        {
            string clients_string = String.Empty;

            foreach (Client c in clients)
                if(client.Name != c.Name)
                    clients_string += c.Name + "*";
            
            client.Send(new IM_Message("SERVER", String.Empty, IM_Message.MESSAGE_TYPE_CLIENT_LIST, clients_string));
            
        }

        /*******************************************************************/

        public string LocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    localIP = ip.ToString();
                
            }
            return localIP;
        }

        private void StartListening()
        {
            
            while (!stop_listening)
            {
                Thread.Sleep(500);
                listener.Start();

                if (listener.Pending())
                {
                    Client c = new Client(listener.AcceptTcpClient(), MessageCallback);
                    c.Name = "Client_" + random.Next(300, 1000000).ToString();
                    clients.Add(c);
                    //Give client a temporary name based on the time they connected
                    SetText("Name: '" + c.Name + "' is attempting to perform handshake");
                    c.Send(new IM_Message("SERVER", String.Empty, IM_Message.MESSAGE_TYPE_SETNAME, c.Name));
                    
                }
            }
            listener.Stop();
        }

        private void StartListeningIdentity()
        {
            IPEndPoint ip_endpoint = new IPEndPoint(IPAddress.Any, Protocol.IM_Message.UDP_PORT);

            udp_client = new UdpClient(); //new UdpClient(Protocol.IM_Message.UDP_PORT);
            udp_client.ExclusiveAddressUse = false;
            udp_client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp_client.Client.Bind(ip_endpoint);
            udp_client.JoinMulticastGroup(IPAddress.Parse(Protocol.IM_Message.MULTI_CAST_ADDRESS));

            byte[] b_data = ASCIIEncoding.ASCII.GetBytes(IM_Message.MESSAGE_TYPE_HELLO);

            while (!stop_listening)
            {
                try
                {
                    byte[] received_data = udp_client.Receive(ref ip_endpoint);
                    if (ASCIIEncoding.ASCII.GetString(received_data) == IM_Message.MESSAGE_TYPE_HELLO)
                    {
                        byte[] temp = ASCIIEncoding.ASCII.GetBytes(IM_Message.MESSAGE_TYPE_CONFIRMATION);
                        udp_client.Send(temp, temp.Length, ip_endpoint);
                    }
                }
                catch (SocketException)
                {

                }
                Thread.Sleep(150);
            }


            /*udp_client = new UdpClient((int)numericUpDown1.Value);
            IPEndPoint ip_endpoint = new IPEndPoint(IPAddress.Any, 0);
            string data;
            byte[] b_data;

            while (!stop_listening)
            {
                Thread.Sleep(150);
                // = ASCIIEncoding.ASCII.GetBytes("HELLO IM - SERVER?")
                try
                {
                    b_data = udp_client.Receive(ref ip_endpoint);
                    data = ASCIIEncoding.ASCII.GetString(b_data, 0, b_data.Length);
                    if (data == IM_Message.MESSAGE_TYPE_HELLO)
                    {
                        byte[] temp = ASCIIEncoding.ASCII.GetBytes(IM_Message.MESSAGE_TYPE_CONFIRMATION);
                        
                    }
                }
                catch (SocketException)
                {

                }
            }*/
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (Client c in clients)
                c.Send(new IM_Message("SERVER", c.Name, IM_Message.MESSAGE_TYPE_BROADCAST, broadcastTextBox.Text));
            
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!stop_listening)
            {
                Visible = false;
                notifyIcon1.Visible = true;
                e.Cancel = true;
            }
            else
                Stop();
        }

        private void Stop()
        {
            stop_listening = true;
            
            SetText("Closing connections");
            ReloadClients();
            List<string> names = new List<string>();
            foreach (Client c in clients)
                names.Add(c.Name);

            foreach (string n in names)
                DisconnectFrom(n);

            try
            {
                udp_client.Client.Close();
            }
            catch (Exception)
            {
 
            }
                       
        }

        private void kickBtn_Click(object sender, EventArgs e)
        {
            if (clientsListBox.SelectedItem.ToString() != "")
                DisconnectFrom(clientsListBox.SelectedItem.ToString());
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Stop();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Visible = true;
            //this.notifyIcon1.Visible = false;
        }
    }
}
