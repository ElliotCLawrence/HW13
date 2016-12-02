//Elliot Lawrence 
//CS 422
//HW 7
//10/15/2016

using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text;


namespace CS422
{
    class WebServer
    {
        static ThreadPoolRouter threadPool;
        private static List<WebService> webServices = new List<WebService>();
        static Thread listenerThreadWorker;
        static TcpListener newListener;

        public static bool Start(int portNum, int threadCount)
        {
            AddService(new DemoService());
            threadPool = new ThreadPoolRouter(threadCount, portNum);
            threadPool.startWork();

            listenerThreadWorker = new Thread(new ParameterizedThreadStart(listenerThread));
            listenerThreadWorker.Start(portNum);

            return true;
        }

        public static void listenerThread(object port) //listener thread constantly executes this
        {
            try
            {
                newListener = new TcpListener(IPAddress.Any, (int)port); //open a tcp Listener
                newListener.Start();
                TcpClient client;
                while (true)
                {
                    client = newListener.AcceptTcpClient(); //have listener keep trying to get a new client

                    if (client == null)
                        break;

                    threadPool.addClient(client); //if not null, send the client to the threadpool
                }
            }
            catch
            {
                Console.WriteLine("Listener ending");
            }
        }

        public static void doThreadWork() //must cast to object in order to use parameterized thread start
        {
            TcpClient client;
            WebRequest request;

            while (true)
            {
                client = threadPool.takeClient(); //constantly try to get a new client

                if (client == null) //if null client break this thread
                    break;

                request = BuildRequest(client); //read from client and build a request

                if (request == null) //if not valid, find new client
                {
                    continue;
                }

                bool found = false;
                foreach (WebService services in webServices) //try to find a valid service request
                {
                    if ((request.URI.Split('/'))[1] == (services.ServiceURI.Split('/'))[1]) //*make sure this isn't out of range
                                                                                            //grabs first word after request. if request was
                                                                                            //GET /files /.... this would return 'files'
                    {
                        services.Handler(request); //call the handler for this request
                        found = true;
                        break;
                    }
                }

                if (!found) //if you don't find it, send 404 to user
                    request.WriteNotFoundResponse("404 Page Not Found");
            }
        }


        private static WebRequest BuildRequest(TcpClient client)
        {
            NetworkStream clientStream = client.GetStream();
            int ammountRead = 0;
            clientStream.ReadTimeout = (int)new TimeSpan(0, 0, 2).TotalMilliseconds;
            int startingSeconds = DateTime.Now.Second;
            int startingMinute = DateTime.Now.Minute;
            string fullRequest = "";
            string destination = "/";

            byte[] streamBuff = new byte[1024]; //create a buffer for reading
            int x;
            try
            {
                x = clientStream.Read(streamBuff, 0, 1024);
            }
            catch //if read times out
            {
                clientStream.Close();
                client.Close();
                return null;
            }
            int y = 0;
            int i = 0; //index of streamBuff
            ammountRead += x;

            string[] validReq = new string[2];
            validReq[0] = "GET / HTTP/1.1\r\n";
            validReq[1] = "PUT / HTTP/1.1\r\n";
            int maxValids = validReq.Length;

            int currentString = 0;

            while (x > 0 && y < validReq[currentString].Length) //while the ammount read is greater than 0 bytes
            {
                #region Timeout Logic

                if (ammountRead > 2048)
                {
                    clientStream.Close();
                    client.Close();
                    return null;
                }
                if (DateTime.Now.Minute != startingMinute)//if it's a different minute, add 60 seconds when you check change in time
                {
                    if (DateTime.Now.Second + 60 - startingSeconds > 10) //if 10 seconds has past since started reading.
                    {
                        clientStream.Close();
                        client.Close();
                        return null;
                    }
                }
                else if (DateTime.Now.Second - startingSeconds > 10) //if 10 seconds has past since started reading.
                {
                    clientStream.Close();
                    client.Close();
                    return null;
                }

                #endregion

                i = 0;
                fullRequest += Encoding.Default.GetString(streamBuff);

                while (i < x && y < validReq[currentString].Length)
                {
                    #region Parsing Request                    
                    if (y < 5 || y > 5)
                    {//first part 'GET /'
                        if (Convert.ToChar(streamBuff[i]) != validReq[currentString][y])
                        { //invalid request

                            i = 0; //start over with different request
                            y = 0;
                            currentString++;

                            if (currentString >= maxValids) //if there are no more strings to check against, fail.
                            {
                                client.Close(); //close stream and return false
                                return null;
                            }
                        }
                        //else
                        y++; //valid
                    }
                    else
                    { //the request is giving the requested web page (this comes right after 'GET /')
                        if (Convert.ToChar(streamBuff[i]) == ' ')
                            y++;
                        else //add this byte to the clientRequest
                            destination += Convert.ToChar(streamBuff[i]);
                    }
                    i++; //increment i
                    #endregion
                }

                //If we don't have enough characters to fully match a valid request yet, we need to read more bytes to verify it's a valid request

                if (y < validReq[currentString].Length) //only read if you need to.
                {
                    try
                    {
                        x = clientStream.Read(streamBuff, 0, 1024);//read next bytes
                    }
                    catch //if read times out
                    {
                        clientStream.Close();
                        client.Close();
                        return null;
                    }
                    ammountRead += x;
                }

                else //otherwise, break the loop
                    break;
            }

            #region Header Logic
            //read in the headers
            while (true)
            {
                if (ammountRead > 102400)
                {
                    clientStream.Close();
                    client.Close();
                    return null;
                }

                if (fullRequest.Contains("\r\n\r\n"))
                {
                    break;
                }

                else
                {
                    //read more stuff
                    try
                    {
                        x = clientStream.Read(streamBuff, 0, 1024); //read in more
                    }
                    catch
                    {
                        clientStream.Close();
                        client.Close();
                        return null;
                    }

                    ammountRead += x;
                    fullRequest += Encoding.Default.GetString(streamBuff);

                    if (DateTime.Now.Minute != startingMinute) //if it's a different minute, add 60 seconds when you check change in time
                    {
                        if (DateTime.Now.Second + 60 - startingSeconds > 10) //if 10 seconds has past since started reading.
                        {
                            clientStream.Close();
                            client.Close();
                            return null;
                        }
                    }
                    else if (DateTime.Now.Second - startingSeconds > 10) //if 10 seconds has past since started reading.
                    {
                        clientStream.Close();
                        client.Close();
                        return null;
                    }
                }

                if (x <= 0)
                    return null; //never had headers, nor a second \r\n. invalid request!
            }
            #endregion

            #region ParseHeaders
            string onlyHeaders;
            onlyHeaders = fullRequest.Substring(validReq[currentString].Length - 2); //before the \r\n
            string endHeaders = "\r\n\r\n";
            int endHeadersCount = 0;

            while (endHeadersCount < onlyHeaders.Length)
            {
                if (onlyHeaders.Substring(endHeadersCount, 4) == endHeaders)
                    break;
                endHeadersCount++;
            }

            List<Tuple<string, string>> headerList = new List<Tuple<string, string>>();

            string headers = onlyHeaders.Substring(0, endHeadersCount);
            string[] splitters = new string[1];
            splitters[0] = "\r\n";

            string[] headerArray = headers.Split(splitters, StringSplitOptions.RemoveEmptyEntries);

            string[] headerSplitter;
            foreach (string headerCombo in headerArray)
            {
                headerCombo.Trim();
                headerSplitter = headerCombo.Split(':');

                if (headerSplitter.Length == 2)
                {
                    headerList.Add(new Tuple<string, string>(headerSplitter[0], headerSplitter[1]));
                }
            }

            #endregion

            #region populateURI
            //populate from fullRequest string the URI, Method, version, etc future HW
            MemoryStream streamOne = new MemoryStream();
            WebRequest request;
            int z = endHeadersCount + 4; //right after the last \r\n\r\n
            string requestType = "";

            if (currentString == 0)
                requestType = "GET";
            else if (currentString == 1)
                requestType = "PUT";


            if (z < fullRequest.Length)
            {
                streamOne.Write(Encoding.ASCII.GetBytes(fullRequest), z, fullRequest.Length - z);
                ConcatStream jointStream = new ConcatStream(streamOne, client.GetStream());
                request = new WebRequest(client, jointStream, headerList, "1.1", requestType, destination); //change "GET" to variable
            }

            else
            {
                request = new WebRequest(client, client.GetStream(), headerList, "1.1", requestType, destination); //change "GET" to variable
            }

            #endregion

            return request;
        }

        public static void AddService(WebService service)
        {
            webServices.Add(service);
        }

        public static void Stop()
        {
            threadPool.Dispose();

            newListener.Stop();
            listenerThreadWorker.Join();

            return;
        }
    }
}


