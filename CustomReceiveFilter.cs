using System;
using System.Text;
using SuperSocket.SocketBase.Protocol;
using GPSTrackerListeners.Util;

namespace GPSTrackerListeners.AtlantaNew
{
    public class CustomReceiveFilter : IReceiveFilter<StringRequestInfo>
    {


        public StringRequestInfo Filter(byte[] readBuffer, int offset, int length, bool toBeCopied, out int rest)
        {


            string source = Encoding.ASCII.GetString(readBuffer, offset, length);
            source = source.Trim();
            rest = 0;
            
            string str1Org = source;
            string rplQuote = str1Org.Replace(@"""", " ");
            string rplslash = rplQuote.Replace(@"/", " ");
            //string rplplus = rplslash.Replace(@"+", " ");//version issue(+ ommitted) 19-05-2020
            string rplundr = rplslash.Replace(@"_", " ");
            string rplcol = rplundr.Replace(@"'", " ");

            source = rplcol.Replace(@"\", " ");
            if (string.IsNullOrEmpty(source))
                return null;

            char[] chars = source.ToCharArray();
            if (chars[0] == '$')
            {
                string[] data1 = source.Split('$');
                if (char.IsDigit(chars[1]) && char.IsDigit(chars[2]))
                    return new StringRequestInfo("response", source, data1);
            }

            // Command sending
            if (source.Contains("$CMD"))
            {
                //$CMD|5930|$MSG,GETGPS<6906>&
                string[] sourceData = source.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

                return new StringRequestInfo(sourceData[1], source, sourceData);
            }

            //Command Sending Response
            if (!source.Contains("$GPR"))
            {   
                return new StringRequestInfo(null, source, null);
            }

            string protocol = General.GetProtocol(source);
            if (protocol != "AtlantaNew" || protocol==null)
            {
                return new StringRequestInfo("WrongProtocol", source, new string[] {protocol});
            }

            string[] data = source.Split(new string[] {"$GPRMC"}, StringSplitOptions.RemoveEmptyEntries);

            if (data.Length < 0)
            {

                return null;
            }

            if (data[0].Length < 18)
            {
                //General.WriteToLogFile(data[0], "exPacket.txt");
                return null;
            }
            else if(data[0].Length==19)  
                {

                return new StringRequestInfo(data[0].Substring(3, 15), source, data);
            }

            return new StringRequestInfo(data[0].Substring(4, 15), source, data);
        }

        public int LeftBufferSize { get; private set; }
        public IReceiveFilter<StringRequestInfo> NextReceiveFilter { get; private set; }
        public FilterState State { get; private set; }

        public void Reset()
        {

        }


    }
}
