using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GTAServer;
using System.Threading;
using System.Threading.Tasks;

namespace Turfs
{
    public enum TurfMapState
    {
        TMAPSTATE_NORMAL = 0,
        TMAPSTATE_BLINKING = 1,
        TMAPSTATE_AVAILABLE,
    }
    public enum TurfColors
    {
        COLOR_WHITE = 0,
        COLOR_RED = 1,
        COLOR_GREEN = 2,
        COLOR_BLUE = 3,
        COLOR_ORANGE = 17,
    }
    public class TurfInfo
    {
        public string turfName { get; set; }
        public int turfOwner { get; set; }
        public DateTime startClaimTime { get; set; }
        public DateTime lastClaim { get; set; }
        public bool beingClaimed { get; set; }
        public DateTime nextTick { get; set; }
        public Vector3 turfPos { get; set; }
        public TurfMapState turfBlinkState { get; set; }
        public TurfColors turfColor { get; set; }
        public Race.Teams turfTeam { get; set; }
        public float turfRange { get; set; }
    }

    public class TurfClass
    {
        Vector3[] TurfPositions = new[] { 
            new Vector3(-149.7129f, -1559.995f, 34.8223f), //Crips turf
            new Vector3(30.6187f, -1877.898f, 22.4998f) //Bloods turf
        };

        public enum TurfArrayEnum
        {
            TURF_NAME = 0,
            TURF_POSX = 1,
            TURF_POSY,
            TURF_POSZ,
            TURF_COLOR,
            TURF_TEAM,
            TURF_RANGE,
        }

        public Object[,] TurfArray = new Object[,] {
            {"Chamberlain Hills Turf",-149.7129f, -1559.995f, 34.8223f, TurfColors.COLOR_BLUE, Race.Teams.TEAM_CRIPS, 150.0f}, 
            {"Davis Block Turf", 30.6187f, -1877.898f, 22.4998f, TurfColors.COLOR_RED, Race.Teams.TEAM_BLOODS, 150.0f},
            {"JamesTown St. Turf", 328.4394f, -2022.034f, 22.49279f, TurfColors.COLOR_WHITE, Race.Teams.TEAM_MS13, 150.0f}
        };

        public static List<TurfInfo> Turfs { get; set; }
        public void initTurfs()
        {
            Turfs = new List<TurfInfo>();
            for (int i = 0; i <= TurfArray.GetUpperBound(0); i++)
            {
                Turfs.Add(new TurfInfo() { 
                    turfName = (string)TurfArray[i, (int)TurfArrayEnum.TURF_NAME],
                    turfPos = new Vector3((float)TurfArray[i, (int)TurfArrayEnum.TURF_POSX], (float)TurfArray[i, (int)TurfArrayEnum.TURF_POSY], (float)TurfArray[i, (int)TurfArrayEnum.TURF_POSZ]),
                    turfColor = (TurfColors)TurfArray[i, (int)TurfArrayEnum.TURF_COLOR],
                    turfTeam = (Race.Teams)TurfArray[i, (int)TurfArrayEnum.TURF_TEAM],
                    turfRange = (float)TurfArray[i, (int)TurfArrayEnum.TURF_RANGE]
                });
            }
            foreach (var turf in Turfs)
	        {
                int num = numTeamMembersInTurf(turf, 1);
	        }
        }
        public int numTeamMembersInTurf(TurfInfo turf, int teamid)  
        {
            Race.Gamemode gm = new Race.Gamemode();
	        int num = 0;
            lock (Race.Gamemode.GPlayers)
            {
                foreach (var player in Race.Gamemode.GPlayers)
                {
                    if ((int)player.TeamID == teamid)
                    {
                        if (player.Spawned == true)
                        {
                            if (Race.RangeExtension.IsInRangeOf(player.Client.LastKnownPosition, turf.turfPos, turf.turfRange))
                            {
                                num++;
                            }
                        }
                    }
                }
            }
	        return num;
        }
        bool turfwargoing = false;
        public int MAX_CAPTURE_TIME = 2; //minutes (currently seconds for testing)
        public int TURF_COOLDOWN = 3; //minutes
        public int TURF_NEXT_TICK = 1; //seconds
        public int TURF_PEOPLE_NEEDED = 1;
        public void MakeTurfFlashForPlayers(int turfteam, bool flash = true)
        {
            lock (Race.Gamemode.GPlayers)
            {
                foreach (var player in Race.Gamemode.GPlayers) //This runs asynchronously and GPlayers could be null at any time while this may be executed so it needs try catch
                {
                    Race.GPlayer curOp = Race.Gamemode.GPlayers.FirstOrDefault(op => op.Client == player.Client);
                    
                    if (curOp != null)
                    {
                        lock(curOp.TurfBlips) //Maybe some of the turfblips are null?
                        {
                            foreach (var element in curOp.TurfBlips.OrderByDescending(x => x.Key))
                            {
                                if (element.Key == (int)turfteam)
                                {
                                    MakeBlipFlashForPlayer(player.Client, element.Value, flash);
                                }
                            }
                        }
                    }
                }
            }
        }
        public void MakeBlipFlashForPlayer(Client player, int blip, bool flash = true)
        {
            Program.ServerInstance.SendNativeCallToPlayer(player, 0xB14552383D39CE3E, blip, flash); //SET_BLIP_FLASHES
        }
        int findTeamIndexInTeamsArrayByTeamID(int teamid)
        {
            Race.Gamemode gm = new Race.Gamemode();
            for (int i = 0; i < gm.TeamsArray.GetLength(0); i++)
            {
                if((int)gm.TeamsArray[i, (int)Race.Gamemode.TeamEnum.TEAM_TEAMID] == teamid) 
                {
                    return i;
                }
            }
            return -1;
        }
        public int GetTurfWarLeader()
        {
            int maxscore = 0, maxi = 0;
            for (int i = 0; i < TurfPoints.GetLength(0); i++)
            {
                if (TurfPoints[i] > maxscore)
                {
                    maxscore = TurfPoints[i];
                    maxi = i;
                }
            }
            return maxi;
        }
        bool isTurfTied() {
	        int leader = GetTurfWarLeader();
	        int maxscore = TurfPoints[leader];
	        for(int i=0;i<TurfPoints.GetLength(0);i++) {
		        if(TurfPoints[i] >= maxscore && i != leader && maxscore != 0) {
			        return true;
		        }
	        }
	        return false;
        }
        public static int MAX_TURF_EXTENSIONS = 5;
        public static int numturfextensions = 0;
        public void OnTurfTick()
        {
            Race.Gamemode gm = new Race.Gamemode();
            if (UtilsExtension.IsNullOrEmpty(Turfs) || UtilsExtension.IsNullOrEmpty(Race.Gamemode.GPlayers)) return;
            foreach (var turf in Turfs) {
                if (DateTime.Now.Subtract(turf.nextTick).TotalSeconds >= TURF_NEXT_TICK)
                {
                    int leader = GetTurfWarLeader();
                    for (int i = 0; i < gm.TeamsArray.GetLength(0); i++)
                    {
                        if (turf.beingClaimed)
                        {
                            if (DateTime.Now.Subtract(turf.startClaimTime).TotalMinutes >= MAX_CAPTURE_TIME) //See if the time is past the limit
                            {
                                if (isTurfTied() && numturfextensions < MAX_TURF_EXTENSIONS) {
                                    numturfextensions++;
                                    turf.startClaimTime.AddMinutes(1.0f); //One more minute
                                    Program.ServerInstance.SendNotificationToAll("The turf is tied, extending the round for one more minute.");
                                } else if (numTeamMembersInTurf(turf, (int)gm.TeamsArray[leader, (int)Race.Gamemode.TeamEnum.TEAM_TEAMID]) == 0) {
                                    MakeTurfFlashForPlayers((int)turf.turfTeam, false);
                                    Program.ServerInstance.SendNotificationToAll(string.Format("The leading team abandoned {0} so it was awarded to no one.", turf.turfName));
                                    turf.beingClaimed = false;
                                } else {
                                    turf.lastClaim = DateTime.Now;
                                    turf.beingClaimed = false;
                                    turf.turfColor = (TurfColors)gm.TeamsArray[leader, (int)Race.Gamemode.TeamEnum.TEAM_COLOR];
                                    turfwargoing = false;
                                    MakeTurfFlashForPlayers((int)turf.turfTeam, false);
                                    setturfBlipPropertiesForAll(turf, (int)gm.TeamsArray[leader, (int)Race.Gamemode.TeamEnum.TEAM_COLOR], 128, turf.turfRange / 25.0f);
                                    Program.ServerInstance.SendNotificationToAll(string.Format("The {0} took over the turf.", Convert.ToString(gm.TeamsArray[leader, (int)Race.Gamemode.TeamEnum.TEAM_NAME])));
                                    turf.turfTeam = (Race.Teams)gm.TeamsArray[leader, (int)Race.Gamemode.TeamEnum.TEAM_TEAMID];
                                }
                                for (int j = 0; j < TurfPoints.GetLength(0); j++)
                                {
                                    TurfPoints[i] = 0;
                                }
                                numturfextensions = 0;
                            }

                            if (numTeamMembersInTurf(turf, (int)gm.TeamsArray[i, (int)Race.Gamemode.TeamEnum.TEAM_TEAMID]) > numTeamMembersInTurf(turf, leader))
                            {
                                Program.ServerInstance.SendNotificationToAll(string.Format("{0} are now in the lead and were awarded a point.", Convert.ToString(gm.TeamsArray[i, (int)Race.Gamemode.TeamEnum.TEAM_NAME])));
                                giveTurfPoint(i, true);
                            }
                        }
                        else
                        {
                            if (!turfwargoing)
                            {
                                if (numTeamMembersInTurf(turf, (int)gm.TeamsArray[i, (int)Race.Gamemode.TeamEnum.TEAM_TEAMID]) >= TURF_PEOPLE_NEEDED)
                                {
                                    if ((int)turf.turfTeam != (int)gm.TeamsArray[i, (int)Race.Gamemode.TeamEnum.TEAM_TEAMID])
                                    {
                                        if (DateTime.Now.Subtract(turf.lastClaim).Minutes >= TURF_COOLDOWN)
                                        {
                                            turf.beingClaimed = true;
                                            turf.startClaimTime = DateTime.Now;
                                            turfwargoing = true;
                                            MakeTurfFlashForPlayers((int)turf.turfTeam, true);
                                            Program.ServerInstance.SendNotificationToAll(string.Format("{0} are trying to take over {1}.", Convert.ToString(gm.TeamsArray[i, (int)Race.Gamemode.TeamEnum.TEAM_NAME]), turf.turfName));
                                        }
                                    }
                                }
                                else
                                {
                                    if (numTeamMembersInTurf(turf, (int)gm.TeamsArray[i, (int)Race.Gamemode.TeamEnum.TEAM_TEAMID]) == 0)
                                    {
                                        MakeTurfFlashForPlayers((int)turf.turfTeam, false);
                                    }
                                }
                            }
                        }
                        turf.nextTick = DateTime.Now;
                    }
                }
            }
        }
        static int [] TurfPoints = new int[Race.Gamemode.MAX_TEAMS];
        public bool turfsOnPlayerCommand(Client player, string text)
        {
            if (text.StartsWith("/reloadturfs"))
            {
                Program.ServerInstance.SendChatMessageToPlayer(player, "Info", "Turfs reloaded!");
                var nt = new Thread((ThreadStart)delegate
                {
                    RefreshTurfsForPlayer(player);
                });
                //PlayAudioTrackForPlayer(Player, "RADIO_09_HIPHOP_OLD", "STRAIGHT_UP_MENACE");
                nt.Start();
                return false;
            }
            return false;
        }
        public void turfSetBlipForAll(TurfInfo turf, float scale = 200.0f, int color = -1)
        {
            if (UtilsExtension.IsNullOrEmpty(Race.Gamemode.GPlayers)) return;
            lock (Race.Gamemode.GPlayers)
            {
                foreach (var player in Race.Gamemode.GPlayers)
                {
                    turfSetBlipForPlayer(player.Client, turf);
                }
            }
        }
        public bool turfSetBlipForPlayer(Client player, TurfInfo turf, float scale = 200.0f, int color = -1)
        {
            Console.WriteLine(string.Format("turfSetBlipForPlayer called: turf {0} color {1} scale {2}\n", turf, color, scale));
            if (UtilsExtension.IsNullOrEmpty(Race.Gamemode.GPlayers)) return false;
            Race.GPlayer curOp = Race.Gamemode.GPlayers.FirstOrDefault(op => op.Client == player);
            if (curOp == null) return false;
            try
            {
                Program.ServerInstance.GetNativeCallFromPlayer(curOp.Client, "turf_blip" + "_" + turf.turfName, 0x5A039BB0BCA604B6, new IntArgument(), // ADD_BLIP_FOR_COORD
                delegate(object o)
                {
                    lock (Race.Gamemode.GPlayers)
                    {
                        if (curOp != null)
                        {
                            if (!curOp.TurfBlips.ContainsKey((int)turf.turfTeam))
                            {
                                try
                                {
                                    curOp.TurfBlips.Add((int)turf.turfTeam, (int)o);
                                    setturfBlipProperties(turf, curOp.Client, curOp.TurfBlips[(int)turf.turfTeam], (int)turf.turfColor, 128, turf.turfRange / 25.0f);
                                }
                                catch { }
                            }
                        }
                    }
                }, turf.turfPos.X, turf.turfPos.Y, turf.turfPos.Z);
            }
            catch
            {
            }
            return true;
        }
        public void setturfBlipPropertiesForAll(TurfInfo turf, int color, int alpha, float scale)
        {
            if (UtilsExtension.IsNullOrEmpty(Race.Gamemode.GPlayers)) return;
            foreach (var player in Race.Gamemode.GPlayers)
            {
                Race.GPlayer curOp = Race.Gamemode.GPlayers.FirstOrDefault(op => op.Client == player.Client);
                lock (Race.Gamemode.GPlayers)
                {
                    if (curOp != null)
                    {
                        foreach (var element in curOp.TurfBlips.OrderByDescending(x => x.Key))
                        {
                            if (element.Key == (int)turf.turfTeam)
                            {
                                Program.ServerInstance.SendNativeCallToPlayer(player.Client, 0x45FF974EEE1C8734, curOp.TurfBlips[(int)turf.turfTeam], alpha); //setblipalpha

                                if (color != -1)
                                {
                                    Program.ServerInstance.SendNativeCallToPlayer(player.Client, 0x03D7FB09E75D6B7E, curOp.TurfBlips[(int)turf.turfTeam], color); //Setblipcolor
                                    Program.ServerInstance.SendNativeCallToPlayer(player.Client, 0xD38744167B2FA257, curOp.TurfBlips[(int)turf.turfTeam], scale); //setblipscale
                                }
                            }
                        }
                    }
                }
            }
        }
        public void setturfBlipProperties(TurfInfo turf, Client client, int blip, int color, int alpha, float scale)
        {
            Program.ServerInstance.SendNativeCallToPlayer(client, 0x45FF974EEE1C8734, blip, alpha); //setblipalpha
            if (color != -1)
            {
                Program.ServerInstance.SendNativeCallToPlayer(client, 0x03D7FB09E75D6B7E, blip, color); //Setblipcolor
                Program.ServerInstance.SendNativeCallToPlayer(client, 0xD38744167B2FA257, blip, scale); //setblipscale
            }
        }
        public void OnPlayerConnect(Client Player)
        {
            var nt = new Thread((ThreadStart)delegate
            {
                RefreshTurfsForPlayer(Player);
            });
            //PlayAudioTrackForPlayer(Player, "RADIO_09_HIPHOP_OLD", "STRAIGHT_UP_MENACE");
            nt.Start();
        }
        public void PlayAudioTrackForPlayer(Client player, string radiostation, string trackname)
        {
            //Program.ServerInstance.SendNativeCallToPlayer(player, 0x1F1F957154EC51DF, radiostation, trackname); //LOAD_STREAM
            Program.ServerInstance.SendNativeCallToPlayer(player, 0x1098355A16064BB3, true); //SET_MOBILE_RADIO_ENABLED_DURING_GAMEPLAY
            //Program.ServerInstance.SendNativeCallToPlayer(player, 0x67C540AA08E4A6F5, -1, trackname, 0, 1); //setblipscale
            //Program.ServerInstance.SendNativeCallToPlayer(player, 0xC69EDA28699D5107, radiostation);
        }
        public void OnPlayerDisconnect(Client Player) {
            Race.GPlayer curOp = Race.Gamemode.GPlayers.FirstOrDefault(op => op.Client == Player); //Get rid of the blips
            if (curOp == null) return;
            if (!UtilsExtension.IsNullOrEmpty(curOp.TurfBlips))
            {
                lock (curOp.TurfBlips)
                {
                    foreach (var element in curOp.TurfBlips.OrderByDescending(x => x.Key))
                    {
                        Program.ServerInstance.SendNativeCallToPlayer(curOp.Client, 0x45FF974EEE1C8734, element.Value, 0);
                    }
                    curOp.TurfBlips.Clear();
                }
            }
        }

        public void RefreshTurfsForPlayer(Client player)
        {
            Race.GPlayer curOp = Race.Gamemode.GPlayers.FirstOrDefault(op => op.Client == player); //First get rid of the blips
            if (curOp == null) return;
            if (!UtilsExtension.IsNullOrEmpty(curOp.TurfBlips))
            {
                lock (curOp.TurfBlips)
                {
                    foreach (var element in curOp.TurfBlips.OrderByDescending(x => x.Key))
                    {
                        Program.ServerInstance.SendNativeCallToPlayer(curOp.Client, 0x45FF974EEE1C8734, element.Value, 0);
                    }
                    curOp.TurfBlips.Clear();
                }
            }

            Thread.Sleep(5000);
            try //Must bandaid the whole network when working with loops so it doesn't send it twice..
            {
                Console.WriteLine("Turf Amount {0}", Turfs.Count);
                for (int i = 0; i < Turfs.Count; i++) //Now load them all
                {
                    turfSetBlipForPlayer(player, Turfs[i], Turfs[i].turfRange / 25.0f, (int)Turfs[i].turfColor);
                    Console.WriteLine("Created blip {0}..\n", i);
                }
            }
            catch (ArgumentException e) {
                Console.WriteLine("RefreshTurfsForPlayer - Exception information: {0}", e);
            }
        }
        public void RefreshTurfsForAll()
        {
            try
            {
                if (UtilsExtension.IsNullOrEmpty(Race.Gamemode.GPlayers)) return;
                lock (Race.Gamemode.GPlayers)
                {
                    foreach (var player in Race.Gamemode.GPlayers)
                    {
                        RefreshTurfsForPlayer(player.Client);
                    }
                }
            }
            catch { } //This tick thingy seems to send double messages at times so watch out
        }
        public void giveTurfPoint(int teamindex, bool increment)
        {
            if (increment)
            {
                TurfPoints[teamindex] += 1;
            }
            else
            {
                TurfPoints[teamindex] -= 1;
            }
        }
    }
    public static class UtilsExtension
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                return true;

            return !enumerable.Any();
        }
    }
}
