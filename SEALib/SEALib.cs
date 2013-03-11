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
    public static class FileBackup
    {
        public static void copyDirectory(string directory)
        {
            //COPYING FILES & FOLDERS
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
        public static void Write(string text, bool newline)
        {
            if (loggingEnabled && !errorEncountered)
            {
                try
                {
                    using (StreamWriter outfile = File.AppendText(loadedFile))
                    {
                        if (newline)
                            outfile.WriteLine(DateTime.Now.ToString() + ": " + text);
                        else
                            outfile.Write(text);
                    }
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
        public class SOCKET
        {
            private Socket client, server;
            private IPAddress ipAddress;
            private Action onAccept, onConnect, onSend, onDisconnect, onTimeout, onHeartbeatTimeout;
            private Action<byte[], int> onReceive;
            private byte[] bytes;
            public bool isListening = false, isConnecting = false, heartbeatEnabled = false;
            public int bytesRec = 0, port = -1, heartbeatTimeout = -1, bufferedSends = 0;
            
            private void taccept()
            {
                if (onAccept != null)
                    onAccept();
            }
            private void tconnect()
            {
                if (onConnect != null)
                    onConnect();
            }
            private void tdisconnect()
            {
                if (onDisconnect != null)
                    onDisconnect();
            }
            private void treceive()
            {
                if (onReceive != null)
                    onReceive(bytes, bytesRec);
            }
            private void tsend()
            {
                if (onSend != null)
                    onSend();
            }
            private void ttimeout()
            {
                if (onTimeout != null)
                    onTimeout();
            }
            private void theartbeattimeout()
            {
                if (onHeartbeatTimeout != null)
                    onHeartbeatTimeout();
            }

            private System.Timers.Timer tmrDisconnect = new System.Timers.Timer();
            private System.Timers.Timer tmrHeartbeat = new System.Timers.Timer();

            public bool isConnected
            {
                get
                {
                    try { return client.Connected; }
                    catch { return false; }
                }
            }
            public void initServer(int port, Action onAccept, Action onDisconnect, Action<byte[], int> onReceive, int byteSize)
            {
                try
                {
                    this.onAccept = onAccept;
                    this.onDisconnect = onDisconnect;
                    this.onReceive = onReceive;
                    bytes = new byte[byteSize];
                    ipAddress = IPAddress.Any;
                    this.port = port;
                    tmrDisconnect.Elapsed += delegate { timeoutServer(); };
                    tmrDisconnect.AutoReset = false;
                }
                catch { }
            }
            public void initClient(String ipAddress, int port, Action onConnect, Action onDisconnect, Action<byte[], int> onReceive, int byteSize)
            {
                try
                {
                    this.onConnect = onConnect;
                    this.onDisconnect = onDisconnect;
                    this.onReceive = onReceive;
                    bytes = new byte[byteSize];
                    this.ipAddress = IPAddress.Parse(ipAddress);
                    this.port = port;
                    tmrDisconnect.Elapsed += delegate { timeoutClient(); };
                    tmrDisconnect.AutoReset = false;
                }
                catch { }
            }
            public void startListening(int timeout, Action onTimeout)
            {
                try
                {
                    if (!isListening)
                    {
                        this.onTimeout = onTimeout;
                        isListening = true;
                        server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        server.LingerState.Enabled = false;
                        server.Bind(new IPEndPoint(IPAddress.Any, this.port));
                        server.Listen(1);
                        server.BeginAccept(new AsyncCallback(cbAccept), null);
                        if (timeout > 0)
                        {
                            tmrDisconnect.Stop();
                            tmrDisconnect.Interval = timeout;
                            tmrDisconnect.Start();
                        }
                    }
                }
                catch { }
            }
            public void startConnecting(int timeout, Action onTimeout)
            {
                try
                {
                    if (!this.isConnecting)
                    {
                        this.onTimeout = onTimeout;
                        isConnecting = true;
                        bufferedSends = 0;
                        client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        client.BeginConnect(new IPEndPoint(ipAddress, port), new AsyncCallback(cbConnect), null);
                        if (timeout > 0)
                        {
                            tmrDisconnect.Stop();
                            tmrDisconnect.Interval = timeout;
                            tmrDisconnect.Start();
                        }
                    }
                }
                catch { }
            }
            public void startSend(Action onSend, byte[] bytes)
            {
                try
                {
                    if (isConnected)
                    {
                        bufferedSends++;
                        client.NoDelay = true;
                        client.DontFragment = true;
                        this.onSend = onSend;
                        client.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, new AsyncCallback(cbSend), null);
                    }
                }
                catch { }
            }
            public void enableHeartbeat(int heartbeatTimeout, Action onHeartbeatTimeout)
            {
                try
                {
                    heartbeatEnabled = true;
                    this.onHeartbeatTimeout = onHeartbeatTimeout;
                    this.heartbeatTimeout = heartbeatTimeout;
                    tmrHeartbeat.Interval = heartbeatTimeout;
                    tmrHeartbeat.Elapsed += delegate { checkHeartbeat(); };
                    if (isConnected)
                        tmrHeartbeat.Start();
                }
                catch { }
            }
            public void disableHeartbeat()
            {
                try
                {
                    heartbeatEnabled = false;
                    tmrHeartbeat.Stop();
                }
                catch { }
            }
            public void disconnect()
            {
                if (heartbeatEnabled)
                    tmrHeartbeat.Stop();
                isListening = false;
                isConnecting = false;
                try
                {
                    if (client != null && client.Connected)
                    {
                        new Thread(tdisconnect).Start();
                        client.Shutdown(SocketShutdown.Both);
                        client.Close(0);
                    }

                }
                catch { }
                try
                {
                    if (server != null)
                        server.Close(0);
                }
                catch { }
            }

            //CALLBACKS
            private void cbAccept(IAsyncResult ar)
            {
                try
                {
                    isListening = false;
                    tmrDisconnect.Stop();
                    client = server.EndAccept(ar);
                    server.Close(0);
                    new Thread(taccept).Start();
                    client.BeginReceive(bytes, 0, bytes.Length, SocketFlags.None, new AsyncCallback(cbReceive), null);
                }
                catch { }
            }
            private void cbConnect(IAsyncResult ar)
            {
                try
                {
                    client.EndConnect(ar);
                    isConnecting = false;
                    tmrDisconnect.Stop();
                    new Thread(tconnect).Start();
                    client.BeginReceive(bytes, 0, bytes.Length, SocketFlags.None, new AsyncCallback(cbReceive), null);
                }
                catch { }
            }
            private void cbSend(IAsyncResult ar)
            {
                try
                {
                    client.EndSend(ar);
                    bufferedSends--;
                    new Thread(tsend).Start();
                }
                catch { disconnect(); }
            }
            private void cbReceive(IAsyncResult ar)
            {
                try
                {
                    if (heartbeatEnabled)
                        tmrHeartbeat.Stop();
                    bytesRec = client.EndReceive(ar);
                    if (bytesRec > 0)
                    {
                        if (isConnected)
                            client.BeginReceive(bytes, 0, bytes.Length, SocketFlags.None, new AsyncCallback(cbReceive), null);
                        new Thread(treceive).Start();
                        if (heartbeatEnabled)
                            tmrHeartbeat.Start();
                    }
                    else
                    {
                        disconnect();
                    }
                }
                catch { disconnect(); }
            }
            private void checkHeartbeat()
            {
                if (isConnected)
                {
                    disconnect();
                    new Thread(theartbeattimeout).Start();
                }
            }
            private void timeoutClient()
            {
                try
                {
                    client.Close(0);
                }
                catch { }
                disconnect();
                new Thread(ttimeout).Start();
            }
            private void timeoutServer()
            {
                try
                {
                    server.Close(0);
                }
                catch { }
                disconnect();
                new Thread(ttimeout).Start();
            }
        }
    }
}
