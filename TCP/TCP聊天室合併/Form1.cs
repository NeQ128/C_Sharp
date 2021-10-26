using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Text.Json;

namespace TCP聊天室合併
{
    public struct MsgItem
    {
        public int num { get; set; }
        public string name { get; set; }
        public string message { get; set; }
    }

    public struct MemberItem
    {
        public string name { get; set; }
        public Socket socket { get; set; }
        public Thread thread { get; set; }
        public object item { get; set; }
    }
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            listBox1.DisplayMember = "name";
            CheckForIllegalCrossThreadCalls = false;
            textBox4.BackColor = textBox5.BackColor;
        }
        private void button_change(Button button)
        {
            button4.Enabled = !button.Enabled;
            button5.Enabled = !button.Enabled;
        }
        private bool check_input()
        {
            if (textBox1.Text == "")
            {
                MessageBox.Show("請輸入IP!!");
                return true;
            }
            if (textBox2.Text == "")
            {
                MessageBox.Show("請輸入Port!!");
                return true;
            }
            if (textBox3.Text == "")
            {
                MessageBox.Show("請輸入使用者名稱!!");
                return true;
            }
            return false;
        }
        private void close_input()
        {
            textBox1.Enabled = !textBox1.Enabled;
            textBox2.Enabled = !textBox2.Enabled;
            textBox3.Enabled = !textBox3.Enabled;
        }
        private void hold_change(Button button)
        {
            if (button.Enabled)
            {
                button2.Enabled = !button2.Enabled;
                button_change(button2);
                button1.Text = "關  閉";
                close_input();
            }
            else
            {
                button2.Enabled = !button2.Enabled;
                button_change(button2);
                button1.Text = "主  持";
                close_input();
            }
        }
        private void join_change(Button button)
        {
            if (button.Enabled)
            {
                button1.Enabled = !button1.Enabled;
                button_change(button1);
                button2.Text = "離  開";
                close_input();
            }
            else
            {
                button1.Enabled = !button1.Enabled;
                button_change(button1);
                button2.Text = "加  入";
                close_input();
            }
        }
        private string MyIP()
        {
            string hn = Dns.GetHostName();
            IPAddress[] ip_array = Dns.GetHostEntry(hn).AddressList;
            foreach (IPAddress ip in ip_array)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && ip.ToString().Substring(0, 7) != "192.168")
                {
                    return ip.ToString();
                }
            }
            MessageBox.Show("您的IP無法主持!!");
            return "";
        }
        private void screem_re(string str)
        {
            textBox4.Text += str;
            textBox4.SelectionStart = textBox4.Text.Length;
            textBox4.ScrollToCaret();
        }
        private string user_name;
        private TcpListener Server;
        private Thread Server_thread;
        private void button1_Click(object sender, EventArgs e)
        {
            if(button1.Text == "主  持")
            {
                textBox1.Text = MyIP();
                if(textBox1.Text == "") return;
                if (check_input()) return;
                user_name = textBox3.Text;
                Server_thread = new Thread(ServerSub);
                Server_thread.IsBackground = true;
                Server_thread.Start();
                MsgItem listItem = new MsgItem
                {
                    num = 0,
                    name = user_name
                };
                listBox1.Items.Add(listItem);
                hold_change(button2);
                string SendMsg = "(系統訊息) 伺服器已開啟，IP : " + MyIP() + "\r\n";
                screem_re(SendMsg);
            }
            else
            {
                Server.Stop();
                Server_thread.Abort();
                listBox1.Items.Clear();
                hold_change(button2);
                string SendMsg = "(系統訊息) 伺服器已關閉\r\n";
                screem_re(SendMsg);
            }
        }
        private Socket T;
        private Thread Client_Listen_thread;
        private void Send(string Str)
        {
            byte[] B = Encoding.Default.GetBytes(Str);
            T.Send(B, 0, B.Length, SocketFlags.None);
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.Text == "加  入")
            {
                if (check_input())  return;
                user_name = textBox3.Text;
                string IP = textBox1.Text;
                int Port = int.Parse(textBox2.Text);
                IPEndPoint EP = new IPEndPoint(IPAddress.Parse(IP), Port);
                T = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    T.Connect(EP);
                    Client_Listen_thread = new Thread(Client_Listen);
                    Client_Listen_thread.IsBackground = true;
                    Client_Listen_thread.Start();
                    textBox4.Text += "(系統訊息) 已連接伺服器!!\r\n";
                    Send("0" + user_name);
                }
                catch
                {
                    textBox4.Text += "(系統訊息) 無法連接伺服器!!\r\n";
                    return;
                }
                join_change(button1);
            }
            else
            {
                try 
                { 
                    Send("1" + user_name);
                }
                finally
                {
                    T.Close();
                }
                listBox1.Items.Clear();
                Client_Listen_thread.Abort();
                join_change(button1);
            }
        }
        private Socket Client_socket;
        private Thread Client_thread;
        private void ServerSub()
        {
            IPEndPoint EP = new IPEndPoint(IPAddress.Parse(textBox1.Text), int.Parse(textBox2.Text));
            Server = new TcpListener(EP);
            Server.Start(100);
            while (true)
            {
                Client_socket = Server.AcceptSocket();
                Client_thread = new Thread(Server_Listen);
                Client_thread.IsBackground = true;
                Client_thread.Start();
            }
        }
        private static int members = 1;
        private Hashtable MemberTable = new Hashtable();
        private void Server_Listen()
        {
            MemberItem memItem = new MemberItem();
            memItem.socket = Client_socket;
            memItem.thread = Client_thread;
            MsgItem msgItem = new MsgItem();
            while (true)
            {
                try
                {
                    byte[] B = new byte[1023];
                    int inLen = memItem.socket.Receive(B);
                    MsgItem onlineMem;
                    string Msg = Encoding.Default.GetString(B, 0, inLen);
                    string Cmd = Msg.Substring(0, 1);
                    string Str = Msg.Substring(1);
                    string SendMsg;
                    switch (Cmd)
                    {
                        case "0":
                            msgItem.num = members++;
                            msgItem.name = Str;
                            memItem.name = Str;
                            memItem.item = msgItem;
                            listBox1.Items.Add(memItem.item);
                            MemberTable.Add(msgItem.num, memItem);
                            SendMsg = $"(系統訊息) 使用者 : {memItem.name} 已加入聊天室\r\n";
                            screem_re(SendMsg);
                            onlineMem = new MsgItem();
                            onlineMem.message = OnlineList();
                            onlineMem.name = msgItem.name;
                            SendMsg = "L0" + JsonSerializer.Serialize(onlineMem);
                            SendAll(SendMsg);
                            break;
                        case "1":
                            SendMsg = $"(系統訊息) 使用者 : {memItem.name} 已離開聊天室\r\n";
                            screem_re(SendMsg);
                            listBox1.Items.Remove(memItem.item);
                            MemberTable.Remove(msgItem.num);
                            onlineMem = new MsgItem();
                            onlineMem.message = OnlineList();
                            onlineMem.name = memItem.name;
                            SendMsg = "L1" + JsonSerializer.Serialize(onlineMem);
                            SendAll(SendMsg);
                            memItem.socket.Close();
                            memItem.thread.Abort();
                            break;
                        case "9":
                            SendAll("9" + Str);
                            break;
                        default:
                            MsgItem msg = JsonSerializer.Deserialize<MsgItem>(Str);
                            SendTo(msg.message, msg.num);
                            break;
                    }
                }
                catch
                {

                }
            }
        }
        private void Client_Listen()
        {
            EndPoint ServerEP = (EndPoint)T.RemoteEndPoint;
            byte[] B = new byte[1023];
            int inLen;
            string Msg;
            string Cmd;
            string Str;
            while (true)
            {
                try
                {
                    inLen = T.ReceiveFrom(B, ref ServerEP);
                    Msg = Encoding.Default.GetString(B, 0, inLen);
                    Cmd = Msg.Substring(0, 1);
                    Str = Msg.Substring(1);
                    string SendMsg;
                    switch (Cmd)
                    {
                        case "L":
                            MsgItem msgGet = JsonSerializer.Deserialize<MsgItem>(Msg.Substring(2));
                            if (Msg.Substring(1, 1) == "0")
                            {
                                SendMsg = $"(系統訊息) 使用者 : {msgGet.name} 已加入聊天室\r\n";
                                screem_re(SendMsg);
                            }
                            else
                            {
                                SendMsg = $"(系統訊息) 使用者 : {msgGet.name} 已離開聊天室\r\n";
                                screem_re(SendMsg);
                            }
                            listBox1.Items.Clear();
                            MsgItem[] items = JsonSerializer.Deserialize<MsgItem[]>(msgGet.message);
                            foreach (MsgItem item in items)
                            {
                                listBox1.Items.Add(item);
                            }
                            break;
                        case "9":
                            screem_re(Str + "\r\n");
                            break;
                        case "2":
                            screem_re(Str + "\r\n");
                            break;
                    }
                }
                catch
                {
                    T.Close();
                    listBox1.Items.Clear();
                    Client_Listen_thread.Abort();
                    MessageBox.Show("與伺服器中斷連線!!");
                    join_change(button1);
                }
            }
        }
        private string OnlineList()
        {
            string msg = JsonSerializer.Serialize(listBox1.Items);
            return msg;
        }
        private void SendAll(string Str)
        {
            byte[] B = Encoding.Default.GetBytes(Str);
            List<int>  sendList = MemberTable.Keys.Cast<int>().ToList();
            for (int i = 0; i < sendList.Count; i++)
            {
                MemberItem m = (MemberItem)MemberTable[sendList[i]];
                try
                {
                    m.socket.Send(B, 0, B.Length, SocketFlags.None);
                }
                catch
                {
                    textBox4.Text += $"(系統訊息) 使用者 : {m.name} 已離開聊天室\r\n";
                    listBox1.Items.Remove(m.item);
                    m.socket.Close();
                    m.thread.Abort();
                    MsgItem listItem = new MsgItem();
                    listItem.name = m.name;
                    listItem.message = OnlineList();
                    string sendmsg = "L1" + JsonSerializer.Serialize(listItem);
                    MemberTable.Remove(sendList[i]);
                    SendAll(sendmsg);
                    continue;
                }
            }
        }
        private void SendTo(string Str, int member_num)
        {
            if (member_num == 0)
            {
                string SendMsg = Str + "\r\n";
                screem_re(SendMsg);
            }
            else
            {
                byte[] B = Encoding.Default.GetBytes("2" + Str);
                MemberItem mem = (MemberItem)MemberTable[member_num];
                mem.socket.Send(B, 0, B.Length, SocketFlags.None);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            listBox1.ClearSelected();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (textBox5.Text == "")
                return;
            if (listBox1.SelectedIndex < 0)
            {
                MessageBox.Show("請選擇傳送對象!!");
                return;
            }
            else
            {
                try
                {
                    MsgItem item = (MsgItem)listBox1.SelectedItem;
                    if (button1.Enabled)
                    {
                        if (item.num == 0)
                        {
                            MessageBox.Show("您正在發訊息給自己!!");
                            return;
                        }
                        string sev_mes = "2From " + user_name + " : " + textBox5.Text;
                        byte[] B = Encoding.Default.GetBytes(sev_mes);
                        MemberItem mem = (MemberItem)MemberTable[item.num];
                        mem.socket.Send(B, 0, B.Length, SocketFlags.None);
                    }
                    else
                    {
                        MsgItem msg = new MsgItem();
                        msg.message = "From " + user_name + " : " + textBox5.Text;
                        msg.num = item.num;
                        string msg_json = JsonSerializer.Serialize(msg);
                        Send("2" + msg_json);
                    }
                    string SendMsg = "To   " + item.name + " : " + textBox5.Text + "\r\n";
                    screem_re(SendMsg);
                    textBox5.Text = "";
                }
                catch
                {
                    MessageBox.Show("訊息發送失敗!!");
                }
            }
        }
        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                if (button1.Enabled)
                {
                    SendAll("9" + user_name + " to 所有人 : " + textBox5.Text);
                    string SendMsg = user_name + " to 所有人 : " + textBox5.Text + "\r\n";
                    screem_re(SendMsg);
                }
                else
                {
                    Send("9" + user_name + " to 所有人 : " + textBox5.Text);
                }
                textBox5.Text = "";
            }
            catch
            {
                MessageBox.Show("訊息發送失敗!!");
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (!button2.Enabled)
                {
                    Server.Stop();
                    Server_thread.Abort();
                }
                if (!button1.Enabled)
                {
                    Send("1" + user_name);
                    T.Close();
                    Client_Listen_thread.Abort();
                }
            }
            finally
            {
                Application.ExitThread();
            }
        }
    }
}
