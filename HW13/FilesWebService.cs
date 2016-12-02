using System;
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
        public FilesWebService(FileSys422 fs)
        {
            r_sys = fs;
            uriPath = null;
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
            for (int i = 0; i < pieces.Length -1; i++) //go through the parts of the path
            {
                
                dir = dir.getDir(pieces[i]);
                if (dir == null) //if you encounter a directory that doesn't exist, tell the user that the target they requested is not found and return
                {
                    req.WriteNotFoundResponse("File not found.\n");
                    return;
                }
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

            return decodedString ;
        }
    }
}

