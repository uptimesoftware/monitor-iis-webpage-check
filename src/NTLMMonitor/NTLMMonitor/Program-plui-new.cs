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
        static int Main(string[] args)
        {
            int returnCode = 0;
            try
            {
                Arguments arguments = new Arguments(args);               
                string port = string.Empty;
                string authtype = string.Empty;
                string domain = null;
                bool ssl = false;

                if (nullemptycheck(arguments["port"])) { port = "80"; } else {port = arguments["port"];}
                if (nullemptycheck(arguments["authtype"])) { authtype = "NTLM"; } else { authtype = arguments["authtype"]; }
                if (nullemptycheck(arguments["domain"])) { domain = null; } else { domain = arguments["domain"]; }
                if (nullemptycheck(arguments["ssl"])) { ssl = false; } else { ssl = Convert.ToBoolean(arguments["ssl"]); }
                if (nullemptycheck(arguments["username"]) || nullemptycheck(arguments["password"]) || nullemptycheck(arguments["url"]) || nullemptycheck(arguments["host"]))
                {
                    Console.Write(printHelp());
                    System.Environment.Exit(1);
                }
                else
                {
                    // Build out the URL
                    StringBuilder completeURL = new StringBuilder();
                    if (ssl == true)
                    {
                        completeURL.Append("https://" + arguments["host"] + ":" + port + "/" + arguments["url"]);
                    }
                    else
                    {
                        completeURL.Append("http://" + arguments["host"] + ":" + port + "/" + arguments["url"]);
                    }

                    // Pass all arguments to get the webpage 
                    // returnCode: 0 = OK, 2 = Critical
                    returnCode = relayRequest(completeURL.ToString(), arguments["username"], arguments["password"], domain, authtype, port, ssl);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                System.Environment.Exit(1);
            }

            return returnCode;
        }
       
        static int relayRequest(string requeststring, string username, string password, string domain, string authtype, string port, bool ssl)
        {
            StringBuilder sb = new StringBuilder();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requeststring);

            // Accepts all certificate when SSL is used
            if (ssl == true)
            {            
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };              
            }

            request.AllowAutoRedirect = true;


            // Setup credentials
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

                // Get the stream associated with the response.
                Stream receiveStream = response.GetResponseStream();

                // Pipes the stream to a higher level stream reader with the required encoding format. 
                StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);

                //Console.WriteLine("Response stream received.");
                Console.Write("Output ");
                Console.WriteLine(readStream.ReadToEnd().Replace(System.Environment.NewLine, System.Environment.NewLine + "Output "));
                response.Close();
                readStream.Close();


                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Console.WriteLine("Output Successfully Login.");
                    return 0;
                }
                else
                {
                    Console.WriteLine("Output Failed.  HTTP Code:" + response.StatusCode.ToString().Replace(System.Environment.NewLine, System.Environment.NewLine + "Output")); 
                    return 2;
                }
            }
            catch (System.Net.WebException e)
            {                
                //e.ToString().Replace(System.Environment.NewLine, System.Environment.NewLine + "Output");
                Console.WriteLine("Output Failed.  Reason:" + e.ToString().Replace(System.Environment.NewLine, System.Environment.NewLine + "Output"));
                return 2;
            }
            

        }
        static string printHelp()
        {
            string help = "Parameters:\r\n" +
                "   -username : Name of user making the request (required)\r\n" +
                "   -password : Password of user making the request (required)\r\n" +
                "   -url      : Url of page you wish to load (required)\r\n" +
                "   -host     : Hostname or ip address which you wish to test (required)\r\n" +
                "   -domain   : [optional] domain of user making the request.\r\n" +               
                "   -port     : [optional] Port website is running on (default is 80)\r\n" +
                "   -ssl      : [optional] [false|true] Whether SSL is enabled or not (default is false)\r\n";
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
