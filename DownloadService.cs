using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;
using TabStatsClientLite.protobuf;

namespace TabStatsClientLite_v2
{
    public static class DownloadService
    {
        private static string GetDemoName(CDataGCCStrike15_v2_MatchInfo matchInfo,
            CMsgGCCStrike15_v2_MatchmakingServerRoundStats roundStats)
        {
            return "match730_" + string.Format("{0,21:D21}", roundStats.reservationid) + "_"
                + string.Format("{0,10:D10}", matchInfo.watchablematchinfo.tv_port) + "_"
                + matchInfo.watchablematchinfo.server_ip;
        }


        private static void ProcessRoundStats(CDataGCCStrike15_v2_MatchInfo matchInfo, CMsgGCCStrike15_v2_MatchmakingServerRoundStats roundStats, Dictionary<string, string> demoUrlList)
        {
            var demoName = GetDemoName(matchInfo, roundStats);
            if (roundStats.reservationid != 0 && roundStats.map != null)
            {
                demoUrlList.Add(demoName, roundStats.map);
            }
        }

        public static Dictionary<string, string> GetMatchUrls(string fPath)
        {
            var demoUrlList = new Dictionary<string, string>();

            var filePath = fPath;

            if (!File.Exists(filePath))
                File.Create(fPath).Dispose();

            using (var file = File.OpenRead(filePath))
            {
                try
                {
                    var matchList = Serializer.Deserialize<CMsgGCCStrike15_v2_MatchList>(file);
                    foreach (var matchInfo in matchList.matches)
                    {
                        // old definition
                        if (matchInfo.roundstats_legacy != null)
                        {
                            var roundStats = matchInfo.roundstats_legacy;
                            ProcessRoundStats(matchInfo, roundStats, demoUrlList);
                        }
                        else
                        {
                            // new definition
                            var roundStatsList = matchInfo.roundstatsall;
                            foreach (var roundStats in roundStatsList)
                            {
                                ProcessRoundStats(matchInfo, roundStats, demoUrlList);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    var applicationPath = AppDomain.CurrentDomain.BaseDirectory;
                    File.WriteAllText(Path.Combine(applicationPath, "downloaderror.log"), $"{e.Message}{Environment.NewLine}{e.InnerException}");
                }
            }

            return demoUrlList;
        }
    }
}
