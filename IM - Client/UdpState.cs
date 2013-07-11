using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace IM___Client
{
    class UdpState
    {
        public IPEndPoint e;
        public UdpClient u;

        public UdpState() { }
    }
}
