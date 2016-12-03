﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.IO;


namespace CS422
{
    class FilesWebService : WebService
    {
        private readonly FileSys422 r_sys;
        private string uriPath;
        private bool m_allowUploads;

        public FilesWebService(FileSys422 fs)
        {
            r_sys = fs;
            uriPath = null;
            m_allowUploads = true;
        }

        public override string ServiceURI
        {
            get
            {
                return "/files/";
            }
        }

        public override void Handler(WebRequest req)
        {
            if (req.URI.Length < this.ServiceURI.Length)
                req.URI = this.ServiceURI;

            if (!req.URI.StartsWith(this.ServiceURI))
            {
                throw new InvalidOperationException();
            }
            
            

            uriPath = req.URI;

            string[] pieces = req.URI.Substring(ServiceURI.Length).Split('/');  //split up the path by '/' tokens

            if (pieces.Length == 1 && pieces[0] == "") //we passed in only the root
                RespondWithList(r_sys.GetRoot(), req);


            for (int x = 0; x < pieces.Length; x++)
            {
                pieces[x] = decode(pieces[x]);

            }


            Dir422 dir = r_sys.GetRoot(); //grab the root of the filesystem
            for (int i = 0; i < pieces.Length - 1; i++) //go through the parts of the path
            {

                dir = dir.getDir(pieces[i]);
                if (dir == null) //if you encounter a directory that doesn't exist, tell the user that the target they requested is not found and return
                {
                    req.WriteNotFoundResponse("File not found.\n");
                    return;
                }
            }

            //we now have the directory of one above the file / directory


            if (req.httpMethod == "PUT") //if the method is put.
            {
                int x = 0;
                byte[] bodyBytes = new byte[4096];
                string bodyContent = "";

                x = req.bodyStream.Read(bodyBytes, 0, 4096);

                while (x > 0)
                {
                    bodyContent += Encoding.ASCII.GetString(bodyBytes);
                    x = req.bodyStream.Read(bodyBytes, 0, 4096);
                }

                if (bodyContent.Length == 0)
                {
                    req.WriteNotFoundResponse("No data in the specified file");
                    return;
                }

                File422 fileToCreate = dir.GetFile(pieces[pieces.Length - 1]); //grab the last file of the path
                if (fileToCreate == null)
                {
                    File422 newFile = dir.CreateFile(pieces[pieces.Length - 1]);
                    FileStream fs = (FileStream) newFile.OpenReadWrite();

                    fs.Write(Encoding.ASCII.GetBytes(bodyContent), 0, Encoding.ASCII.GetBytes(bodyContent).Length);
                    req.WriteHTMLResponse("200 OK");
                }

                else
                {
                    req.WriteHTMLResponse("File already exists");
                }

                
                return;
            }




            //one piece to process left
            //check if dir is in the last piece we have 
            File422 file = dir.GetFile(pieces[pieces.Length - 1]); //grab the last file of the path
            if (file != null)
            {
                RespondWithFile(file, req);
            }
            else
            {
                dir = dir.getDir(pieces[pieces.Length - 1]); //if it wasn't a file, grab it as a dir
                if (dir != null)
                {
                    RespondWithList(dir, req);
                }
                else //if it's null, tell the user it was not found
                {
                    req.WriteNotFoundResponse("Not found\n");
                }
            }
        }

        String BuildDirHTML(Dir422 directory)
        {

            var html = new StringBuilder("<html>");

            if (m_allowUploads) {
                html.AppendLine(
                @"<script> 
                function selectedFileChanged(fileInput, urlPrefix) {     
                    document.getElementById('uploadHdr').innerText = 'Uploading ' + fileInput.files[0].name + '...'; 
                    
                    //Need XMLHttpRequest to do the upload     
                    if (!window.XMLHttpRequest)     {         
                        alert('Your browser does not support XMLHttpRequest. Please update your browser.');         
                        return;     
                    } 
 
                    //Hide the file selection controls while we upload     
                    var uploadControl = document.getElementById('uploader');     
                    if (uploadControl)     
                    {         
                        uploadControl.style.visibility = 'hidden';     
                    } 
 
                    // Build a URL for the request     
                    if (urlPrefix.lastIndexOf('/') != urlPrefix.length - 1)     
                    {         
                        urlPrefix += '/';     
                    } 

                    var uploadURL = urlPrefix + fileInput.files[0].name; 
 
                    //Create the service request object     
                    var req = new XMLHttpRequest();     
                    req.open('PUT', uploadURL);
                    req.onreadystatechange = function(){         
                        document.getElementById('uploadHdr').innerText = 'Upload (request status == ' + req.status + ')';
                        // Un-comment the line below and comment-out the line above if you want the page to          
                        // refresh after the upload         
                        //location.reload();     
                    };     
                    req.send(fileInput.files[0]); 
                } 
                </script> "
                );
            }

            html.AppendLine("<h1>Folders</h1>"); //label the beginning of folders
            foreach (Dir422 dir in directory.GetDirs())
            {
                html.AppendLine(
                    String.Format("<a href=\"{0}\">{1}</a>", GetHREFFromDir422(dir), dir.Name) //FIX THIS, first one should be full path
                    );
                html.AppendLine("</br>");
            }

            html.AppendLine("<h1>Files</h1>"); //label the beginning of files

            foreach (File422 file in directory.GetFiles())
            {
                html.AppendLine(
                    String.Format("<a href=\"{0}\">{1}</a>", GetHREFFromFile422(file), file.Name) //FIX THIS, first one should be full path
                );
                html.AppendLine("</br>"); //append new lines for styling
            }

            if (m_allowUploads) //if you can upload, show the upload button and browser
            {
                html.AppendFormat(
                    "<hr><h3 id='uploadHdr'>Upload</h3><br>" +
                    "<input id=\"uploader\" type='file' " +
                    "onchange='selectedFileChanged(this,\"{0}\")' /><hr>",
                    GetHREFFromDir422(directory) //give a reference to this folder
                    );
            } //end uploading area

            html.AppendLine("</html>");
            return html.ToString();
        }

        private void RespondWithList(Dir422 dir, WebRequest req)
        {
            req.WriteHTMLResponse(BuildDirHTML(dir).ToString());
        }

        private void RespondWithFile(File422 file, WebRequest req) //return a file
        {
            string contentType = "text/html";//default to text/html

            if (file.Name.Contains(".jpg") || file.Name.Contains(".jpeg"))
                contentType = "image/jpeg";
            else if (file.Name.Contains(".gif"))
                contentType = "image/gif";
            else if (file.Name.Contains(".png"))
                contentType = "image/png";
            else if (file.Name.Contains(".pdf"))
                contentType = "application/pdf";
            else if (file.Name.Contains(".mp4"))
                contentType = "video/mp4";
            else if (file.Name.Contains(".xml"))
                contentType = "text/xml";



            req.WriteHTMLResponse(file.OpenReadOnly(), contentType); //write a page as a file
        }

        string GetHREFFromFile422(File422 file) //get filepath from file
        {
            string path = ""; //path string
            path = uriPath + '/' + file.Name;
            path = encode(path);
            return path;

        }

        string GetHREFFromDir422(Dir422 dir) //get filepath from directory
        {
            string path = ""; //path string

            if (!uriPath.EndsWith("/")) //if you're not in root
                path = uriPath + '/' + dir.Name;
            else //if you're in root
                path = uriPath + dir.Name;

            path = encode(path); //encode it for HTML
            return path;
        }

        string encode(string decodedString) //adds %20 for spaces and other character encodes
        {
            string encodedString = "";

            encodedString = decodedString.Replace(" ", "%20"); //encoding space with %20

            return encodedString;
        }

        string decode(string encodedString) //removes %20 for spaces and other character encodes
        {
            string decodedString = "";

            decodedString = encodedString.Replace("%20", " "); //replace %20 with space

            return decodedString;
        }
    }
}

