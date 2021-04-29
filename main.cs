using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace formbye
{
    class Program
    {
        static public string accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
        static public string link, msg = "", body;
        static public int n = 0, sent = 0, thrds = 5, scs = 0;
        static void Main(string[] args)
        {
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i].ToLower();
                    if (arg == "/l")
                    {
                        if (args[i + 1][0] != 'h') throw new ArgumentException("Invalid argument given for /L");
                        link = args[++i];
                    }
                    if (arg == "/t")
                    {
                        int num;
                        bool test = int.TryParse(args[++i], out num);
                        if(!test) throw new ArgumentException("Please enter a numeric argument for /T.");
                        thrds = num;
                    }
                    if (arg == "/m") msg = args[++i];
                    if (arg == "/n")
                    {
                        int num;
                        bool test = int.TryParse(args[++i], out num);
                        if (!test) throw new ArgumentException("Please enter a numeric argument for /N.");
                        n = num;
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Invalid Parameters. {0}",e.Message);
                return;
            }
            string tofetch = Base64Encode(getLinkVar());
            if(tofetch == "error")
            {
                Console.WriteLine("Error while fetching the link.");
                return;
            }
            string data = getData(tofetch);
            if(data == "error")
            {
                Console.WriteLine("Error while getting the data from link.");
                return;
            }
            body = getBody(data.Split(','));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("You will be spamming '{0}' with '{1}' {2} times. Are you sure you want to continue?", link, msg, n.ToString());
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("(y / n) ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            string _in = Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.White;
            if (_in.ToLower() != "y")
            {
                Console.WriteLine("Aborted.");
                return;
            }
            StringBuilder builder = new StringBuilder(link);
            builder.Replace("viewform", "formResponse");
            link = builder.ToString();
            Thread t = new Thread(StartNThreads);
            t.Start();
        }

        #region spam
        static TimeSpan timeout = new TimeSpan(0, 0, 3);
        static void Payload(int num)
        {
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(link);
                httpWebRequest.Host = "docs.google.com";
                httpWebRequest.Method = "POST";
                httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                httpWebRequest.Accept = accept;
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(body);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                try
                {
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    int code = (int)httpResponse.StatusCode;
                    if (code == 200)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("[T:{0}] Successfully sent a request!", num);
                        scs += 1;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[T:{0}] Unsucessfully sent a request ({1}). {2}", num, code, httpResponse.StatusDescription);
                    }
                }
                catch (WebException ex)
                {
                    using (var stream = ex.Response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        string err = reader.ReadToEnd();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[T:{0}] WebException Catched.", num, err.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", ""));
                    }
                }
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("[T:{0}] Unhandeled exception", num);
            }
        }
        static void ThreadPayload(int num)
        {
            while (true)
            {
                if (scs >= n)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("Sent {0} requests, {1} successfull. Exiting", sent, scs);
                    Console.ResetColor();
                    Environment.Exit(Environment.ExitCode);
                }
                Thread t = new Thread(() => Payload(num));
                t.Start();
                if(t.Join(timeout)) sent += 1;
                else
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("[{0}] Thread timed out", num);
                    Console.ResetColor();
                }
                Thread.Sleep(150);
            }
        }
        static public void StartNThreads()
        {
            int threadNum = 0;
            for (int i = 0; i < thrds; i++)
            {
                threadNum += 1;
                Thread t = new Thread(() => ThreadPayload(threadNum));
                t.Start();
            }
        }
        #endregion

        #region link fetching
        public static string getBody(string[] data)
        {
            string res = "";
            foreach(string s in data) res += "entry." + s + "=" + msg + "&";
            return res.Substring(0, res.Length-1);
        }
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        public static string getData(string js)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.CreateNoWindow = true;

            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C node -e \"try{let buff = new Buffer('"+ js + "', 'base64'); eval(buff.toString('ascii')); var idNums = FB_PUBLIC_LOAD_DATA_[1][1]; for (var i = 0; i < idNums.length; i++) { if (idNums[i][3] > 7) continue; var arr = idNums[i][4]; for (var j = 0; j < arr.length; j++) { process.stdout.write(arr[j][0].toString()); } if (i + 1 < idNums.length) process.stdout.write(\\\",\\\"); } } catch { process.stdout.write(\\\"error\\\"); }\"";
            process.StartInfo = startInfo;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();

            if (string.IsNullOrEmpty(output))
            {
                output = process.StandardError.ReadToEnd();
            }
            process.WaitForExit();
            return output;
        }
        public static string getLinkVar()
        {
            string l = link;
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(l);
                httpWebRequest.Host = "docs.google.com";
                httpWebRequest.Method = "GET";

                httpWebRequest.ContentType = "application/x-www-form-urlencoded";

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    string res = streamReader.ReadToEnd().ToString();
                    int ind = res.IndexOf("var FB_PUBLIC_LOAD_DATA_");
                    res = res.Substring(ind, res.Length - ind);
                    res = res.Substring(0, res.IndexOf(";</script>")+1);
                    return res;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while getting the link.");
                Console.WriteLine(e);
                return "error";
            }
        }
        #endregion
    }
}
