using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;


namespace vrCockpitOverride
{
        [Serializable()]
        [XmlRoot("vrCockpit_LinkedROV_Override_Config")]
        public class Cockpit_LinkedROV_Override_Config
        {
            public string PartnerIPAddressString;
            public string LocalIPAddressString;

            //UDP ports for Override to CoPilot comms
            public int UDP_port_in;
            public int UDP_port_out;


            public Cockpit_LinkedROV_Override_Config()
            {
                PartnerIPAddressString = System.Net.IPAddress.Loopback.ToString();
                LocalIPAddressString = System.Net.IPAddress.Loopback.ToString();
                UDP_port_in = 11000;
                UDP_port_out = 11001;
            }

            /// <summary>
            /// Return an IPAddress class for the CopilotIPAddressString member.  
            /// This saves the hassel of having to serialize an IPAddress.
            /// </summary>
            [XmlIgnore]
            public System.Net.IPAddress PartnerIPAddress
            {
                get
                {
                    return System.Net.IPAddress.Parse(PartnerIPAddressString);
                }
            }

            /// Return an IPAddress class for the CopilotIPAddressString member.  
            /// This saves the hassel of having to serialize an IPAddress.
            /// </summary>
            [XmlIgnore]
            public System.Net.IPAddress LocalIPAddress
            {
                get
                {
                    return System.Net.IPAddress.Parse(LocalIPAddressString);
                }
            }
        
        }
}
