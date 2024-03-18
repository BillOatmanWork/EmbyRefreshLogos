﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Net;
using System.Text.RegularExpressions;


namespace EmbyRefreshLogos
{
    public class EmbyRefreshLogos
    {
        private const string format1 = "http://{0}:{1}/emby/LiveTv/Manage/Channels&api_key={2}";
        private string agent = "EmbyRefreshLogos";

        private Dictionary<string, string> channelData = new Dictionary<string, string>();

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

            int count = 0;
            string m3u = args[0];
            string host = "localhost";
            string port = "8096";
            string key = args[1];

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
                ConsoleWithLog("EmbyRefreshLogos m3uPath API_KEY {server} {port}");
                ConsoleWithLog("To get Emby api key go to dashboard>advanced>security and generate one");
                return;
            }
            
            if (!File.Exists(m3u))
            {
                ConsoleWithLog($"Specified m3u file {m3u} not found.");
                ConsoleWithLog("EmbyRefreshLogos m3uPath API_KEY {server} {port}");
                ConsoleWithLog("To get Emby api key go to dashboard>advanced>security and generate one");
                return;
            }

            try
            {
                ReadM3u(m3u);
            }
            catch (Exception ex)
            {
                ConsoleWithLog("Problem trying to read the m3u file. ");
                ConsoleWithLog($"Exception: {ex.Message}");
                return;
            }

            string uriName = string.Format(format1, host, port, key);

            try
            {
                var restClient = new RestClient($"http://{host}:{port}");
                RestRequest restRequest = new RestRequest($"emby/LiveTv/Manage/Channels?api_key={key}", Method.Get);
                restRequest.AddHeader("user-agent", agent);
                var restResponse = restClient.Execute(restRequest);
                if (restResponse.StatusCode != HttpStatusCode.OK)
                {
                    ConsoleWithLog($"EmbyRefreshLogos Error Getting Emby Channels: {restResponse.StatusCode}  {restResponse.StatusDescription}");
                }
                else
                {
                    Root? channelsData = JsonConvert.DeserializeObject<Root>(restResponse.Content);

                    if(channelsData == null)
                    {
                        ConsoleWithLog("EmbyRefreshLogos Error: No channels found.");
                        return;
                    }

                    //using (StreamWriter file = File.CreateText(@"C:\Stuff\EmbyRefreshLogos\test\channelsData.json"))
                    //{
                    //    file.Write(JsonPrettify(restResponse.Content));
                    //}

                    foreach (Item item in channelsData.Items)
                    {
                        ConsoleWithLog($"Processing {item.Name} ...");
                        string id = item.Id;
                        bool found = channelData.TryGetValue(item.Name, out string? logoUrl);

                        if (found)
                        {

                            RestRequest restRequest2 = new RestRequest($"emby/Items/{id}/Images/Primary/0/Url?Url={logoUrl}&api_key={key}", Method.Post);
                            restRequest2.AddHeader("user-agent", agent);
                            var restResponse2 = restClient.Execute(restRequest2);

                            if (!restResponse2.IsSuccessful)
                                ConsoleWithLog($"Failed to set logo for {item.Name}.");
                            else
                                count++;
                        } 
                        else 
                        {
                            ConsoleWithLog($"EmbyRefreshLogos: Could not find logo for {item.Name}.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleWithLog($"EmbyRefreshLogos Exception: {ex.Message}.");
            }

            ConsoleWithLog($"EmbyRefreshLogos Complete. Number of logos set: {count}.");
        }

        private void ReadM3u(string fileName)
        {
            try
            {
                string pattern = @"\btvg-name=""([^""]+)"".tvg-logo=""([^""]+)"".group-title=""([^""]+)"",(.*?)\n*(https?\S+)";
                string input = File.ReadAllText(fileName);

                foreach (Match m in Regex.Matches(input, pattern))
                {
                    string channelName = m.Groups[4].Value.TrimEnd('\r', '\n');
                    string logoUrl = m.Groups[2].Value;
                    channelData.Add(channelName, logoUrl);
                }
            }
            catch (Exception ex)
            {
                ConsoleWithLog("Problem trying to read the m3u file. ");
                ConsoleWithLog($"Exception: {ex.Message}");
            }
        }

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
