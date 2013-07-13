using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Protocol
{
    #region Message Class
    [Serializable()]
    public class IM_Message : ISerializable
    {
        // <from>|<to>|type|message
        public const int MESSAGE_TYPE_LOGIN = 0,
                         MESSAGE_TYPE_LOGOUT = 1,
                         MESSAGE_TYPE_MSG = 2,
                         MESSAGE_TYPE_AUTH = 3,
                         MESSAGE_TYPE_GETNAME = 4,
                         MESSAGE_TYPE_SETNAME = 5,
                         MESSAGE_TYPE_BROADCAST = 6,
                         MESSAGE_TYPE_CLIENT_LIST = 7,
                         MESSAGE_TYPE_FORM_CLOSING = 8,
                         MESSAGE_TYPE_SERVER_SHUTTING_DOWN = 9,
                         MESSAGE_TYPE_CLIENT_SHUTTING_DOWN = 10,
                         MESSAGE_TYPE_COMMAND = 11,
                         MESSAGE_TYPE_RESPONSE = 12,
                         MESSAGE_TYPE_GET_FILE = 13,
                         MESSAGE_TYPE_FILE = 14,
                         MESSAGE_TYPE_CLIENT_CONNECTED = 15,
                         MESSAGE_TYPE_CLIENT_DISCONNECTED = 16,
                         MESSAGE_TYPE_SETNAME_CONFIRMATION_OK = 17,
                         MESSAGE_TYPE_SETNAME_CONFIRMATION_NO = 18,
                         MESSAGE_TYPE_CLIENT_NAME_CHANGE = 19,
                         MESSAGE_TYPE_SERVER_DISCONNECTING = 20,
                         MESSAGE_TYPE_CLIENT_LIST_CONFIRMATION = 21,

                         UDP_PORT = 9999;

        public const string MESSAGE_TYPE_HELLO = "*&(00^+1268^%^r462^587%1KILL",
                            MESSAGE_TYPE_CONFIRMATION = "f_773__s%^4549",
                            MULTI_CAST_ADDRESS = "224.0.0.224";


        public IM_Message(string from, string to, int type, byte[] data)
        {
            Initialize(from, to, type, data);
        }

        public IM_Message(string from, string to, int type, string data)
        {
            Initialize(from, to, type, GetBytes(data));
        }

        public IM_Message(SerializationInfo info, StreamingContext context)
        {
            From = (string)info.GetValue("From", typeof(string));
            To = (string)info.GetValue("To", typeof(string));
            Type = (int)info.GetValue("Type", typeof(int));
            Data = (byte[])info.GetValue("Data", typeof(byte[]));

        }

        private void Initialize(string from, string to, int type, byte[] data)
        {
            From = from;
            To = to;
            Type = type;
            Data = data;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("From", From);
            info.AddValue("To", To);
            info.AddValue("Type", Type);
            info.AddValue("Data", Data);
        }

        public string To
        {
            set;
            get;
        }

        public string From
        {
            set;
            get;
        }

        public int Type
        {
            set;
            get;
        }

        public byte[] Data
        {
            set;
            get;
        }

        public static byte[] GetBytes(string msg)
        {
            return Encoding.UTF8.GetBytes(msg);
        }

        public static string GetString(byte[] msg)
        {
            return Encoding.UTF8.GetString(msg);
        }

        public static implicit operator string(IM_Message message)
        {
            return IM_Message.GetString(message.Data);
        }

        /*public static explicit operator string(IM_Message message)
        {
            return IM_Message.GetString(message.Data);
        }*/

        public static implicit operator byte[](IM_Message message)
        {
            return message.Data;
        }

        /*public override string ToString()
        {
            return String.Format("{0}|{1}|{2}|{3}",Type.ToString(), );
        }*/

        public byte[] GetBytes()
        {
            return ASCIIEncoding.ASCII.GetBytes(ToString());
        }

    }
    #endregion
}
