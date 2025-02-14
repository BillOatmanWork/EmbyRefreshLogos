using Newtonsoft.Json;
using RestSharp;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;


namespace EmbyRefreshLogos
{
    public class EmbyRefreshLogos
    {
        private const string format = "http://{0}:{1}/emby/LiveTv/Manage/Channels&api_key={2}";
        private string agent = "EmbyRefreshLogos";
        private int logoCountount;
        private string? apiKey;

        private Dictionary<string, string> channelData = new Dictionary<string, string>();

        private List<Item> retryItems = [];

        static void Main(string[] args)
        {
            EmbyRefreshLogos p = new EmbyRefreshLogos();
            p.RealMain(args);
        }

        private void RealMain(string[] args)
        {
            File.Delete("EmbyRefreshLogos.log");

            ConsoleWithLog($"EmbyRefreshLogos version {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            ConsoleWithLog("");

            if (args.Length < 2)
            {
                ConsoleWithLog("EmbyRefreshLogos filePath API_KEY [server] [port]");
                ConsoleWithLog("File extenstion can be .m3u, .xml, or .xmltv");
                ConsoleWithLog("To get Emby api key go to dashboard>advanced>security and generate one");
                return;
            }

            string fileName = args[0];
            string host = "localhost";
            string port = "8096";
            apiKey = args[1];

            if (args.Length == 3)
            {
                host = args[2];
            }
            else if (args.Length == 4)
            {
                host = args[2];
                port = args[3];
            }
            else if (args.Length != 2)
            {
                ConsoleWithLog("EmbyRefreshLogos filePath API_KEY [server] [port]");
                ConsoleWithLog("File extenstion can be .m3u, .xml, or .xmltv");
                ConsoleWithLog("To get Emby api key go to dashboard>advanced>security and generate one");
                return;
            }

            ConsoleWithLog($"Reading file {fileName}.");

            if (!File.Exists(fileName))
            {
                ConsoleWithLog($"Specified file {fileName} not found.");
                ConsoleWithLog("EmbyRefreshLogos filePath API_KEY [server] [port]");
                ConsoleWithLog("File extenstion can be .m3u, .xml, or .xmltv"); ;
                ConsoleWithLog("To get Emby api key go to dashboard>advanced>security and generate one");
                return;
            }

            if(fileName.EndsWith(".m3u"))
            {
                try
                {
                    ReadM3u(fileName);
                }
                catch (Exception ex)
                {
                    ConsoleWithLog($"Problem trying to read the m3u file {fileName}.");
                    ConsoleWithLog($"Exception: {ex.Message}");
                    return;
                }
            }
            else if (fileName.EndsWith(".xml") || fileName.EndsWith(".xmltv"))
            {
                try
                {
                    ReadXmlTv(fileName);
                }
                catch (Exception ex)
                {
                    ConsoleWithLog($"Problem trying to read the XMLTV file {fileName}.");
                    ConsoleWithLog($"Exception: {ex.Message}");
                    return;
                }
            }
            else
            {
                ConsoleWithLog($"EmbyRefreshLogos Error: File extension {Path.GetExtension(fileName)} not supported.");
                return;
            }

            string uriName = string.Format(CultureInfo.InvariantCulture, format, host, port, apiKey);

            try
            {
                var restClient = new RestClient($"http://{host}:{port}");
                RestRequest restRequest = new RestRequest($"emby/LiveTv/Manage/Channels?api_key={apiKey}", Method.Get);
                restRequest.AddHeader("user-agent", agent);
                var restResponse = restClient.Execute(restRequest);
                if (restResponse.StatusCode != HttpStatusCode.OK || restResponse.Content is null)
                {
                    ConsoleWithLog($"EmbyRefreshLogos Error Getting Emby Channels: {restResponse.StatusCode}  {restResponse.StatusDescription}");
                }
                else
                {
                    Root? channelsData = JsonConvert.DeserializeObject<Root>(restResponse.Content);

                    if (channelsData == null || channelsData.Items is null)
                    {
                        ConsoleWithLog("EmbyRefreshLogos Error: No channels found.");
                        return;
                    }

                    //using (StreamWriter file = File.CreateText(@"C:\Stuff\Repos\EmbyRefreshLogos\test\channelsData.json"))
                    //{
                    //    file.Write(JsonPrettify(restResponse.Content));
                    //}

                    foreach (Item item in channelsData.Items)
                    {
                        ConsoleWithLog($"Processing {item.Name} ...");
                        ProcessChannelData(item, restClient, false);
                    }

                    if (retryItems.Count == 0)
                    {
                        ConsoleWithLog("Processing channels that were not found in the first pass ...");
                        channelData.Clear();

                        if (fileName.EndsWith(".m3u"))
                        {
                            ReadXmlTv(Path.ChangeExtension(fileName, ".xmltv"));
                        }
                        else
                        { 
                            ReadM3u(Path.ChangeExtension(fileName, ".m3u"));
                        }

                        foreach (Item item in retryItems)
                        {
                            ConsoleWithLog($"ReProcessing {item.Name} ...");
                            ProcessChannelData(item, restClient, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleWithLog($"EmbyRefreshLogos Exception: {ex.Message}.");
            }

            ConsoleWithLog($"EmbyRefreshLogos Complete. Number of logos set: {logoCountount}.");
        }

        private void ProcessChannelData(Item item, RestClient restClient, bool retry)
        {
            if (item.Id is null || item.Name is null)
                return;

            string id = item.Id;
            bool found = channelData.TryGetValue(item.Name, out string? logoUrl);

            if (found && !string.IsNullOrEmpty(logoUrl))
            {
                RestRequest restRequest2 = new RestRequest($"emby/Items/{id}/Images/Primary/0/Url?Url={logoUrl}&api_key={apiKey}", Method.Post);
                restRequest2.AddHeader("user-agent", agent);
                var restResponse2 = restClient.Execute(restRequest2);

                if (!restResponse2.IsSuccessful)
                    ConsoleWithLog($"Failed to set logo for {item.Name}. Reason: {restResponse2.ErrorException?.Message ?? "Unknown error"}");
                else
                    logoCountount++;
            }
            else
            {
                if (retry)
                    ConsoleWithLog($"EmbyRefreshLogos: Could not find logo for {item.Name} in the second pass.");
                else
                {

                    ConsoleWithLog($"EmbyRefreshLogos: Could not find logo for {item.Name} in the first pass.");
                    retryItems.Add(item);
                }
            }
        }

        /// <summary>
        /// Read m3u file and store channel name and logo url in dictionary
        /// </summary>
        /// <param name="fileName"></param>
        private void ReadM3u(string fileName)
        {

                string pattern = @"\btvg-name=""([^""]+)"".tvg-logo=""([^""]+)"".group-title=""([^""]+)"",(.*?)\n*(https?\S+)";
                string input = File.ReadAllText(fileName);

                foreach (Match m in Regex.Matches(input, pattern))
                {
                    string channelName = m.Groups[4].Value.TrimEnd('\r', '\n');
                    string logoUrl = m.Groups[2].Value;

                try
                {
                    channelData.Add(channelName, logoUrl);
                }
                catch (Exception ex)
                {
                    ConsoleWithLog("Non-fatal problem trying to read the m3u file. ");
                    ConsoleWithLog($"Exception: {ex.Message}  Channel: {channelName}  LogoURL: {logoUrl}");
                }
            }
        }

        /// <summary>
        /// Read xmltv file and store channel name and logo url in dictionary
        /// </summary>
        /// <param name="fileName"></param>
        public void ReadXmlTv(string fileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            if(doc is null)
            {
                ConsoleWithLog($"EmbyRefreshLogos Error: Could not load XMLTV file {fileName}.");
                return;
            }

            XmlNodeList? channels = doc.SelectNodes("//channel");

            if(channels is null)
            {
                ConsoleWithLog($"EmbyRefreshLogos Error: Could not find channels in XMLTV file {fileName}.");
                return;
            }

            foreach (XmlNode channel in channels)
            {
                string? channelName = channel.SelectSingleNode("display-name")?.InnerText;
                if (channelName is null)
                {
                    ConsoleWithLog($"EmbyRefreshLogos Error: Channel name is null in XMLTV file {fileName}.");
                    continue;
                }

                // string channelId = channel.Attributes["id"].Value;

                string? logoUrl = channel.SelectSingleNode("icon")?.Attributes?["src"]?.Value;

                if(logoUrl is null)
                {
                    ConsoleWithLog($"EmbyRefreshLogos Error: Logo URL is null for channel {channelName} in XMLTV file {fileName}.");
                    continue;
                }

                try
                {
                    channelData.Add(channelName, logoUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Non-fatal problem trying to read the XMLTV file.");
                    Console.WriteLine($"Exception: {ex.Message}  Channel: {channelName}  LogoURL: {logoUrl}");
                }
            }
        }

        /// <summary>
        /// Writes to console and log file
        /// </summary>
        /// <param name="text"></param>
        public static void ConsoleWithLog(string text)
        {
            Console.WriteLine(text);

            using (StreamWriter file = File.AppendText("EmbyRefreshLogos.log"))
            {
                file.Write(text + Environment.NewLine);
            }
        }

        /// <summary>
        /// Indents and adds line breaks etc to make it pretty for printing/viewing
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static string JsonPrettify(string json)
        {
            using (var stringReader = new StringReader(json))
            using (var stringWriter = new StringWriter())
            {
                var jsonReader = new JsonTextReader(stringReader);
                var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Newtonsoft.Json.Formatting.Indented };
                jsonWriter.WriteToken(jsonReader);
                return stringWriter.ToString();
            }
        }
    }
}
