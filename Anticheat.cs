using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTAServer;
using System.Threading;

namespace Anticheat
{
    class AnticheatClass
    {
        static uint[] weaponHashes = {
            0xA2719263, //WEAPON_UNARMED
            0xF9FBAEBE,
            0x08D4BE52,
            0x99B507EA,
            0x678B81B1,
            0x4E875F73,
            0x958A4A8F,
            0x440E4788,
            0x84BD7BFD,
            0x1B06D571,
            0x5EF9FEC4,
            0x22D8FE39,
            0x99AEEB3B,
            0x13532244,
            0x2BE6766B,
            0xEFE7E2DF,
            0xBFEFFF6D,
            0x83BF0278,
            0xAF113F99,
            0x9D07F764,
            0x7FD62962,
            0x1D073A89,
            0x7846A318,
            0xE284C527,
            0x9D61E50F,
            0x3656C8C1,
            0x05FC3C11,
            0x0C472FE2,
            0x33058E22,
            0xA284510B,
            0x4DD2DC56,
            0xB1CA77B1,
            0x166218FF,
            0x13579279,
            0x687652CE,
            0x42BF8A85,
            0x93E220BD,
            0x2C3731D9,
            0xFDBC8A50,
            0xA0973D5E,
            0x24B17070,
            0x060EC506,
            0x34A67B97,
            0xFDBADCED,
            0x88C78EB7,
            0x01B79F17,
            0x23C9F95C,
            0x497FACC3,
            0xBEFDC581,
            0x48E7B178,
            0xFF58C4FB,
            0x736F5990,
            0x8B7333FB,
            0x92BD4EBB,
            0x2024F4E8,
            0xCC34325E,
            0x07FC7D7A,
            0xA36D413E,
            0x145F1012,
            0xDF8E89EB
        };
        public static int MAX_HEALTH = 96;
        public static int MAX_HACK_WARNS = 3;
        public static int MAX_GODMODE_WARNS = 10;
        public static int TIME_ALLOW_ACSYNC = 5;
        public static int TIME_NEXT_SCAN = 1;
        public static int TIME_ALLOW_KILL_AGAIN = 20;

        public static DateTime lastscan;
        static uint[] TurfBannedWeapons = new uint[5] { 0xA284510B, 0x42BF8A85, 0x2C3731D9, 0x42BF8A85, 0xB1CA77B1 };
        public void AntiCheatTick()
        {
            if (Turfs.UtilsExtension.IsNullOrEmpty(Race.Gamemode.GPlayers)) return;
            if (DateTime.Now.Subtract(lastscan).TotalSeconds >= TIME_NEXT_SCAN)
            {
                lock (Race.Gamemode.GPlayers)
                {
                    foreach (var player in Race.Gamemode.GPlayers)
                    {
                        if (player.ACSync == null) continue;
                        if (DateTime.Now.Subtract(player.ACSync).TotalSeconds >= TIME_ALLOW_ACSYNC)
                        {
                            try
                            {
                                Turfs.TurfClass turf = new Turfs.TurfClass();
                                foreach (var turfx in Turfs.TurfClass.Turfs)
                                {
                                    if (Race.RangeExtension.IsInRangeOf(player.Client.LastKnownPosition, turfx.turfPos, turfx.turfRange))
                                    {
                                        Race.Gamemode gm = new Race.Gamemode();
                                        Program.ServerInstance.GetNativeCallFromPlayer(player.Client, "GET_PLAYER_HEALTH", 0xEEF059FAD016D209, new IntArgument(),
                                        delegate(object o)
                                        {
                                            if ((int)o > 100 + MAX_HEALTH)
                                            {
                                                if (player.Client.LastKnownPosition == null)
                                                {
                                                    return;
                                                }

                                                if (player.HealthHackWarns++ > MAX_HACK_WARNS)
                                                {
                                                    Program.ServerInstance.KickPlayer(player.Client, "~r~ANTICHEAT: ~w~Health Hacks in a turf");
                                                    player.HealthHackWarns = 0;
                                                    Program.ServerInstance.SendNotificationToAll("~r~ANTICHEAT: ~w~A player has been kicked for using health hacks in a turf.");
                                                }
                                                else
                                                {
                                                    SetPlayerHealthEx(player.Client, MAX_HEALTH);
                                                }
                                            }
                                        }, new LocalPlayerArgument());
                                        //Are they invincible?
                                        Program.ServerInstance.GetNativeCallFromPlayer(player.Client, "GET_PLAYER_INVINCIBLE", 0xB721981B2B939E07, new BooleanArgument(), delegate(object invincible)
                                        {
                                            if ((bool)invincible)
                                            {
                                                try
                                                { //NO NO NO MUST USE TRY CATCH IN BEFORE player.Client.LastKnownPosition IS NULL!!! :OOOOOOOO
                                                    if (Race.RangeExtension.IsInRangeOf(player.Client.LastKnownPosition, turfx.turfPos, turfx.turfRange / 2.0f))
                                                    {
                                                        if (DateTime.Now.Subtract(player.TurfACLastKillTime).TotalSeconds >= Anticheat.AnticheatClass.TIME_ALLOW_KILL_AGAIN)
                                                        {
                                                            if (player.godmodeWarns++ > MAX_GODMODE_WARNS)
                                                            {
                                                                Program.ServerInstance.SetPlayerHealth(player.Client, -1);
                                                                Program.ServerInstance.SendNotificationToPlayer(player.Client, "You have been killed for using health cheats at a turf.");
                                                                Program.ServerInstance.SendNativeCallToPlayer(player.Client, 0x239528EACDC3E7DE, 0, false); //setplayerinvincible
                                                                Program.ServerInstance.SendNotificationToAll("~r~ANTICHEAT: ~w~A player has been killed for using health cheats at a turf.");
                                                                player.TurfACLastKillTime = DateTime.Now;
                                                                player.godmodeWarns = 0;
                                                            }
                                                            else
                                                            {
                                                                Program.ServerInstance.SendNotificationToPlayer(player.Client, string.Format("~r~TURFS ANTICHEAT: ~w~You have ~r~{0} seconds to ~w~turn your godmode off.", MAX_GODMODE_WARNS - player.godmodeWarns));
                                                            }
                                                        }
                                                    }
                                                }
                                                catch { }
                                            }
                                        }, 0);
                                        try
                                        {
                                            //Must use try catch so we don't get the duplicate key error!!
                                            foreach (var weapon in TurfBannedWeapons)
                                            {
                                                Program.ServerInstance.GetNativeCallFromPlayer(player.Client, "HAS_PED_GOT_WEAPON", 0x8DECB02F88F428BC, new BooleanArgument(), delegate(object pedhasweapon)
                                                {
                                                    if ((bool)pedhasweapon)
                                                    {
                                                        Program.ServerInstance.SendNativeCallToPlayer(player.Client, 0xF25DF915FA38C5F3, new LocalPlayerArgument(), true); //Strip all weapons
                                                        Program.ServerInstance.SendNativeCallToPlayer(player.Client, 0xBF0FD6E56C964FCB, new LocalPlayerArgument(), unchecked((int)0x13532244), -1, true, true);
                                                        Program.ServerInstance.SendNativeCallToPlayer(player.Client, 0xBF0FD6E56C964FCB, new LocalPlayerArgument(), unchecked((int)0xBFEFFF6D), -1, true, true);
                                                        Program.ServerInstance.SendNotificationToAll("~r~ANTICHEAT: ~w~A player has had their weapons reset for having restricted weapons at a turf.");
                                                    }
                                                }, new LocalPlayerArgument(), unchecked((int)weapon), false);
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                lastscan = DateTime.Now;
            }
        }
        public static void SetPlayerHealthEx(Client player, int health)
        {
            if (Turfs.UtilsExtension.IsNullOrEmpty(Race.Gamemode.GPlayers)) return;
            Race.GPlayer curOp = Race.Gamemode.GPlayers.FirstOrDefault(op => op.Client == player);
            if (curOp == null) return;
            if (health < 0) health = 0;
            if (health > MAX_HEALTH)
            {
                //if(health < 0.0 || health > MAX_HEALTH+25.0) health = 0.0;
                //else
                health = MAX_HEALTH;
            }
            curOp.PlayerHealth = health;
            curOp.ACSync = DateTime.Now;
            Program.ServerInstance.SetPlayerHealth(player, curOp.PlayerHealth);
        }
    }
    
}
