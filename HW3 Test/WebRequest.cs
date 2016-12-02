using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;

namespace CS422
{
    public class WebRequest
    {
        TcpClient client;
        private NetworkStream netStream;
        public Stream bodyStream;
        public List<Tuple<string, string>> headers;
        public string httpVersion;
        public string httpMethod;
        public string URI;
        public int bodyLength;


        public WebRequest(TcpClient myClient,  Stream originBodyStream, List<Tuple<string, string>> headerList, string httpVer, string httpMeth, string Destination)
        {
            client = myClient;
            netStream = client.GetStream();
            bodyStream = originBodyStream;
            headers = new List<Tuple<string, string>>(headerList);
            bodyLength = -1;
            foreach (Tuple<string, string> header in headers)
            {
                if (header.Item1 == "Content-Length")
                {
                    bodyLength = Int32.Parse(header.Item2);
                }
            }
            
            httpVersion = httpVer;
            httpMethod = httpMeth;
            URI = Destination;
        }

       
        public void WriteNotFoundResponse(string pageHTML)
        {
            string responseString = "HTTP/1.1 404 Not Found\nContent-Type: text/html\nContent-Length: " + pageHTML.Length + "\r\n\r\n" + pageHTML;
            byte[] responseBytes = Encoding.ASCII.GetBytes(responseString);
            try
            {
                netStream.Write(responseBytes, 0, responseBytes.Length); //write the status line 
            }
            catch
            {
                //do nothing, just exit the method after disposing
            }
            
            netStream.Dispose();
        }

        public bool WriteHTMLResponse(string htmlString)
        {
            string responseString = "HTTP/1.1 200 OK\r\nContent-Type: text/html\r\nContent-Length: " + htmlString.Length + "\r\n\r\n" + htmlString;
            byte[] responseBytes = Encoding.ASCII.GetBytes(responseString);
            try
            {
                netStream.Write(responseBytes, 0, responseBytes.Length); //write the status line 
            }
            catch
            {

            }
            netStream.Dispose();
            return true;
        }

        private Tuple<string,string> getRangeHeader()
        {
            

            foreach(Tuple<string,string> header in headers)
            {
                if (header.Item1 == "Range")
                    return header;
            }

            return null;
            
        }
        public bool WriteHTMLResponse(Stream htmlStream, string contentType)
        {
            if (htmlStream == null)
                return false;

            Tuple<string, string> header = getRangeHeader();

            //if (contentType == "video/mp4" && header != null)
            //{
            //    while (true) //serves as a loop to break out of...
            //    {
            //        string[] rangeInfo = header.Item2.Split('='); //right hand side should be byte ammounts and left should be measurement
            //        if (rangeInfo.Length < 2) { break; } //nevermind range

            //        string rangeType = rangeInfo[0];
            //        string rangeByteCounts = rangeInfo[1];
            //        string[] rangeByteLists = rangeByteCounts.Split('-');

            //        int beginRange = -1;
            //        int endRange = -1;
            //        string rangeResponse = "HTTP/1.1 206 Partial Content\r\nContent-Type: " + contentType + "\r\nContent-Length: " + htmlStream.Length; //not finished yet
            //        if (rangeByteLists.Length <= 1)
            //            rangeResponse += "Accept-Ranges: " + rangeType + "\r\n\r\n";
            //        else if (rangeByteLists.Length == 2) //always ends with ""
            //        {
            //            try
            //            {
            //                Int32.TryParse(rangeByteLists[0], out beginRange);
            //            }

            //            catch
            //            {
            //                break;
            //            }
            //            rangeResponse += "Accept-Ranges: " + rangeType + "\r\nContent-Range: " + rangeType + " " + beginRange.ToString() + "-" + htmlStream.Length + "/" + htmlStream.Length + "\r\n\r\n";

            //        }
            //        else if (rangeByteLists.Length == 3) //awlays ends with ""
            //        {
            //            try
            //            {
            //                Int32.TryParse(rangeByteLists[0], out beginRange);
            //                Int32.TryParse(rangeByteLists[1], out endRange);
            //            }
            //            catch
            //            {
            //                break;
            //            }
            //            rangeResponse += "Accept-Ranges: " + rangeType + "\r\nContent-Range: " + rangeType + " " + beginRange.ToString() + "-" + endRange.ToString() + "/" + htmlStream.Length + "\r\n\r\n";
            //        }
            //        else break;

            //        netStream.Write(Encoding.ASCII.GetBytes(rangeResponse), 0, Encoding.ASCII.GetBytes(rangeResponse).Length); //write the beginning of the range response
            //        //end of foramtting, now read what we have to....
            //        long counter = -1;
            //        try
            //        {
            //            if (beginRange != -1)//user specified beginning range
            //                htmlStream.Seek(beginRange, SeekOrigin.Begin);


            //            if (endRange != -1) //check to see if an end range was given
            //            {
            //                counter = endRange - beginRange;
            //            }
            //            if (counter <= 0) //make sure endRange is after begin range
            //                counter = htmlStream.Length + 1024; //make the counter larger than the stream.
            //        }
            //        catch
            //        {
            //            break;
            //        }

            //        byte[] buf = new byte[1024]; //create a buffer for reading from a file

            //        try
            //        {
            //            while (htmlStream.Read(buf, 0, 1024) > 0 && counter >= 0) //read in the file and add it to HTML
            //            {
            //                counter -= buf.Length;

            //                if (counter < 0)
            //                {
            //                    netStream.Write(buf, 0, buf.Length + (int)counter); //counter would be negative
            //                }
            //                else
            //                {
            //                    netStream.Write(buf, 0, buf.Length); //if counter is greater than length, just write everything read in.
            //                }
            //            }
            //        }
            //        catch
            //        {
            //            netStream.Close();
            //            htmlStream.Close();
            //            return false; //failed so return false
            //        }


            //        netStream.Close();
            //        htmlStream.Close();
            //        return true;

            //    }

                    
            //} //else, do normal response

            string responseString = "HTTP/1.1 200 OK\r\nContent-Type: "+ contentType + "\r\nContent-Length: " + htmlStream.Length + "\r\n\r\n";

            byte[] responseBytes = Encoding.ASCII.GetBytes(responseString);
            netStream.Write(responseBytes, 0, responseBytes.Length); //write the beginning response to the client.


            byte[] buffer = new byte[1024]; //create a buffer for reading from a file

            try
            {
                while (htmlStream.Read(buffer, 0, 1024) > 0) //read in the file and add it to HTML
                {
                    netStream.Write(buffer, 0, buffer.Length);
                }
            }
            catch
            {
                htmlStream.Close();
            }

            htmlStream.Close();
            netStream.Close();
            return true;
        }
    }
}
