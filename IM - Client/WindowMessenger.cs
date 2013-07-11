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
using FlashWindow;


namespace IM___Client
{
    public partial class WindowMessenger : Form
    {
        private delegate void SetTextBoxText(string text, string says = "");

        public WindowMessenger(string username, string name, MainClient.MessengerObjects mObj, string messages = "")
        {
            InitializeComponent();
            SendMessage = mObj.sm;
            MessageRecieved = new Client.ProcessReceivedMessagesDelegate(MessageProcessor);
            MainMsgReceiver = mObj.MainReceiver;
            MyName = name;
            NameTo = username;
            SetText(messages, username);
            this.Text = "IM - " + username;
            if (messages.Length > 0) Flash();
        }

        private string MyName
        {
            get;
            set;
        }

        private string NameTo
        {
            get;
            set;
        }

        public Client.ProcessReceivedMessagesDelegate MessageRecieved
        {
            get;
            set;
        }


        public Client.SendMessageDelegate SendMessage
        {
            get;
            set;
        }

        public Client.ProcessReceivedMessagesDelegate MainMsgReceiver
        {
            get;
            set;
        }
        /*******************************************************/
        /*******************************************************/
        /*******************************************************/
        /// <summary>
        /// Receive Message from main client
        /// </summary>
        /// <param name="message"></param>
        private void MessageProcessor(IM_Message message)
        {
            switch(message.Type)
            {
                case IM_Message.MESSAGE_TYPE_MSG:
                    SetText(message, message.From);
                    Flash();
                break;
                case IM_Message.MESSAGE_TYPE_CLIENT_DISCONNECTED:
                    msgTextBox.ReadOnly = true;
                    button1.Enabled = false;
                    this.Text += " - OFFLINE";
                break;
                case IM_Message.MESSAGE_TYPE_CLIENT_CONNECTED:
                    msgTextBox.ReadOnly = false;
                    button1.Enabled = true;
                    this.Text = this.Text.Replace(" - OFFLINE", String.Empty);
                break;
                
            }
        }

        //Send Message should be a delegate
        /*******************************************************/
        /*******************************************************/
        /*******************************************************/

        private void Flash()
        {
            if (this.WindowState == FormWindowState.Minimized || !this.Focused)
                FlashWindow.FlashWindow.Flash(this);
        }

        private void SetText(string text, string says = "")
        {
            if (text == String.Empty) return;

            if (this.mainOutputTxtBox.InvokeRequired)
            {
                SetTextBoxText s = new SetTextBoxText(SetText);
                this.Invoke(s, new object[] { text, says });
            }
            else
            {
                mainOutputTxtBox.AppendText(String.Format("{0} {1} says: {2}{3}", DateTime.Now.ToString(), says, text, Environment.NewLine + Environment.NewLine));
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SendMessage(new IM_Message(MyName, this.NameTo, IM_Message.MESSAGE_TYPE_MSG, msgTextBox.Text));
            SetText(msgTextBox.Text, MyName);
            msgTextBox.Clear();
        }

        private void WindowMessenger_FormClosed(object sender, FormClosedEventArgs e)
        {
            MainMsgReceiver(new IM_Message(MyName, "NONE", IM_Message.MESSAGE_TYPE_FORM_CLOSING, NameTo));
        }

        //private void 
    }
}
