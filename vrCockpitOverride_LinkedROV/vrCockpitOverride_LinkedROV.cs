using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace vrCockpitOverride
{
    public class vrCockpitOverride_LinkedROV : VideoRay.IOverride<VideoRay.ROV>, VideoRay.IOverride<VideoRay.UI.Joystick>, VideoRay.UI.IMessageDisplay
    {
        public enum State { PortMaster, PortSlave, StrbMaster, StrbSlave, Cancel };
        State _state = State.Cancel;
        public State state
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                if (state==State.Cancel) {
                    IsEngaged = false;
                }
                else {
                    IsEngaged = true;
                }
            }
        }

        //Networking sockets
        UdpClient input; //Thruster command
        IPEndPoint in_endpoint;

        //Sockets from Override into Copilot "output"
        IPEndPoint out_endpoint;
        Socket output;

        //Persistant configuration storage
        const string ConfigFilename = "vCockpit_LinkedROV_Override_Config.config";
        Cockpit_LinkedROV_Override_Config config;

        object dataLock = new object();

        public vrCockpitOverride_LinkedROV()
        {
            IsOn = true;
            IsEngaged = false;
            state = State.Cancel;

            DisplayInit();

            VideoRay.Util.ConfigurationFile<Cockpit_LinkedROV_Override_Config> cf = new VideoRay.Util.ConfigurationFile<Cockpit_LinkedROV_Override_Config>();
            if (cf.Load(ConfigFilename, null))
            {
                config = cf.Data;
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("Error Opening Cockpit_LinkedROV_Override_Config.config");
                config = new Cockpit_LinkedROV_Override_Config();
            }

        }

        #region Data Handlers
        /// <summary>
        /// Handle reception of a PVRMV packet from CoPilot
        /// </summary>
        /// <param name="ar"></param>
        private void Command_callback(IAsyncResult ar)
        {
            string[] parsedString;
            string receivedString = "";
            char[] parseDelimiters = { ',', '*' };

            UdpClient udpClient = (UdpClient)((UdpState)(ar.AsyncState)).u;
            IPEndPoint ipEndpt = (IPEndPoint)((UdpState)(ar.AsyncState)).e;
            Byte[] buf = udpClient.EndReceive(ar, ref ipEndpt);

            System.IO.MemoryStream stm = new System.IO.MemoryStream(buf, 0, buf.Length);
            System.IO.StreamReader r = new System.IO.StreamReader(stm);

            receivedString = r.ReadLine();

            //            debugForm.setReceivedString(receivedString);

            parsedString = receivedString.Split(parseDelimiters);

//            System.Diagnostics.Trace.WriteLine("CMD: " + receivedString);

            if (parsedString[0].Equals("$CANCEL")) {
                state = State.Cancel;
                display.Dispatcher.BeginInvoke(new Action(display.SetCancel));
            }
            else
            if (parsedString[0].Equals("$JOY"))
            {
                string dir = parsedString[1];
                slave_j1 = double.Parse(parsedString[2]); 
                slave_j2 = double.Parse(parsedString[3]);
                slave_j5 = double.Parse(parsedString[4]);
                
                display.Dispatcher.BeginInvoke(new Action(display.SetSlave));

                if (dir == "S")
                {
                    state = State.StrbSlave;
                    display.Dispatcher.BeginInvoke(new Action(display.SetStarboard));
                }
                else
                {
                    state = State.PortSlave;
                    display.Dispatcher.BeginInvoke(new Action(display.SetPort));
                }
                IsOn = true;
            }
            r.Close();

            UdpState udp = new UdpState();
            udp.u = input;
            udp.e = in_endpoint;
            input.BeginReceive(new AsyncCallback(Command_callback), udp);
        }
        #endregion

        #region IOverride Section

        #region Control Functions

        /// <summary>
        /// Control Override function
        /// </summary>
        /// <param name="_rov">ROV structure</param>
        /// <returns>true if overriden</returns>
        public bool Control(VideoRay.ROV _rov)
        {

            return true;
        }

        double slave_j1 = 0;
        double slave_j2 = 0;
        double slave_j5 = 0;

        /// <summary>
        /// Joystick override function
        /// This is called directly in the joystick aquisition loop
        /// This allows for both the pass through of joystick values and also the override of 
        /// the joystick data before Cockpit processes this data.
        /// </summary>
        /// <param name="_joystick"></param>
        /// <returns></returns>
        public bool Control(VideoRay.UI.Joystick _joystick)
        {

            if (state != State.Cancel)
            {
                if (state == State.PortSlave || state == State.StrbSlave)
                {
                    _joystick.axis[1] = slave_j1; 
                    _joystick.axis[0] = slave_j2;
                    _joystick.axis[5] = slave_j5;
                }
                else
                if (state == State.PortMaster || state == State.StrbMaster)
                {

                    string cmdStr = "$JOY,";

                    double surge = _joystick.axis[1];
                    double yaw = _joystick.axis[0];

                    double angle = Math.Atan2(surge, yaw);
                    double a = Math.Abs(1 * yaw);
                    double b = Math.Abs(1 * surge);
                    //map to ellipse
                    double map_yaw = a * Math.Cos(angle);
                    double map_surge = b * Math.Sin(angle);

                    double port = map_surge;
                    double startboard = map_surge;

                    port += map_yaw;
                    startboard -= map_yaw;

                    port = Math.Min(port, 1);
                    port = Math.Max(port, -1);
                    startboard = Math.Min(startboard, 1);
                    startboard = Math.Max(startboard, -1);

                    double depth = _joystick.axis[5];
                    double depth_mod = _joystick.axis[2];

                    double sbdepth = depth + depth_mod;
                    sbdepth = Math.Min(sbdepth, 1);
                    sbdepth = Math.Max(sbdepth, -1);

                    double portdepth = depth - depth_mod;
                    portdepth = Math.Min(portdepth, 1);
                    portdepth = Math.Max(portdepth, -1);


 //                  Console.WriteLine("C: " + _joystick.axis[1] + " " + _joystick.axis[0] + " " + port + " " + startboard);
//                    Console.WriteLine("C: " + depth + " " + depth_mod + " " + _joystick.axis[4] + " " + _joystick.axis[5]);
                    if (state == State.PortMaster)
                    {

                        _joystick.axis[1] = port;
                        _joystick.axis[0] = 0;
                        _joystick.axis[5] = portdepth;

                        cmdStr += "S," + startboard + ",0," + sbdepth;
                    }
                    else
                    if (state == State.StrbMaster)
                    {
                        _joystick.axis[1] = startboard;
                        _joystick.axis[0] = 0;
                        _joystick.axis[5] = sbdepth;
                        cmdStr += "P," + port + ",0," + portdepth;
                    }
                    //transmit data across UDP bridge
                    cmdStr += "\r\n";
                    byte[] outbuf = Encoding.ASCII.GetBytes(cmdStr);
                    output.SendTo(outbuf, out_endpoint);

                }
            }
            return true;
        }
        #endregion

        #region Other IOverride Functions
        public bool Start()
        {
            return SetupDataPipes();
        }

        /// <summary>
        /// Flag to indicate if ovverride is on or off
        /// </summary>
        public bool IsOn
        {
            get;
            set;
        }

        public bool IsEngaged
        {
            get;
            set;
        }


        /// <summary>
        /// This function is called by cockpit to REQUEST the override be canceled
        /// </summary>
        /// <returns>true if request was granted</returns>
        public bool RequestOff()
        {
            IsEngaged = false;
            return true;
        }

        /// <summary>
        /// Simple mechanism for reporting errors back to cockpit
        /// </summary>
        public string LastErrorMessage
        {
            set;
            get;
        }

        /// <summary>
        /// Close down the control object gracefully
        /// </summary>
        /// <returns></returns>
        public bool End()
        {
            VideoRay.Util.ConfigurationFile<Cockpit_LinkedROV_Override_Config> cf = new VideoRay.Util.ConfigurationFile<Cockpit_LinkedROV_Override_Config>();
            cf.SaveFile(ConfigFilename, config, null);
            return true;
        }

        #endregion

        #endregion

        #region IMessageDisplay Section
        OverrideDisplay display;

        void DisplayInit()
        {
            display = new OverrideDisplay();
            display.Port.MouseDown += new System.Windows.Input.MouseButtonEventHandler(Port_MouseDown);
            display.Starboard.MouseDown += new MouseButtonEventHandler(Starboard_MouseDown);
            display.RequestCancel.Click += new RoutedEventHandler(Cancel_Click);
        }

        public void ShowMessageDisplay(System.Windows.FrameworkElement parent)
        {
            display.Show();
        }

        public void HideMessageDisplay()
        {
            display.Hide();
        }



        #endregion

        private void Port_MouseDown(object sender, MouseButtonEventArgs e)
        {

            state = State.PortMaster;
            display.SetPort();
            display.SetMaster();
        }

        private void Starboard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            state = State.StrbMaster;
            display.SetStarboard();
            display.SetMaster();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            state = State.Cancel;
            display.SetCancel();
            string cmdStr = "$CANCEL\r\n";
            byte[] outbuf = Encoding.ASCII.GetBytes(cmdStr);
            output.SendTo(outbuf, out_endpoint);
        }


        #region Helper Functions
        /// <summary>
        /// Sets up the udp pipes for communication with SeeByte CoPilot
        /// </summary>
        private bool SetupDataPipes()
        {
            try
            {
                //Setup output pipes from the override plugin --> CoPilot
                output = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                out_endpoint= new IPEndPoint(config.PartnerIPAddress, config.UDP_port_out);

                //Setup input pipe from CoPilot-->Override plugin
                input = new UdpClient(config.UDP_port_in);
                in_endpoint = new IPEndPoint(config.LocalIPAddress, config.UDP_port_in);
                UdpState udp = new UdpState();
                udp.u = input;
                udp.e = in_endpoint;
                input.BeginReceive(new AsyncCallback(Command_callback), udp);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERR: " + e.Message);
                return false;
            }
            return true;
        }

        private class UdpState
        {
            public IPEndPoint e;
            public UdpClient u;

        }


        /// <summary>
        /// Convert a bool to a string of the form "0" or "1"
        /// </summary>
        /// <param name="b">value to convert</param>
        /// <returns>"0" = false, "1" = true</returns>
        private string bool2str(bool b)
        {
            return (b ? "1" : "0");
        }

        #endregion
    }
}
