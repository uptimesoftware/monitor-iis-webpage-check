using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.IO;
using System.Text;

namespace WebCheckNTLM
{
    /// <summary>
    /// This is a utility which can be used to test web pages secured by IIS ntlm or kerberos, also sort of support digest and negotiate auth. 
    /// Written by Seth Hahner, supported by the man/woman in the mirror. 
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Arguments arguments = new Arguments(args);
                string output = string.Empty;
                //authtypes = NTLM, Kerberos, Basic, Digest, Negotiate
                string port = string.Empty;
                string authtype = string.Empty;
                string domain = null;

                if (nullemptycheck(arguments["port"])) { port = "80"; } else {port = arguments["port"];}
                if (nullemptycheck(arguments["authtype"])) { authtype = "NTLM"; } else { authtype = arguments["authtype"]; }
                if (nullemptycheck(arguments["domain"])) { domain = null; } else { domain = arguments["domain"]; }

                if (nullemptycheck(arguments["username"]) || nullemptycheck(arguments["password"]) || nullemptycheck(arguments["url"]) || nullemptycheck(arguments["host"]))
                {
                    Console.Write(printHelp());
                }
                else
                {
                    output = relayRequest(arguments["url"], arguments["username"], arguments["password"], domain, authtype, arguments["host"], port);
                }
                Console.WriteLine(output);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                System.Environment.Exit(1);
            }
        }

        static string relayRequest(string requeststring, string username, string password, string domain, string authtype, string hostname, string port)
        {
            StringBuilder sb = new StringBuilder();
            byte[] buf = new byte[8192];
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requeststring);
//Console.WriteLine(requeststring);
//HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://demo-shart01/default.aspx");


            request.AllowAutoRedirect = true;
            request.Proxy = new WebProxy(hostname,Int32.Parse(port));
            NetworkCredential credential;
            if (domain != null)
            {
                credential = new NetworkCredential(username, password, domain);
            }
            else
            {
                credential = new NetworkCredential(username, password);
            }
            CredentialCache credentialCache = new CredentialCache();
            credentialCache.Add(new Uri(requeststring), authtype, credential);
            request.Credentials = credentialCache;
            
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();


Stream resStream = response.GetResponseStream();
string tempString = null;
int count = 0;
do
{
    count = resStream.Read(buf, 0, buf.Length);
    if (count != 0)
    {
        tempString = Encoding.ASCII.GetString(buf, 0, count);
        sb.Append(tempString);
    }
}
while (count > 0); // any more data to read?
Console.Write(sb.ToString());



                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return "OK REALLY??";
                }
                else
                {
                    return "Failed.  HTTP Code:" + response.StatusCode;
                }
            }
            catch (System.Net.WebException e)
            {
                return "Failed.  Reason:" + e.ToString();
            }
            

        
/*
            Stream resStream = response.GetResponseStream();
            string tempString = null;
            int count = 0;
            do
            {
                count = resStream.Read(buf, 0, buf.Length);
                if (count != 0)
                {
                    tempString = Encoding.ASCII.GetString(buf, 0, count);
                    sb.Append(tempString);
                }
            }
            while (count > 0); // any more data to read?
            return sb.ToString();
 */ 
        }
        static string printHelp()
        {
            string help = "Parameters:\r\n" +
                "   -username : Name of user making the request (required)\r\n" +
                "   -password : Password of user making the request (required)\r\n" +
                "   -url      : Url of page you wish to load (required)\r\n" +
                "   -host     : Hostname or ip address which you wish to test (required)\r\n" +
                "   -domain   : [optional] domain of user making the request.\r\n" +
                "   -authtype : [optional] [NTLM, Kerberos, Digest, Negotiate(default)]\r\n" +
                "   -port     : [optional] Port website is running on (default is 80)\r\n";
            return help;
        }
        static bool nullemptycheck(object ckobject)
        {
            if(ckobject == null || (string)ckobject==String.Empty)
                return true;
            return false;


        }
    }
    public class Arguments{
        // Variables

        private StringDictionary Parameters;
        public int length {
            get { return Parameters.Count; }
        }
        // Constructor

        public Arguments(string[] Args)
        {
            Parameters = new StringDictionary();
            Regex Spliter = new Regex(@"^-{1,2}",
                RegexOptions.IgnoreCase|RegexOptions.Compiled);

            Regex Remover = new Regex(@"^['""]?(.*?)['""]?$",
                RegexOptions.IgnoreCase|RegexOptions.Compiled);

            string Parameter = null;
            string[] Parts;

            // Valid parameters forms:

            // {-,/,--}param{ ,=,:}((",')value(",'))

            // Examples: 

            // -param1 value1 --param2 /param3:"Test-:-work" 

            //   /param4=happy -param5 '--=nice=--'

            foreach(string Txt in Args)
            {
                // Look for new parameters (-,/ or --) and a

                // possible enclosed value (=,:)

                Parts = Spliter.Split(Txt,3);

                switch(Parts.Length){
                // Found a value (for the last parameter 

                // found (space separator))

                case 1:
                    if(Parameter != null)
                    {
                        if(!Parameters.ContainsKey(Parameter)) 
                        {
                            Parts[0] = 
                                Remover.Replace(Parts[0], "$1");

                            Parameters.Add(Parameter, Parts[0]);
                        }
                        Parameter=null;
                    }
                    // else Error: no parameter waiting for a value (skipped)

                    break;

                // Found just a parameter

                case 2:
                    // The last parameter is still waiting. 

                    // With no value, set it to true.

                    if(Parameter!=null)
                    {
                        if(!Parameters.ContainsKey(Parameter)) 
                            Parameters.Add(Parameter, "true");
                    }
                    Parameter=Parts[1];
                    break;

                // Parameter with enclosed value

                case 3:
                    // The last parameter is still waiting. 

                    // With no value, set it to true.

                    if(Parameter != null)
                    {
                        if(!Parameters.ContainsKey(Parameter)) 
                            Parameters.Add(Parameter, "true");
                    }

                    Parameter = Parts[1];

                    // Remove possible enclosing characters (",')

                    if(!Parameters.ContainsKey(Parameter))
                    {
                        Parts[2] = Remover.Replace(Parts[2], "$1");
                        Parameters.Add(Parameter, Parts[2]);
                    }

                    Parameter=null;
                    break;
                }
            }
            // In case a parameter is still waiting

            if(Parameter != null)
            {
                if(!Parameters.ContainsKey(Parameter)) 
                    Parameters.Add(Parameter, "true");
            }
        }

        // Retrieve a parameter value if it exists 

        // (overriding C# indexer property)

        public string this [string Param]
        {
            get
            {
                return(Parameters[Param]);
            }
        }
    }

}
