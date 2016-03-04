using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GTAServer;

namespace Race
{
    public enum Teams
    {
        TEAM_CIVILIAN = 0,
        TEAM_BLOODS = 1,
        TEAM_CRIPS,
        TEAM_COPS,
        TEAM_MS13,
    }

    public class GPlayer
    {
        public GPlayer(Client c)
        {
            Client = c;
            CheckpointsPassed = 0;
        }

        public Client Client { get; set; }
        public int CheckpointsPassed { get; set; }
        public bool HasFinished { get; set; }
        public bool HasStarted { get; set; }
        public int Vehicle { get; set; }
        public int Blip { get; set; }
        public Dictionary<int, int> TurfBlips { get; set; }

        public DateTime DeathTime { get; set; }
        public DateTime ACSync { get; set; }
        public bool Spawned { get; set; }
        public Teams TeamID { get; set;  }
        public int LastSkinUsed { get; set; }

        public int PlayerHealth;
        public DateTime LastMapUIRefresh { get; set; }

        public DateTime TurfACLastKillTime { get; set; }
        public int HealthHackWarns;
        public int godmodeWarns;
    }

    public class Gamemode : ServerScript
    {
        public bool IsRaceOngoing { get; set; }
        public static List<GPlayer> GPlayers { get; set; }
        public List<Race> AvailableRaces { get; set; }
        public Dictionary<long, int> RememberedBlips { get; set; }
        public DateTime RaceStart { get; set; }

        // Voting
        public DateTime VoteStart { get; set; }
        public List<Client> Voters { get; set; }
        public Dictionary<int, int> Votes { get; set; }
        public Dictionary<int, Race> AvailableChoices { get; set; }

        static string[] vehModels = {
	        "NINEF", "NINEF2", "BLISTA", "ASEA", "ASEA2", "BOATTRAILER", "BUS", "ARMYTANKER", "ARMYTRAILER", "ARMYTRAILER2" ,
	        "SUNTRAP", "COACH", "AIRBUS", "ASTEROPE", "AIRTUG", "AMBULANCE", "BARRACKS", "BARRACKS2", "BALLER", "BALLER2" ,
	        "BJXL", "BANSHEE", "BENSON", "BFINJECTION", "BIFF", "BLAZER", "BLAZER2", "BLAZER3", "BISON", "BISON2" ,
	        "BISON3", "BOXVILLE", "BOXVILLE2", "BOXVILLE3", "BOBCATXL", "BODHI2", "BUCCANEER", "BUFFALO", "BUFFALO2", "BULLDOZER" ,
	        "BULLET", "BLIMP", "BURRITO", "BURRITO2", "BURRITO3", "BURRITO4", "BURRITO5", "CAVALCADE", "CAVALCADE2", "POLICET" ,
	        "GBURRITO", "CABLECAR", "CADDY", "CADDY2", "CAMPER", "CARBONIZZARE", "CHEETAH", "COMET2", "COGCABRIO", "COQUETTE" ,
	        "CUTTER", "GRESLEY", "DILETTANTE", "DILETTANTE2", "DUNE", "DUNE2", "HOTKNIFE", "DLOADER", "DUBSTA", "DUBSTA2" ,
	        "DUMP", "RUBBLE", "DOCKTUG", "DOMINATOR", "EMPEROR", "EMPEROR2", "EMPEROR3", "ENTITYXF", "EXEMPLAR", "ELEGY2" ,
	        "F620", "FBI", "FBI2", "FELON", "FELON2", "FELTZER2", "FIRETRUK", "FLATBED", "FORKLIFT", "FQ2" ,
	        "FUSILADE", "FUGITIVE", "FUTO", "GRANGER", "GAUNTLET", "HABANERO", "HAULER", "HANDLER", "INFERNUS", "INGOT" ,
	        "INTRUDER", "ISSI2", "JACKAL", "JOURNEY", "JB700", "KHAMELION", "LANDSTALKER", "LGUARD", "MANANA", "MESA" ,
	        "MESA2", "MESA3", "CRUSADER", "MINIVAN", "MIXER", "MIXER2", "MONROE", "MOWER", "MULE", "MULE2" ,
	        "ORACLE", "ORACLE2", "PACKER", "PATRIOT", "PBUS", "PENUMBRA", "PEYOTE", "PHANTOM", "PHOENIX", "PICADOR" ,
	        "POUNDER", "POLICE", "POLICE4", "POLICE2", "POLICE3", "POLICEOLD1", "POLICEOLD2", "PONY", "PONY2", "PRAIRIE" ,
	        "PRANGER", "PREMIER", "PRIMO", "PROPTRAILER", "RANCHERXL", "RANCHERXL2", "RAPIDGT", "RAPIDGT2", "RADI", "RATLOADER" ,
	        "REBEL", "REGINA", "REBEL2", "RENTALBUS", "RUINER", "RUMPO", "RUMPO2", "RHINO", "RIOT", "RIPLEY" ,
	        "ROCOTO", "ROMERO", "SABREGT", "SADLER", "SADLER2", "SANDKING", "SANDKING2", "SCHAFTER2", "SCHWARZER", "SCRAP" ,
	        "SEMINOLE", "SENTINEL", "SENTINEL2", "ZION", "ZION2", "SERRANO", "SHERIFF", "SHERIFF2", "SPEEDO", "SPEEDO2" ,
	        "STANIER", "STINGER", "STINGERGT", "STOCKADE", "STOCKADE3", "STRATUM", "SULTAN", "SUPERD", "SURANO", "SURFER" ,
	        "SURFER2", "SURGE", "TACO", "TAILGATER", "TAXI", "TRASH", "TRACTOR", "TRACTOR2", "TRACTOR3", "GRAINTRAILER" ,
	        "BALETRAILER", "TIPTRUCK", "TIPTRUCK2", "TORNADO", "TORNADO2", "TORNADO3", "TORNADO4", "TOURBUS", "TOWTRUCK", "TOWTRUCK2" ,
	        "UTILLITRUCK", "UTILLITRUCK2", "UTILLITRUCK3", "VOODOO2", "WASHINGTON", "STRETCH", "YOUGA", "ZTYPE", "SANCHEZ", "SANCHEZ2" ,
	        "SCORCHER", "TRIBIKE", "TRIBIKE2", "TRIBIKE3", "FIXTER", "CRUISER", "BMX", "POLICEB", "AKUMA", "CARBONRS" ,
	        "BAGGER", "BATI", "BATI2", "RUFFIAN", "DAEMON", "DOUBLE", "PCJ", "VADER", "VIGERO", "FAGGIO2" ,
	        "HEXER", "ANNIHILATOR", "BUZZARD", "BUZZARD2", "CARGOBOB", "CARGOBOB2", "CARGOBOB3", "SKYLIFT", "POLMAV", "MAVERICK" ,
	        "NEMESIS", "FROGGER", "FROGGER2", "CUBAN800", "DUSTER", "STUNT", "MAMMATUS", "JET", "SHAMAL", "LUXOR" ,
	        "TITAN", "LAZER", "CARGOPLANE", "SQUALO", "MARQUIS", "DINGHY", "DINGHY2", "JETMAX", "PREDATOR", "TROPIC" ,
	        "SEASHARK", "SEASHARK2", "SUBMERSIBLE", "TRAILERS", "TRAILERS2", "TRAILERS3", "TVTRAILER", "RAKETRAILER", "TANKER", "TRAILERLOGS" ,
	        "TR2", "TR3", "TR4", "TRFLAT", "TRAILERSMALL", "VELUM", "ADDER", "VOLTIC", "VACCA", "BIFTA" ,
	        "SPEEDER", "PARADISE", "KALAHARI", "JESTER", "TURISMOR", "VESTRA", "ALPHA", "HUNTLEY", "THRUST", "MASSACRO" ,
	        "MASSACRO2", "ZENTORNO", "BLADE", "GLENDALE", "PANTO", "PIGALLE", "WARRENER", "RHAPSODY", "DUBSTA3", "MONSTER" ,
	        "SOVEREIGN", "INNOVATION", "HAKUCHOU", "FUROREGT", "MILJET", "COQUETTE2", "BTYPE", "BUFFALO3", "DOMINATOR2", "GAUNTLET2" ,
	        "MARSHALL", "DUKES", "DUKES2", "STALION", "STALION2", "BLISTA2", "BLISTA3", "DODO", "SUBMERSIBLE2", "HYDRA" ,
	        "INSURGENT", "INSURGENT2", "TECHNICAL", "SAVAGE", "VALKYRIE", "KURUMA", "KURUMA2", "JESTER2", "CASCO", "VELUM2" ,
	        "GUARDIAN", "ENDURO", "LECTRO", "SLAMVAN", "SLAMVAN2", "RATLOADER2", "", "", "", "" 
        };
        static string[] pedModels = {
	        "player_zero", "player_one", "player_two", "a_c_boar", "a_c_chimp", "a_c_cow", "a_c_coyote", "a_c_deer", "a_c_fish", "a_c_hen" ,
	        "a_c_cat_01", "a_c_chickenhawk", "a_c_cormorant", "a_c_crow", "a_c_dolphin", "a_c_humpback", "a_c_killerwhale", "a_c_pigeon", "a_c_seagull", "a_c_sharkhammer" ,
	        "a_c_pig", "a_c_rat", "a_c_rhesus", "a_c_chop", "a_c_husky", "a_c_mtlion", "a_c_retriever", "a_c_sharktiger", "a_c_shepherd", "s_m_m_movalien_01" ,
	        "a_f_m_beach_01", "a_f_m_bevhills_01", "a_f_m_bevhills_02", "a_f_m_bodybuild_01", "a_f_m_business_02", "a_f_m_downtown_01", "a_f_m_eastsa_01", "a_f_m_eastsa_02", "a_f_m_fatbla_01", "a_f_m_fatcult_01" ,
	        "a_f_m_fatwhite_01", "a_f_m_ktown_01", "a_f_m_ktown_02", "a_f_m_prolhost_01", "a_f_m_salton_01", "a_f_m_skidrow_01", "a_f_m_soucentmc_01", "a_f_m_soucent_01", "a_f_m_soucent_02", "a_f_m_tourist_01" ,
	        "a_f_m_trampbeac_01", "a_f_m_tramp_01", "a_f_o_genstreet_01", "a_f_o_indian_01", "a_f_o_ktown_01", "a_f_o_salton_01", "a_f_o_soucent_01", "a_f_o_soucent_02", "a_f_y_beach_01", "a_f_y_bevhills_01" ,
	        "a_f_y_bevhills_02", "a_f_y_bevhills_03", "a_f_y_bevhills_04", "a_f_y_business_01", "a_f_y_business_02", "a_f_y_business_03", "a_f_y_business_04", "a_f_y_eastsa_01", "a_f_y_eastsa_02", "a_f_y_eastsa_03" ,
	        "a_f_y_epsilon_01", "a_f_y_fitness_01", "a_f_y_fitness_02", "a_f_y_genhot_01", "a_f_y_golfer_01", "a_f_y_hiker_01", "a_f_y_hippie_01", "a_f_y_hipster_01", "a_f_y_hipster_02", "a_f_y_hipster_03" ,
	        "a_f_y_hipster_04", "a_f_y_indian_01", "a_f_y_juggalo_01", "a_f_y_runner_01", "a_f_y_rurmeth_01", "a_f_y_scdressy_01", "a_f_y_skater_01", "a_f_y_soucent_01", "a_f_y_soucent_02", "a_f_y_soucent_03" ,
	        "a_f_y_tennis_01", "a_f_y_topless_01", "a_f_y_tourist_01", "a_f_y_tourist_02", "a_f_y_vinewood_01", "a_f_y_vinewood_02", "a_f_y_vinewood_03", "a_f_y_vinewood_04", "a_f_y_yoga_01", "a_m_m_acult_01" ,
	        "a_m_m_afriamer_01", "a_m_m_beach_01", "a_m_m_beach_02", "a_m_m_bevhills_01", "a_m_m_bevhills_02", "a_m_m_business_01", "a_m_m_eastsa_01", "a_m_m_eastsa_02", "a_m_m_farmer_01", "a_m_m_fatlatin_01" ,
	        "a_m_m_genfat_01", "a_m_m_genfat_02", "a_m_m_golfer_01", "a_m_m_hasjew_01", "a_m_m_hillbilly_01", "a_m_m_hillbilly_02", "a_m_m_indian_01", "a_m_m_ktown_01", "a_m_m_malibu_01", "a_m_m_mexcntry_01" ,
	        "a_m_m_mexlabor_01", "a_m_m_og_boss_01", "a_m_m_paparazzi_01", "a_m_m_polynesian_01", "a_m_m_prolhost_01", "a_m_m_rurmeth_01", "a_m_m_salton_01", "a_m_m_salton_02", "a_m_m_salton_03", "a_m_m_salton_04" ,
	        "a_m_m_skater_01", "a_m_m_skidrow_01", "a_m_m_socenlat_01", "a_m_m_soucent_01", "a_m_m_soucent_02", "a_m_m_soucent_03", "a_m_m_soucent_04", "a_m_m_stlat_02", "a_m_m_tennis_01", "a_m_m_tourist_01" ,
	        "a_m_m_trampbeac_01", "a_m_m_tramp_01", "a_m_m_tranvest_01", "a_m_m_tranvest_02", "a_m_o_acult_01", "a_m_o_acult_02", "a_m_o_beach_01", "a_m_o_genstreet_01", "a_m_o_ktown_01", "a_m_o_salton_01" ,
	        "a_m_o_soucent_01", "a_m_o_soucent_02", "a_m_o_soucent_03", "a_m_o_tramp_01", "a_m_y_acult_01", "a_m_y_acult_02", "a_m_y_beachvesp_01", "a_m_y_beachvesp_02", "a_m_y_beach_01", "a_m_y_beach_02" ,
	        "a_m_y_beach_03", "a_m_y_bevhills_01", "a_m_y_bevhills_02", "a_m_y_breakdance_01", "a_m_y_busicas_01", "a_m_y_business_01", "a_m_y_business_02", "a_m_y_business_03", "a_m_y_cyclist_01", "a_m_y_dhill_01" ,
	        "a_m_y_downtown_01", "a_m_y_eastsa_01", "a_m_y_eastsa_02", "a_m_y_epsilon_01", "a_m_y_epsilon_02", "a_m_y_gay_01", "a_m_y_gay_02", "a_m_y_genstreet_01", "a_m_y_genstreet_02", "a_m_y_golfer_01" ,
	        "a_m_y_hasjew_01", "a_m_y_hiker_01", "a_m_y_hippy_01", "a_m_y_hipster_01", "a_m_y_hipster_02", "a_m_y_hipster_03", "a_m_y_indian_01", "a_m_y_jetski_01", "a_m_y_juggalo_01", "a_m_y_ktown_01" ,
	        "a_m_y_ktown_02", "a_m_y_latino_01", "a_m_y_methhead_01", "a_m_y_mexthug_01", "a_m_y_motox_01", "a_m_y_motox_02", "a_m_y_musclbeac_01", "a_m_y_musclbeac_02", "a_m_y_polynesian_01", "a_m_y_roadcyc_01" ,
	        "a_m_y_runner_01", "a_m_y_runner_02", "a_m_y_salton_01", "a_m_y_skater_01", "a_m_y_skater_02", "a_m_y_soucent_01", "a_m_y_soucent_02", "a_m_y_soucent_03", "a_m_y_soucent_04", "a_m_y_stbla_01" ,
	        "a_m_y_stbla_02", "a_m_y_stlat_01", "a_m_y_stwhi_01", "a_m_y_stwhi_02", "a_m_y_sunbathe_01", "a_m_y_surfer_01", "a_m_y_vindouche_01", "a_m_y_vinewood_01", "a_m_y_vinewood_02", "a_m_y_vinewood_03" ,
	        "a_m_y_vinewood_04", "a_m_y_yoga_01", "u_m_y_proldriver_01", "u_m_y_rsranger_01", "u_m_y_sbike", "u_m_y_staggrm_01", "u_m_y_tattoo_01", "csb_abigail", "csb_anita", "csb_anton" ,
	        "csb_ballasog", "csb_bride", "csb_burgerdrug", "csb_car3guy1", "csb_car3guy2", "csb_chef", "csb_chin_goon", "csb_cletus", "csb_cop", "csb_customer" ,
	        "csb_denise_friend", "csb_fos_rep", "csb_g", "csb_groom", "csb_grove_str_dlr", "csb_hao", "csb_hugh", "csb_imran", "csb_janitor", "csb_maude" ,
	        "csb_mweather", "csb_ortega", "csb_oscar", "csb_porndudes", "csb_porndudes_p", "csb_prologuedriver", "csb_prolsec", "csb_ramp_gang", "csb_ramp_hic", "csb_ramp_hipster" ,
	        "csb_ramp_marine", "csb_ramp_mex", "csb_reporter", "csb_roccopelosi", "csb_screen_writer", "csb_stripper_01", "csb_stripper_02", "csb_tonya", "csb_trafficwarden", "cs_amandatownley" ,
	        "cs_andreas", "cs_ashley", "cs_bankman", "cs_barry", "cs_barry_p", "cs_beverly", "cs_beverly_p", "cs_brad", "cs_bradcadaver", "cs_carbuyer" ,
	        "cs_casey", "cs_chengsr", "cs_chrisformage", "cs_clay", "cs_dale", "cs_davenorton", "cs_debra", "cs_denise", "cs_devin", "cs_dom" ,
	        "cs_dreyfuss", "cs_drfriedlander", "cs_fabien", "cs_fbisuit_01", "cs_floyd", "cs_guadalope", "cs_gurk", "cs_hunter", "cs_janet", "cs_jewelass" ,
	        "cs_jimmyboston", "cs_jimmydisanto", "cs_joeminuteman", "cs_johnnyklebitz", "cs_josef", "cs_josh", "cs_lamardavis", "cs_lazlow", "cs_lestercrest", "cs_lifeinvad_01" ,
	        "cs_magenta", "cs_manuel", "cs_marnie", "cs_martinmadrazo", "cs_maryann", "cs_michelle", "cs_milton", "cs_molly", "cs_movpremf_01", "cs_movpremmale" ,
	        "cs_mrk", "cs_mrsphillips", "cs_mrs_thornhill", "cs_natalia", "cs_nervousron", "cs_nigel", "cs_old_man1a", "cs_old_man2", "cs_omega", "cs_orleans" ,
	        "cs_paper", "cs_paper_p", "cs_patricia", "cs_priest", "cs_prolsec_02", "cs_russiandrunk", "cs_siemonyetarian", "cs_solomon", "cs_stevehains", "cs_stretch" ,
	        "cs_tanisha", "cs_taocheng", "cs_taostranslator", "cs_tenniscoach", "cs_terry", "cs_tom", "cs_tomepsilon", "cs_tracydisanto", "cs_wade", "cs_zimbor" ,
	        "g_f_y_ballas_01", "g_f_y_families_01", "g_f_y_lost_01", "g_f_y_vagos_01", "g_m_m_armboss_01", "g_m_m_armgoon_01", "g_m_m_armlieut_01", "g_m_m_chemwork_01", "g_m_m_chemwork_01_p", "g_m_m_chiboss_01" ,
	        "g_m_m_chiboss_01_p", "g_m_m_chicold_01", "g_m_m_chicold_01_p", "g_m_m_chigoon_01", "g_m_m_chigoon_01_p", "g_m_m_chigoon_02", "g_m_m_korboss_01", "g_m_m_mexboss_01", "g_m_m_mexboss_02", "g_m_y_armgoon_02" ,
	        "g_m_y_azteca_01", "g_m_y_ballaeast_01", "g_m_y_ballaorig_01", "g_m_y_ballasout_01", "g_m_y_famca_01", "g_m_y_famdnf_01", "g_m_y_famfor_01", "g_m_y_korean_01", "g_m_y_korean_02", "g_m_y_korlieut_01" ,
	        "g_m_y_lost_01", "g_m_y_lost_02", "g_m_y_lost_03", "g_m_y_mexgang_01", "g_m_y_mexgoon_01", "g_m_y_mexgoon_02", "g_m_y_mexgoon_03", "g_m_y_mexgoon_03_p", "g_m_y_pologoon_01", "g_m_y_pologoon_01_p" ,
	        "g_m_y_pologoon_02", "g_m_y_pologoon_02_p", "g_m_y_salvaboss_01", "g_m_y_salvagoon_01", "g_m_y_salvagoon_02", "g_m_y_salvagoon_03", "g_m_y_salvagoon_03_p", "g_m_y_strpunk_01", "g_m_y_strpunk_02", "hc_driver" ,
	        "hc_gunman", "hc_hacker", "ig_abigail", "ig_amandatownley", "ig_andreas", "ig_ashley", "ig_ballasog", "ig_bankman", "ig_barry", "ig_barry_p" ,
	        "ig_bestmen", "ig_beverly", "ig_beverly_p", "ig_brad", "ig_bride", "ig_car3guy1", "ig_car3guy2", "ig_casey", "ig_chef", "ig_chengsr" ,
	        "ig_chrisformage", "ig_clay", "ig_claypain", "ig_cletus", "ig_dale", "ig_davenorton", "ig_denise", "ig_devin", "ig_dom", "ig_dreyfuss" ,
	        "ig_drfriedlander", "ig_fabien", "ig_fbisuit_01", "ig_floyd", "ig_groom", "ig_hao", "ig_hunter", "ig_janet", "ig_jay_norris", "ig_jewelass" ,
	        "ig_jimmyboston", "ig_jimmydisanto", "ig_joeminuteman", "ig_johnnyklebitz", "ig_josef", "ig_josh", "ig_kerrymcintosh", "ig_lamardavis", "ig_lazlow", "ig_lestercrest" ,
	        "ig_lifeinvad_01", "ig_lifeinvad_02", "ig_magenta", "ig_manuel", "ig_marnie", "ig_maryann", "ig_maude", "ig_michelle", "ig_milton", "ig_molly" ,
	        "ig_mrk", "ig_mrsphillips", "ig_mrs_thornhill", "ig_natalia", "ig_nervousron", "ig_nigel", "ig_old_man1a", "ig_old_man2", "ig_omega", "ig_oneil" ,
	        "ig_orleans", "ig_ortega", "ig_paper", "ig_patricia", "ig_priest", "ig_prolsec_02", "ig_ramp_gang", "ig_ramp_hic", "ig_ramp_hipster", "ig_ramp_mex" ,
	        "ig_roccopelosi", "ig_russiandrunk", "ig_screen_writer", "ig_siemonyetarian", "ig_solomon", "ig_stevehains", "ig_stretch", "ig_talina", "ig_tanisha", "ig_taocheng" ,
	        "ig_taostranslator", "ig_taostranslator_p", "ig_tenniscoach", "ig_terry", "ig_tomepsilon", "ig_tonya", "ig_tracydisanto", "ig_trafficwarden", "ig_tylerdix", "ig_wade" ,
	        "ig_zimbor", "mp_f_deadhooker", "mp_f_freemode_01", "mp_f_misty_01", "mp_f_stripperlite", "mp_g_m_pros_01", "mp_headtargets", "mp_m_claude_01", "mp_m_exarmy_01", "mp_m_famdd_01" ,
	        "mp_m_fibsec_01", "mp_m_freemode_01", "mp_m_marston_01", "mp_m_niko_01", "mp_m_shopkeep_01", "mp_s_m_armoured_01", "", "", "", "" ,
	        "", "s_f_m_fembarber", "s_f_m_maid_01", "s_f_m_shop_high", "s_f_m_sweatshop_01", "s_f_y_airhostess_01", "s_f_y_bartender_01", "s_f_y_baywatch_01", "s_f_y_cop_01", "s_f_y_factory_01" ,
	        "s_f_y_hooker_01", "s_f_y_hooker_02", "s_f_y_hooker_03", "s_f_y_migrant_01", "s_f_y_movprem_01", "s_f_y_ranger_01", "s_f_y_scrubs_01", "s_f_y_sheriff_01", "s_f_y_shop_low", "s_f_y_shop_mid" ,
	        "s_f_y_stripperlite", "s_f_y_stripper_01", "s_f_y_stripper_02", "s_f_y_sweatshop_01", "s_m_m_ammucountry", "s_m_m_armoured_01", "s_m_m_armoured_02", "s_m_m_autoshop_01", "s_m_m_autoshop_02", "s_m_m_bouncer_01" ,
	        "s_m_m_chemsec_01", "s_m_m_ciasec_01", "s_m_m_cntrybar_01", "s_m_m_dockwork_01", "s_m_m_doctor_01", "s_m_m_fiboffice_01", "s_m_m_fiboffice_02", "s_m_m_gaffer_01", "s_m_m_gardener_01", "s_m_m_gentransport" ,
	        "s_m_m_hairdress_01", "s_m_m_highsec_01", "s_m_m_highsec_02", "s_m_m_janitor", "s_m_m_lathandy_01", "s_m_m_lifeinvad_01", "s_m_m_linecook", "s_m_m_lsmetro_01", "s_m_m_mariachi_01", "s_m_m_marine_01" ,
	        "s_m_m_marine_02", "s_m_m_migrant_01", "u_m_y_zombie_01", "s_m_m_movprem_01", "s_m_m_movspace_01", "s_m_m_paramedic_01", "s_m_m_pilot_01", "s_m_m_pilot_02", "s_m_m_postal_01", "s_m_m_postal_02" ,
	        "s_m_m_prisguard_01", "s_m_m_scientist_01", "s_m_m_security_01", "s_m_m_snowcop_01", "s_m_m_strperf_01", "s_m_m_strpreach_01", "s_m_m_strvend_01", "s_m_m_trucker_01", "s_m_m_ups_01", "s_m_m_ups_02" ,
	        "s_m_o_busker_01", "s_m_y_airworker", "s_m_y_ammucity_01", "s_m_y_armymech_01", "s_m_y_autopsy_01", "s_m_y_barman_01", "s_m_y_baywatch_01", "s_m_y_blackops_01", "s_m_y_blackops_02", "s_m_y_busboy_01" ,
	        "s_m_y_chef_01", "s_m_y_clown_01", "s_m_y_construct_01", "s_m_y_construct_02", "s_m_y_cop_01", "s_m_y_dealer_01", "s_m_y_devinsec_01", "s_m_y_dockwork_01", "s_m_y_doorman_01", "s_m_y_dwservice_01" ,
	        "s_m_y_dwservice_02", "s_m_y_factory_01", "s_m_y_fireman_01", "s_m_y_garbage", "s_m_y_grip_01", "s_m_y_hwaycop_01", "s_m_y_marine_01", "s_m_y_marine_02", "s_m_y_marine_03", "s_m_y_mime" ,
	        "s_m_y_pestcont_01", "s_m_y_pilot_01", "s_m_y_prismuscl_01", "s_m_y_prisoner_01", "s_m_y_ranger_01", "s_m_y_robber_01", "s_m_y_sheriff_01", "s_m_y_shop_mask", "s_m_y_strvend_01", "s_m_y_swat_01" ,
	        "s_m_y_uscg_01", "s_m_y_valet_01", "s_m_y_waiter_01", "s_m_y_winclean_01", "s_m_y_xmech_01", "s_m_y_xmech_02", "u_f_m_corpse_01", "u_f_m_miranda", "u_f_m_promourn_01", "u_f_o_moviestar" ,
	        "u_f_o_prolhost_01", "u_f_y_bikerchic", "u_f_y_comjane", "u_f_y_corpse_01", "u_f_y_corpse_02", "u_f_y_hotposh_01", "u_f_y_jewelass_01", "u_f_y_mistress", "u_f_y_poppymich", "u_f_y_princess" ,
	        "u_f_y_spyactress", "u_m_m_aldinapoli", "u_m_m_bankman", "u_m_m_bikehire_01", "u_m_m_fibarchitect", "u_m_m_filmdirector", "u_m_m_glenstank_01", "u_m_m_griff_01", "u_m_m_jesus_01", "u_m_m_jewelsec_01" ,
	        "u_m_m_jewelthief", "u_m_m_markfost", "u_m_m_partytarget", "u_m_m_prolsec_01", "u_m_m_promourn_01", "u_m_m_rivalpap", "u_m_m_spyactor", "u_m_m_willyfist", "u_m_o_finguru_01", "u_m_o_taphillbilly" ,
	        "u_m_o_tramp_01", "u_m_y_abner", "u_m_y_antonb", "u_m_y_babyd", "u_m_y_baygor", "u_m_y_burgerdrug_01", "u_m_y_chip", "u_m_y_cyclist_01", "u_m_y_fibmugger_01", "u_m_y_guido_01" ,
            "u_m_y_gunvend_01", "u_m_y_hippie_01", "u_m_y_imporage", "u_m_y_justin", "u_m_y_mani", "u_m_y_militarybum", "u_m_y_paparazzi", "u_m_y_party_01", "u_m_y_pogo_01", "u_m_y_prisoner_01" 
        };
        public override void Start()
        {
            AvailableRaces = new List<Race>();
            GPlayers = new List<GPlayer>();
            RememberedBlips = new Dictionary<long, int>();
            Turfs.TurfClass turf = new Turfs.TurfClass();
            turf.initTurfs();

            Console.WriteLine("Clean Gamemode started!");
        }

        public bool IsVoteActive()
        {
            return DateTime.Now.Subtract(VoteStart).TotalSeconds < 60;
        }

        public override void OnTick()
        {
            var nt = new Thread((ThreadStart)delegate
            {
                lock (GPlayers)
                {
                    foreach (var player in GPlayers) //Why does c# try to loop through this if there's no elements in it? w/e.. http://stackoverflow.com/questions/3088147/why-does-net-foreach-loop-throw-nullrefexception-when-collection-is-null
                    {
                        try //Error-prone mod functions need try catch
                        {
                            Program.ServerInstance.GetNativeCallFromPlayer(player.Client, "IS_SCREEN_FADING_IN", 0x5C544BC6C57AC575, new BooleanArgument(), delegate(object isfadingin)
                            {
                                if (player.Spawned == false && GetPlayerHealthEx(player.Client) > 0) //Assuming they died 30 seconds ago and that their health is greater than 0..
                                {
                                    if ((bool)isfadingin == true)
                                    {
                                        player.Spawned = true;
                                        OnPlayerSpawn(player.Client);
                                    }
                                }
                            }, false);
                        }
                        catch { }
                    }
                }
                Turfs.TurfClass turf = new Turfs.TurfClass();
                Anticheat.AnticheatClass ac = new Anticheat.AnticheatClass();
                turf.OnTurfTick();
                ac.AntiCheatTick();
            });
            nt.Start();
        }
        public bool OnPlayerSpawn(Client player) {
            var newPos = SpawnPositions[new Random().Next(0, SpawnPositions.GetUpperBound(0))]; //Set the spawn position to that.
            GPlayer curOp = GPlayers.FirstOrDefault(op => op.Client == player);
            Anticheat.AnticheatClass.SetPlayerHealthEx(player, Anticheat.AnticheatClass.MAX_HEALTH);
            if (curOp == null) return true;
            if (curOp.TeamID != Teams.TEAM_CIVILIAN)
            {
                for (int c = 0; c < TeamsArray.GetLength(0); c++)
                {
                    if ((Teams)TeamsArray[c, (int)TeamEnum.TEAM_TEAMID] == curOp.TeamID)
                    {
                        TeamRespawn(player, c);
                        return true;
                    }
                }
                Program.ServerInstance.SetPlayerPosition(player, newPos);
                return true;
            }
            return false;
        }
        public int GetPlayerHealthEx(Client player)
        {
            GPlayer curOp = GPlayers.FirstOrDefault(op => op.Client == player);
            if (curOp == null) return 0;
            if (curOp.PlayerHealth < 0) curOp.PlayerHealth = 0;
            return curOp.PlayerHealth;
        }

        public void ApplyPatchesForPed(Client player)
        {
            //Patches
            //0xD80958FC74E988A6
            Program.ServerInstance.GetNativeCallFromPlayer(player, "GET_PLAYER_PED", 0x43A66C31C68491C0, new IntArgument(),
            delegate(object o)
            {
                try
                {
                    var pedid = unchecked((int)o); 
                    //Doesn't do anything 0 effect on the player whatsoever
                    Program.ServerInstance.SendNativeCallToPlayer(player, 0x3C49C870E66F0A28, 0, true); //GIVE_PLAYER_RAGDOLL_CONTROL
                    Program.ServerInstance.SendNativeCallToPlayer(player, 0xB128377056A54E2A, pedid, false); //SET_PED_CAN_RAGDOLL
                    Program.ServerInstance.SendNativeCallToPlayer(player, 0xDF993EE5E90ABA25, pedid, false); //SET_PED_CAN_RAGDOLL_FROM_PLAYER_IMPACT
                    Program.ServerInstance.SendNativeCallToPlayer(player, 0xF0A4F1BBF4FA7497, pedid, false); //SET_PED_RAGDOLL_ON_COLLISION
                    
                }
                catch { }
            }, 0);

            //End of patches
        }
        public bool TeamRespawn(Client player, int teamindex)
        {
            var newPos = SpawnPositions[new Random().Next(0, SpawnPositions.GetUpperBound(0))]; //Set the spawn position to that.
            GPlayer curOp = GPlayers.FirstOrDefault(op => op.Client == player);
            if (curOp == null) return true;
            //Position
            Vector3 Position = newPos;
            Position.X = (float)TeamsArray[teamindex, (int)TeamEnum.TEAM_POSX];
            Position.Y = (float)TeamsArray[teamindex, (int)TeamEnum.TEAM_POSY];
            Position.Z = (float)TeamsArray[teamindex, (int)TeamEnum.TEAM_POSZ];
            Program.ServerInstance.SetPlayerPosition(player, Position);

            var nt = new Thread((ThreadStart)delegate
            {
                //Skin setting
                int skin = -1;
                int tryFindNewSkins = 0;
                while (skin == -1)
                {
                    skin = TeamGetSkin(curOp.TeamID, tryFindNewSkins > 0 ? curOp.LastSkinUsed : -1);
                    if (skin == curOp.LastSkinUsed && tryFindNewSkins < 3)
                    {
                        tryFindNewSkins++;
                        continue;
                    }

                    if (skin != -1)
                    {
                        SetSkin(player, pedModels[skin]);
                        curOp.LastSkinUsed = skin;
                    }
                }
                Thread.Sleep(5000);
                Program.ServerInstance.SendNativeCallToPlayer(player, 0xF25DF915FA38C5F3, new LocalPlayerArgument(), true); //Strip all weapons
                Program.ServerInstance.SendNativeCallToPlayer(player, 0xBF0FD6E56C964FCB, new LocalPlayerArgument(), unchecked((int)0x13532244), -1, true, true);
                Program.ServerInstance.SendNativeCallToPlayer(player, 0xBF0FD6E56C964FCB, new LocalPlayerArgument(), unchecked((int)0xBFEFFF6D), -1, true, true);
            });
            nt.Start();
            return true;
        }
        public bool SetSkin(Client player, string selectedModel)
        {
            Program.ServerInstance.GetNativeCallFromPlayer(player, "GET_HASH_KEY", 0xD24D37CC275948CC, new IntArgument(),
            delegate(object o)
            {
                try
                {
                    var model = unchecked((int)o);
                    Program.ServerInstance.SetNativeCallOnTickForPlayer(player, "RACE_REQUEST_MODEL", 0x963D27A58DF860AC, model);
                    Thread.Sleep(1000);
                    Program.ServerInstance.RecallNativeCallOnTickForPlayer(player, "RACE_REQUEST_MODEL");
                    Program.ServerInstance.SendNativeCallToPlayer(player, 0x00A1CADD00108836, 0, unchecked((int)model)); //SET_PLAYER_MODEL
                    Program.ServerInstance.SendNativeCallToPlayer(player, 0x45EEE61580806D63, new LocalPlayerArgument()); //SET_PED_DEFAULT_COMPONENT_VARIATION
                    Program.ServerInstance.SendNativeCallToPlayer(player, 0xE532F5D78798DAAB, unchecked((int)model)); //Set model as not used anymore
                }
                catch { }
            }, selectedModel);
            return true;
        }
        public override bool OnPlayerDisconnect(Client player)
        {
            GPlayer curOp = GPlayers.FirstOrDefault(op => op.Client == player);
            if (curOp == null) return true;
            if (curOp.Blip != 0)
            {
                Program.ServerInstance.SendNativeCallToPlayer(player, 0x45FF974EEE1C8734, curOp.Blip, 0);
            }

            //Re Enable Patches
            for (int x = 0; x < 5; x++)
            {
                Program.ServerInstance.SendNativeCallToPlayer(player, 0xC8535819C450EBA8, x, true); //Enable hospital restart / spawn
            }
            Program.ServerInstance.SendNativeCallToPlayer(player, 0x5262CC1995D07E09, false); //Enable loading screen (SET_NO_LOADING_SCREEN)
            Program.ServerInstance.SendNativeCallToPlayer(player, 0x2C2B3493FBF51C71, false); //DISABLE_AUTOMATIC_RESPAWN
            Program.ServerInstance.SendNativeCallToPlayer(player, 0xB128377056A54E2A, 0, true); //SET_PED_CAN_RAGDOLL

            try { GPlayers.Remove(curOp); }
            catch { }
            return true;
        }
        Vector3[] SpawnPositions = new [] { 
            new Vector3(-1036.612f, -2733.31f, 13.75665f), //Airport
            new Vector3(128.0869f,-1307.666f,29.19628f) //Club 
        };
        public static int MAX_TEAMS = 5;

        public enum TeamColors
        {
            COLOR_WHITE = 0,
            COLOR_RED = 1,
            COLOR_GREEN = 2,
            COLOR_BLUE = 3,
            COLOR_ORANGE = 17,
        }
        public enum TeamEnum
        {
            TEAM_TEAMID = 0,
            TEAM_POSX = 1,
            TEAM_POSY,
            TEAM_POSZ,
            TEAM_NAME,
            TEAM_SKINID,
            TEAM_SCORE,
            TEAM_COLOR,
        }

        enum TeamSkins
        {
            SKINS_CRIPS = 0,
            SKINS_BLOODS = 1,
            SKINS_COPS,
            SKINS_CIVILIAN,
            SKINS_THELOST,
            SKINS_MS13,
        };

        public int[,] SkinsArray = //These need to be ordered with the TeamSkins enum!!
        {
            {-1, -1, -1, -1, -1, 206, 397}, //Crips
            {-1, -1, -1, -1, -1, -1, 209 }, //Bloods
            {-1, -1, -1, -1, -1, -1, 238 }, //Cops
            { 345, 346, 25, 26, 27, 28, 29 }, //Civilian
            { 380, 381, 382, 344, -1, -1, -1 }, //The Lost
            { 385, 386, 353, 191, -1, -1, -1 } //MS13
        };

        public Object[,] TeamsArray = new Object[,] {
            {Teams.TEAM_BLOODS, 104.94f, -1946.092f, 22.80f, "Bloods", TeamSkins.SKINS_BLOODS, 0, TeamColors.COLOR_RED}, 
            {Teams.TEAM_CRIPS, -217.9195f, -1613.455f, 34.86932f, "Crips", TeamSkins.SKINS_CRIPS, 0, TeamColors.COLOR_BLUE},
            {Teams.TEAM_CIVILIAN, -1036.612f, -2733.31f, 13.75665f, "Civilians", TeamSkins.SKINS_CIVILIAN, 0, TeamColors.COLOR_GREEN},
            {Teams.TEAM_COPS, 357.6711f, -1582.419f, 29.2919f, "Cops", TeamSkins.SKINS_COPS, 0, TeamColors.COLOR_ORANGE},
            {Teams.TEAM_MS13, 328.4394f, -2022.034f, 22.49279f, "MS13", TeamSkins.SKINS_MS13, 0, TeamColors.COLOR_WHITE}
        };

        public int TeamGetSkin(Teams teamid, int ignoreskin = -1) {
            for (int i = 0; i < TeamsArray.GetLength(0); i++)
            {
                if ((Teams)TeamsArray[i, (int)TeamEnum.TEAM_TEAMID] == teamid)
                {
                    for (int j = 0; j < SkinsArray.GetLength(0); j++)
                    {
                        if (j == (int)TeamsArray[i, (int)TeamEnum.TEAM_SKINID])
                        {
                            for (int k = 0; k <= SkinsArray.GetUpperBound(1); k++ )
                            {
                                var skin = SkinsArray[j, k];
                                if (skin == -1 || skin == ignoreskin)
                                    continue;
                                return skin;
                            }
                        }
                    }
                }
            }
            return -1;
        }
        public override void OnPlayerKilled(Client sender)
        {
            GPlayer curOp = GPlayers.FirstOrDefault(op => op.Client == sender);
            if (curOp == null) return;
            curOp.Spawned = false;
            curOp.DeathTime = DateTime.Now;          
            return;
        }
        public int findTeamIndexByTeamID(Teams teamid)
        {
            for (int i = 0; i < TeamsArray.GetLength(0); i++) {
                if((Teams)TeamsArray[i, (int)TeamEnum.TEAM_TEAMID] == teamid) {
                    return i;
                }
            }
            return -1;
        }
        public override bool OnChatMessage(Client sender, string message)
        {
            if (message.StartsWith("/goto"))
            {
                var args = message.Split();

                if (args.Length <= 1)
                {
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "USAGE", "/goto [playername]");
                    return false;
                }
                Client target = null;
                lock (Program.ServerInstance.Clients) target = Program.ServerInstance.Clients.FirstOrDefault(c => c.DisplayName.ToLower().StartsWith(args[1].ToLower()));

                if (target == null)
                {
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "ERROR", "No such player found: " + args[1]);
                    return false;
                }

                Program.ServerInstance.GetPlayerPosition(target, o =>
                {
                    var newPos = (Vector3)o;
                    Program.ServerInstance.SetPlayerPosition(sender, newPos);
                });

                Program.ServerInstance.SendChatMessageToPlayer(sender, "USAGE", "/goto [playername]");
                return false;
            }
            else if (message.StartsWith("/spawncar"))
            {
                var args = message.Split();
                if (args.Length < 2)
                {
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "USAGE", "/spawncar [part of car name]");
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "Example", "/spawncar zentorno");
                    return false;
                }

                for(int x=0; x<vehModels.GetLength(0); x++) {
                        if (vehModels[x].ToLower().Contains(args[1]))
                        {
                            Program.ServerInstance.SendChatMessageToPlayer(sender, "INFO", string.Format("Your {0} is being spawned, please wait..", vehModels[x].ToLower()));
                            Program.ServerInstance.GetNativeCallFromPlayer(sender, "GET_HASH_KEY", 0xD24D37CC275948CC, new IntArgument(),
                            delegate(object o)
                            {
                                var carHash = (int)o;
                                var nt = new Thread((ThreadStart)delegate
                                {
                                    SetPlayerInVehicle(sender, carHash, sender.LastKnownPosition, 0.0f, false);
                                });
                                nt.Start();
                            }, vehModels[x]);
                            return false;
                        }
                }
                
            }
            else if (message.StartsWith("/enginemult"))
            {
                var args = message.Split();
                if (args.Length < 2)
                {
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "USAGE", "/enginemult [amount float]");
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "Example", "/enginemult 2.5");
                    return false;
                }

                Program.ServerInstance.GetNativeCallFromPlayer(sender, "GET_VEHICLE_PED_IS_IN", 0x9A9112A0FE9A4713, new IntArgument(),
                delegate(object vehicleid)
                {
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "INFO", string.Format("Beginning to set multiplier for car id: {0}...", (int)vehicleid));
                    float mult;
                    bool ret = float.TryParse(args[1], out mult);
                    if (ret == false)
                    {
                        Program.ServerInstance.SendChatMessageToPlayer(sender, "USAGE", "/enginemult [amount float]");
                        Program.ServerInstance.SendChatMessageToPlayer(sender, "Example", "/enginemult 2.5");
                        return;
                    }

                    var nt = new Thread((ThreadStart)delegate
                    {
                        Program.ServerInstance.SendChatMessageToPlayer(sender, "INFO", string.Format("Setting multiplier for car id: {0}...", (int)vehicleid));
                        Program.ServerInstance.SendNativeCallToPlayer(sender, 0x93A3996368C94158, (int)vehicleid, mult); //_SET_VEHICLE_ENGINE_POWER_MULTIPLIER
                    });
                    nt.Start();
                }, new LocalPlayerArgument(), false);
                return false;
            }
            else if (message.StartsWith("/fixcar"))
            {
                Program.ServerInstance.GetNativeCallFromPlayer(sender, "GET_VEHICLE_PED_IS_IN", 0x9A9112A0FE9A4713, new IntArgument(),
                delegate(object vehicleid)
                {
                    var nt = new Thread((ThreadStart)delegate
                    {
                        Program.ServerInstance.SendNativeCallToPlayer(sender, 0x115722B1B9C14C1C, (int)vehicleid); //SET_VEHICLE_FIXED
                        Program.ServerInstance.SendNativeCallToPlayer(sender, 0x953DA1E1B12C0491, (int)vehicleid); //SET_VEHICLE_DEFORMATION_FIXED
                    });
                    nt.Start();
                }, new LocalPlayerArgument(), false);
                return false;
            }
            else if (message.StartsWith("/msg"))
            {
                var args = message.Split();
                if (args.Length < 2)
                {
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "USAGE", "/msg [message]");
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "Example", "/msg hi");
                    return false;
                }
                var nt = new Thread((ThreadStart)delegate
                {
                    GPlayer curOp = GPlayers.FirstOrDefault(op => op.Client == sender);
                    if (curOp == null) return;
                    try
                    {
                        foreach (var player in GPlayers)
                        {
                            if (player.TeamID == curOp.TeamID)
                            {
                                int index = findTeamIndexByTeamID(player.TeamID);
                                if (index != -1)
                                {
                                    Program.ServerInstance.SendChatMessageToPlayer(player.Client, string.Format("({0}) {1}", Convert.ToString(TeamsArray[index, (int)TeamEnum.TEAM_NAME]), sender.Name), args[1]);
                                }
                            }
                        }
                    }
                    catch { }
                });
                nt.Start();
                return false;
            }
            else if (message.StartsWith("/pm"))
            {
                var args = message.Split();
                if (args.Length < 3)
                {
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "USAGE", "/pm [username] [message]");
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "Example", "/pm colistro123 hi");
                    return false;
                }
                Client target = null;
                lock (Program.ServerInstance.Clients) target = Program.ServerInstance.Clients.FirstOrDefault(c => c.DisplayName.ToLower().StartsWith(args[1].ToLower()));

                if (target == null)
                {
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "ERROR", "No such player found: " + args[1]);
                    return false;
                }
                Program.ServerInstance.SendChatMessageToPlayer(target, string.Format("(PM) {0}", sender.Name), args[2]);
                return false;
            }
            else if (message.StartsWith("/mypos"))
            {
                try
                {
                    Program.ServerInstance.SendChatMessageToPlayer(sender, String.Format("Your position is ~r~ {0}, {1}, {2}", sender.LastKnownPosition.X, sender.LastKnownPosition.Y, sender.LastKnownPosition.Z));
                }
                catch {}
                return false;
             }
            else if (message.StartsWith("/suicide"))
            {
                Program.ServerInstance.SetPlayerHealth(sender, -1);
                Program.ServerInstance.SendChatMessageToPlayer(sender, "You ~r~killed yourself!");
                return false;
            }
            else if (message.StartsWith("/setskinid")) {
                var args = message.Split();
                if (args.Length < 2)
                {
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "USAGE", "/setskinid [id]");
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "Example", "/setskinid 24");
                    return false;
                }
                uint skinid;
                if (!uint.TryParse(args[1], out skinid))
                {
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "USAGE", "/setskinid [id]");
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "Example", "/setskinid 24");
                    return false;
                }
                var nt = new Thread((ThreadStart)delegate
                {
                    if (skinid > pedModels.GetUpperBound(0) || skinid < pedModels.GetLowerBound(0))
                    {
                        Program.ServerInstance.SendChatMessageToPlayer(sender, "Error", string.Format("Invalid skin, pick a skin between {0} and {1}.", pedModels.GetLowerBound(0), pedModels.GetUpperBound(0)));
                        return;
                    }
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "Info", string.Format("Skin set to {0} with index {1}!", pedModels[skinid], skinid));
                    SetSkin(sender, pedModels[skinid]);
                });
                nt.Start();
                return false;
            }
            else if (message.StartsWith("/setskin"))
            {
                var args = message.Split();
                if (args.Length < 2)
                {
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "USAGE", "/setskin [part of skin name]");
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "Example", "/setskin franklin");
                    return false;
                }
                var nt = new Thread((ThreadStart)delegate
                {
                    for (int x = 0; x < pedModels.GetUpperBound(0); x++)
                    {
                        if (pedModels[x].Contains(args[1]))
                        {
                            SetSkin(sender, pedModels[x]);
                            Program.ServerInstance.SendChatMessageToPlayer(sender, "Info", string.Format("Skin set to {0} with index {1}!", pedModels[x], x));
                            return;
                        }
                    }
                });
                nt.Start();
                return false;
            }
            else if (message.StartsWith("/jointeam"))
            {
                var args = message.Split();

                if (args.Length < 2)
                {
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "USAGE", "/jointeam [teamname]");
                    string tmpstr = String.Empty;
                    for (int i = 0; i < TeamsArray.GetLength(0); i++)
                    {
                        tmpstr += i + 1 < TeamsArray.GetLength(0) ? Convert.ToString(TeamsArray[i, (int)TeamEnum.TEAM_NAME]) + ", " : Convert.ToString(TeamsArray[i, (int)TeamEnum.TEAM_NAME]) + ".";      
                    }
                    Program.ServerInstance.SendChatMessageToPlayer(sender, "Team Names", tmpstr);
                    return false;
                }
                //Program.ServerInstance.SendChatMessageToPlayer(sender, "Team Names", tmpstr);
                GPlayer curOp = GPlayers.FirstOrDefault(op => op.Client == sender);

                if (curOp != null)
                {
                    for(int x=0; x<TeamsArray.GetLength(0); x++) {
                        string curTeam = Convert.ToString(TeamsArray[x, (int)TeamEnum.TEAM_NAME]);
                        if (curTeam.ToLower().Contains(args[1]))
                        {
                            curOp.TeamID = (Teams)TeamsArray[x, (int)TeamEnum.TEAM_TEAMID];
                            Program.ServerInstance.SetPlayerHealth(sender, -1);
                            Program.ServerInstance.SendChatMessageToPlayer(sender, string.Format("You ~r~joined team ~r~{0}!", curTeam));
                            Program.ServerInstance.SendNotificationToAll(string.Format("Player {0} joined team ~r~{1}.", sender.DisplayName, curTeam));
                        }
                    }
                }
                return false;
            }
            else if (message.StartsWith("/help"))
            {
                Program.ServerInstance.SendChatMessageToPlayer(sender, "Player Commands", "/goto, /pm, /mypos, /suicide");
                Program.ServerInstance.SendChatMessageToPlayer(sender, "Vehicle Commands", "/spawncar, /fixcar, /enginemult");
                Program.ServerInstance.SendChatMessageToPlayer(sender, "Team Commands", "/jointeam, /msg");
                return false;
            }
            else if (message == "/q")
            {
                GPlayer curOp = GPlayers.FirstOrDefault(op => op.Client == sender);

                if (curOp != null)
                {
                    OnPlayerDisconnect(sender);
                }

                Program.ServerInstance.KickPlayer(sender, "requested");
                return false;
            }
            Turfs.TurfClass turf = new Turfs.TurfClass();
            turf.turfsOnPlayerCommand(sender, message);
            return true;
        }

        public override bool OnPlayerConnect(Client player)
        {
            //Program.ServerInstance.SetNativeCallOnTickForPlayer(player, "RACE_DISABLE_VEHICLE_EXIT", 0xFE99B66D079CF6BC, 0, 75, true);
            Program.ServerInstance.SendNotificationToPlayer(player, "~r~IMPORTANT~w~~n~" + "Quit the server using the ~h~/q~h~ command to remove the blip.");
            Program.ServerInstance.SendChatMessageToPlayer(player, "Welcome, use ~r~/goto [playername]~w~ to teleport to other players.");
            Program.ServerInstance.SendChatMessageToPlayer(player, "Use ~r~/help~w~ to see all available commands.");

            var newPos = SpawnPositions[new Random().Next(0, SpawnPositions.GetUpperBound(0))];
            Program.ServerInstance.SetPlayerPosition(player, newPos);

            //Patches
            for (int x = 0; x < 5; x++)
            {
                Program.ServerInstance.SendNativeCallToPlayer(player, 0xC8535819C450EBA8, x, false); //Disable hospital restart / spawn
            }
            Program.ServerInstance.SendNativeCallToPlayer(player, 0x5262CC1995D07E09, true); //Disable loading screen
            Program.ServerInstance.SendNativeCallToPlayer(player, 0x2C2B3493FBF51C71, true); //DISABLE_AUTOMATIC_RESPAWN
            
            GPlayers.Add(new GPlayer(player));
            //Everything gets done after the player is added to the list

            GPlayer inOp = GPlayers.FirstOrDefault(op => op.Client == player);

            lock (GPlayers)
            {
                if (inOp != null)
                {
                    Turfs.TurfClass turf = new Turfs.TurfClass();
                    inOp.TurfBlips = new Dictionary<int, int>();
                    turf.OnPlayerConnect(player);
                    Anticheat.AnticheatClass.SetPlayerHealthEx(player, Anticheat.AnticheatClass.MAX_HEALTH); //Set the health (server side)
                }
            }
            return true;
        }

        private Random randGen = new Random();

        private void SetPlayerInVehicle(Client player, int model, Vector3 pos, float heading, bool freeze)
        {
            Program.ServerInstance.SetNativeCallOnTickForPlayer(player, "RACE_REQUEST_MODEL", 0x963D27A58DF860AC, model);
            Thread.Sleep(1000);
            Program.ServerInstance.RecallNativeCallOnTickForPlayer(player, "RACE_REQUEST_MODEL");
            Program.ServerInstance.GetNativeCallFromPlayer(player, "spawn", 0xAF35D0D2583051B0, new IntArgument(),
                delegate(object o)
                {
                    Program.ServerInstance.SendNativeCallToPlayer(player, 0xF75B0D629E1C063D, new LocalPlayerArgument(), (int)o, -1);
                    if (freeze)
                        Program.ServerInstance.SendNativeCallToPlayer(player, 0x428CA6DBD1094446, (int)o, true);

                    GPlayer inOp = GPlayers.FirstOrDefault(op => op.Client == player);

                    lock (GPlayers)
                    {
                        if (inOp != null)
                        {
                            inOp.Vehicle = (int)o;
                            inOp.HasStarted = true;
                        }
                        else
                            GPlayers.Add(new GPlayer(player) { Vehicle = (int)o, HasStarted = true });
                    }

                    Program.ServerInstance.SendNativeCallToPlayer(player, 0xE532F5D78798DAAB, model);
                }, model, pos.X, pos.Y, pos.Z, heading, false, false);
        }
    }

    public static class RangeExtension
    {
        public static bool IsInRangeOf(this Vector3 center, Vector3 dest, float radius)
        {
            return center.Subtract(dest).Length() < radius;
        }

        public static Vector3 Subtract(this Vector3 left, Vector3 right)
        {
            return new Vector3()
            {
                X = left.X - right.X,
                Y = left.Y - right.Y,
                Z = left.Z - right.Z,
            };
        }

        public static float Length(this Vector3 vect)
        {
            return (float) Math.Sqrt((vect.X*vect.X) + (vect.Y*vect.Y) + (vect.Z*vect.Z));
        }

        public static Vector3 Normalize(this Vector3 vect)
        {
            float length = vect.Length();
            if (length == 0) return vect;

            float num = 1/length;

            return new Vector3()
            {
                X = vect.X * num,
                Y = vect.Y * num,
                Z = vect.Z * num,
            };
        }
    }
}

