using System;
//Configuration
using System.Linq;
using System.Xml.Linq;
//Database
using System.Data;
using System.Data.OleDb;
//ErrorMessages
using System.Windows.Forms;
//Logging
using System.IO;
//TCP
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace SEALib
{
    public static class Configuration
    {
        private static XDocument xDoc;
        private static string loadedFile;
        private static bool errorEncountered = false;
        public static void Init(string filename)
        {
            try
            {
                loadedFile = filename;
                xDoc = XDocument.Load(loadedFile);
                errorEncountered = false;
            }
            catch (Exception ex)
            {
                //CONFIG NOT FOUND
                errorEncountered = true;
            }
        }
        public static void Save()
        {
            if (!errorEncountered)
            {
                try
                {
                    xDoc.Save(loadedFile);
                }
                catch (Exception ex)
                {
                    //COULD NOT SAVE CONFIG
                }
            }
        }
        public static void SaveAs(string filename)
        {
            if (!errorEncountered)
            {
                try
                {
                    xDoc.Save(filename);
                }
                catch (Exception ex)
                {
                    //COULD NOT SAVE CONFIG
                }
            }
        }
        public static string GetString(string parent, string name)
        {
            if (!errorEncountered)
            {
                try
                {
                    return xDoc.Descendants().Where(x => x.Name == name && x.Parent.Name == parent).Single().Value;
                }
                catch (Exception ex)
                {
                    //COULD NOT RETRIEVE FROM DOCUMENT
                    errorEncountered = true;
                }
            }
            return null;
        }
        public static bool Exists(string parent, string name)
        {
            if (!errorEncountered)
            {
                try
                {
                    return xDoc.Descendants().Where(x => x.Name == name && x.Parent.Name == parent).Any();
                }
                catch (Exception ex)
                {
                    //COULD NOT RETRIEVE FROM DOCUMENT
                }
            }
            return false;
        }
        public static void Set(string parent, string name, string value)
        {
            if (!errorEncountered)
            {
                try
                {
                    if (Exists(parent, name))
                    {
                        xDoc.Descendants().Where(x => x.Name == name && x.Parent.Name == parent).Single().SetValue(value);
                    }
                    else
                    {
                        xDoc.Descendants().Where(x => x.Name == parent).Single().Add(new XElement(name, value));
                    }
                }
                catch (Exception ex)
                {
                    //COULD NOT SET TO DOCUMENT
                }
            }
        }
        public static void Remove(string parent, string name)
        {
            if (!errorEncountered)
            {
                try
                {
                    xDoc.Descendants().Where(x => x.Name == name && x.Parent.Name == parent).Single().Remove();
                }
                catch (Exception ex)
                {
                    //NODE NOT FOUND
                }
            }
        }
    }
    public static class Database
    {
        public static class OLEDB
        {
            private static OleDbConnection dbCon;
            private static bool errorEncountered = false;
            public static void Init(string dbPath)
            {
                dbCon = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + dbPath + ";");
                try
                {
                    dbCon.Open();
                    dbCon.Close();
                    errorEncountered = false;
                }
                catch (Exception ex)
                {
                    //COULD NOT OPEN OLEDB
                    errorEncountered = true;
                }
            }
            public static void Close()
            {
                try
                {
                    dbCon.Close();
                }
                catch (Exception ex)
                {
                    //COULD NOT CLOSE OLEDATABASE
                }
            }
            public static DataTable Select(String cmd, OleDbParameter[] pc)
            {
                if (!errorEncountered)
                {
                    try
                    {
                        dbCon.Open();
                        OleDbCommand dbcmd = new OleDbCommand(cmd, dbCon);
                        if (pc != null)
                            foreach (OleDbParameter p in pc)
                                dbcmd.Parameters.Add(p);
                        OleDbDataReader dr = dbcmd.ExecuteReader();
                        DataTable dt = new DataTable();
                        dt.Load(dr);
                        dbCon.Close();
                        return dt;
                    }
                    catch (Exception ex)
                    {
                        //COULD NOT SELECT FROM OLEDB
                        errorEncountered = true;
                    }

                }
                return null;
            }
            public static int Update(String cmd, OleDbParameter[] pc)
            {
                if (!errorEncountered)
                {
                    try
                    {
                        OleDbCommand dbcmd = new OleDbCommand(cmd, dbCon);
                        if (pc != null)
                            foreach (OleDbParameter p in pc)
                                dbcmd.Parameters.Add(p);
                        return dbcmd.ExecuteNonQuery();
                    }
                    catch (Exception)
                    {
                        errorEncountered = true;
                    }
                }
                return 0;
            }
            public static int Insert(String cmd, OleDbParameter[] pc)
            {
                if (!errorEncountered)
                {
                    try
                    {
                        dbCon.Open();
                        OleDbCommand dbcmd = new OleDbCommand(cmd, dbCon);
                        if (pc != null)
                            foreach (OleDbParameter p in pc)
                                dbcmd.Parameters.Add(p);
                        int rtn = dbcmd.ExecuteNonQuery();
                        dbCon.Close();
                        return rtn;
                    }
                    catch (Exception ex)
                    {
                        errorEncountered = true;
                    }
                }
                return 0;
            }
            public static int Delete(String cmd, OleDbParameter[] pc)
            {
                if (!errorEncountered)
                {
                    try
                    {
                        OleDbCommand dbcmd = new OleDbCommand(cmd, dbCon);
                        if (pc != null)
                            foreach (OleDbParameter p in pc)
                                dbcmd.Parameters.Add(p);
                        return dbcmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        errorEncountered = true;
                    }
                }
                return 0;
            }
        }
        public static class SQL
        {
        }
    }
    public static class ErrorMessages
    {
        public static bool debug = false;
        public enum Level { msg, warning, critical, decision };
        public static bool ThrowError(String msg, String title, Level level, Func<int> fBefore, Exception ex)
        {
            bool val = false;
            if (fBefore != null)
                fBefore();
            switch (level)
            {
                case Level.msg:
                    MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case Level.warning:
                    MessageBox.Show("Warning: " + msg, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case Level.critical:
                    MessageBox.Show("Critical Error: " + msg, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                case Level.decision:
                    val = (MessageBox.Show(msg, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes);
                    break;
            }
            return val;
        }
    }
    public static class Logging
    {
        private static string loadedFile;
        private static bool errorEncountered = false;
        public static bool loggingEnabled = true;
        public static void Init(string filename)
        {
            try
            {
                loadedFile = filename;
                if (!File.Exists(filename) && loggingEnabled)
                    File.Create(filename).Close();
                errorEncountered = false;
            }
            catch (Exception ex)
            {
                //COULD NOT CREATE LOG FILE
                errorEncountered = true;
            }
        }
        public static void Write(string line)
        {
            if (loggingEnabled && !errorEncountered)
            {
                try
                {
                    using (StreamWriter outfile = File.AppendText(loadedFile))
                        outfile.WriteLine(DateTime.Now.ToString() + ": " + line);
                }
                catch (Exception ex)
                {
                    //COULD NOT WRITE TO LOG FILE
                    errorEncountered = true;
                }
            }
        }
    }
    public static class TCP
    {
        private struct SOCKET
        {
            public string name;
            public Socket client, server;
            public IPAddress ipAddress;
            public Action<string> onAccept, onConnect, onSend, onDisconnect;
            public Action<string, byte[], int> onReceive;
            public byte[] bytes;
            public bool listening, listener;
            public int bytesRec, port;
            public void accept()
            {
                onAccept(name);
            }
            public void connect()
            {
                onConnect(name);
            }
            public void disconnect()
            {
                onDisconnect(name);
            }
            public void receive()
            {
                onReceive(name, bytes, bytesRec);
            }
            public void send()
            {
                onSend(name);
            }
        }
        private static Dictionary<string, SOCKET> dServerSockets =  new Dictionary<string,SOCKET>();
        public static bool isConnected(String name)
        {
            try
            {
                SOCKET s = dServerSockets[name];
                return s.client.Connected;
            }
            catch { }
            return false;
        }
        public static bool isListening(String name)
        {
            SOCKET s = dServerSockets[name];
            return s.listening;
        }
        public static void addServer(String name, int port, Action<string> onAccept, Action<string> onDisconnect, Action<string, byte[], int> onReceive, int byteSize)
        {
            SOCKET s = new SOCKET();
            s.onAccept = onAccept;
            s.onDisconnect = onDisconnect;
            s.onReceive = onReceive;
            s.bytes = new byte[byteSize];
            s.name = name;
            s.ipAddress = IPAddress.Any;
            s.port = port;
            s.listening = false;
            s.listener = true;
            s.server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.server.Bind(new IPEndPoint(IPAddress.Any, port));
            dServerSockets.Add(name, s);
        }
        public static void addClient(String name, IPAddress ipAddress, int port, Action<string> onConnect, Action<string> onDisconnect, Action<string, byte[], int> onReceive, int byteSize)
        {
            SOCKET s = new SOCKET();
            s.onConnect = onConnect;
            s.onDisconnect = onDisconnect;
            s.onReceive = onReceive;
            s.bytes = new byte[byteSize];
            s.name = name;
            s.ipAddress = ipAddress;
            s.port = port;
            s.listening = false;
            s.listener = false;
            dServerSockets.Add(name, s);

        }
        public static void startListening(String name)
        {
            SOCKET s = dServerSockets[name];
            if (!s.listening)
            {
                s.listening = true;
                dServerSockets[name] = s;
                s.server.Listen(1);
                s.server.BeginAccept(new AsyncCallback(cbAccept), s);
                dServerSockets[name] = s;
            }
        }
        public static void startConnecting(String name)
        {
            SOCKET s = dServerSockets[name];
            s.client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            dServerSockets[name] = s;
            s.client.BeginConnect(new IPEndPoint(s.ipAddress, s.port), new AsyncCallback(cbConnect), s);
        }
        public static void startSend(String name, Action<string> onSend, byte[] bytes)
        {
            SOCKET s = dServerSockets[name];
            s.client.NoDelay = true;
            s.client.DontFragment = true;
            s.onSend = onSend;
            s.client.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, new AsyncCallback(cbSend), s);
            dServerSockets[name] = s;
        }
        public static void disconnect(String name)
        {
            SOCKET s = dServerSockets[name];
            if (isConnected(name))
            {
                s.client.Shutdown(SocketShutdown.Both);
                s.client.Disconnect(true);
                new Thread(s.disconnect).Start();
            }
        }
        private static void cbSend(IAsyncResult ar)
        {
            SOCKET s = (SOCKET)ar.AsyncState;
            s.client.EndSend(ar);
            new Thread(s.send).Start();
        }
        private static void cbAccept(IAsyncResult ar)
        {
            SOCKET s = (SOCKET)ar.AsyncState;
            s.client = s.server.EndAccept(ar);
            s.listening = false;
            new Thread(s.accept).Start();
            dServerSockets[s.name] = s;
            s.client.BeginReceive(s.bytes, 0, s.bytes.Length, SocketFlags.None, new AsyncCallback(cbReceive), s);
        }
        private static void cbConnect(IAsyncResult ar)
        {
            SOCKET s = (SOCKET)ar.AsyncState;
            s.client.EndConnect(ar);
            new Thread(s.connect).Start();
            s.client.BeginReceive(s.bytes, 0, s.bytes.Length, SocketFlags.None, new AsyncCallback(cbReceive), s);
        }
        private static void cbReceive(IAsyncResult ar)
        {
            SOCKET s = (SOCKET)ar.AsyncState;
            s.bytesRec = s.client.EndReceive(ar);
            if (s.bytesRec > 0)
            {
                s.client.BeginReceive(s.bytes, 0, s.bytes.Length, SocketFlags.None, new AsyncCallback(cbReceive), s);
                new Thread(s.receive).Start();
            }
            else
            {
                disconnect(s.name);
            }
        }
    }
}
