using System;
using System.Collections;
using System.Net;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using IM___Library;

namespace IM___Client
{
    public partial class Loading : Form
    {
        BackgroundWorker worker;

        public Loading()
        {
            InitializeComponent();

            worker = new System.ComponentModel.BackgroundWorker();
            worker.DoWork += new System.ComponentModel.DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
        }

        void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            this.Close();
        }

        void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            MainClient.hostToIP = LoadNetworkDevices();
        }

        private Dictionary<string, IPAddress> LoadNetworkDevices()
        {
            
            Dictionary<string, IPAddress> host_to_ip = new Dictionary<string, IPAddress>();

            ArrayList s = NetApi32.GetServerList(NetApi32.SV_101_TYPES.SV_TYPE_WORKSTATION);
            foreach (object c in s)
            {
                string host = ((NetApi32.SERVER_INFO_101)c).sv101_name;

                host_to_ip.Add(host, MainClient.GetIP(host));
            }

            return host_to_ip;
        }

        private void Loading_Shown(object sender, EventArgs e)
        {
            worker.RunWorkerAsync();
        }
    }
}
