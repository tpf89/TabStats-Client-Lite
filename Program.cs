using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace TabStatsClientLite_v2
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("TabStatsClient Lite started...");
            Console.WriteLine("Checking if Steam is running...");

            if (!IsSteamActive())
            {
                Console.WriteLine("You need to start and log into Steam first. Restart this client.");
                Console.WriteLine("Press enter to close this client.");
                Console.ReadLine();
                Environment.Exit(0);
            }

            Console.WriteLine("Press enter as soon as you are logged into Steam");

            if (IsCsgoExeActive())
            {
                Console.WriteLine("It seems that a csgo.exe process is running. Please terminate the process(es) before continuing.");
                Console.WriteLine("Press enter to continue");
                Console.ReadLine();
            }

            if (!CheckForInternetConnection())
            {
                Console.WriteLine("It seems that TabStats is not reachable. Check www.tabstats.net and/or check your internet connection.");
                Console.WriteLine("Press enter to continue");
                Console.ReadLine();
            }

            SendMatchIds();
            Console.WriteLine();
            Console.WriteLine("Press enter to close this client.");
            Console.ReadLine();
        }

        #region Checks
        public static bool IsCsgoExeActive()
        {
            var currentProcess = Process.GetProcessesByName("csgo");
            return currentProcess.Length > 0;
        }

        public static bool IsSteamActive()
        {
            var currentProcess = Process.GetProcessesByName("steam");
            return currentProcess.Length > 0;
        }

        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Headers.Add("user-agent", "TabStats Client");

                    using (var stream = client.OpenRead("https://www.tabstats.net/"))
                    {
                        return true;
                    }
                }
            }
            catch (WebException e)
            {
                return false;
            }
        }
        #endregion

        #region logic
        private static async void SendMatchIds()
        {
            var matchIds = GetMatchIds();

            if (matchIds.Count > 0)
            {
                if (await SendMatchIds(matchIds))
                {
                    Console.WriteLine("Success!");
                    Console.WriteLine($"The client sent the following match ids to TabStats:");
                    foreach (var matchId in matchIds)
                    {
                        Console.WriteLine(matchId);
                    }
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("Couldn't contact the TabStats server. Please try again later or contact TabStats.");
                }
            }
            else
            {
                Console.WriteLine("Couldn't get new match ids, probably the CS:GO replay servers are having problems right now. Please try again later.");
                Console.WriteLine("If the problem persists, please visit www.tabstats.net and contact me.");
            }
        }

        public static async Task<bool> SendMatchIds(List<string> matchIds)
        {
            try
            {
                var parameter = matchIds.Aggregate(string.Empty, (current, matchId) => current + $"{matchId},");

                parameter = parameter.Remove(parameter.Length - 1);

                var getUrl = $"https://www.tabstats.net/AddMatches/AddMatchIds?matchIds={parameter}";

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "TabStats Client");
                    var response = await httpClient.GetAsync(getUrl);

                    response.EnsureSuccessStatusCode();

                    httpClient.Dispose();
                    var sd = response.Content.ReadAsStringAsync().Result;
                    var errorMessage = sd;

                    if (sd.Contains("Match ids sent"))
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                return false;
            }

            return false;
        }

        public static List<string> GetMatchIds()
        {
            var matchIds = new List<string>(8);

            const string matchesDatFile = "matches.dat";
            const string boilerExe = "boiler.exe";

            if (!File.Exists(matchesDatFile))
            {
                File.Create(matchesDatFile).Dispose();
            }


            var boiler = new Process
            {
                StartInfo =
                {
                    FileName = boilerExe,
                    Arguments = "matches.dat",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            boiler.Start();
            Console.WriteLine("Boiler.exe started");
            Console.WriteLine("\tGetting replay urls");

            boiler.WaitForExit();
            Console.WriteLine("Boiler.exe exited");

            var matchUrls = DownloadService.GetMatchUrls(matchesDatFile);


            if (matchUrls != null)
            {
                matchIds = matchUrls.Keys.ToList();
            }

            return matchIds;
        }
        #endregion
    }
}
