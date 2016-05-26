// Copyright © 2016 Ultralink Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UL
{
    public class Achievement
    {
        public User user;
        public string type;
        public string time;
        public string unlocked;
        public string progress;

        /* GROUP(Class Functions) user(User) Returns all the achievements for <b>user</b>. */
        public static List<Achievement> allAchievementsForUser(User user)
        {
            List<Achievement> theAchievements = new List<Achievement>();

            JArray achievements = (JArray)Master.cMaster.APICall("0.9.1/user/" + user.ID, "achievements");
            if(achievements != null )
            {
                foreach( JObject achievement in achievements ){ theAchievements.Add( AWithRow( achievement, user ) ); }
            }
            else{ UltralinkAPI.commandResult(500, "Could not get achievements for " + user.description()); }

            return theAchievements;
        }

        /* GROUP(Class Functions) type(An Achievement type string.) user(User) Returns the achievement of <b>type</b> for <b>user</b>. */
        public static Achievement A( string type, User user )
        {
            Achievement achievement = new Achievement();

            achievement.user = user;
            achievement.type = type;

            JObject cheevo = (JObject)Master.cMaster.APICall("0.9.1/user/" + user.ID, new JObject{ ["achievement"] = type } );
            if( cheevo != null )
            {
                achievement.time     = cheevo["unixTime"].ToString();
                achievement.progress = cheevo["progress"].ToString();
                achievement.unlocked = cheevo["unlocked"].ToString();

                return achievement;
            }

            achievement.time     = "0";
            achievement.progress = "0";
            achievement.unlocked = "0";

            return achievement;
        }

        public static Achievement AWithRow( JObject row, User user )
        {
            Achievement achievement = new Achievement();

            achievement.user     = user;
            achievement.type     = row["achievement"].ToString();
            achievement.time     = row["unixTime"].ToString();
            achievement.progress = row["progress"].ToString();
            achievement.unlocked = row["unlocked"].ToString();

            return achievement;
        }

        /* GROUP(Status) Returns the progress of the achievment if set and the unlocked status otherwise. */
        public int status()
        {
            int statusValue = 0;

            if( progress != "" ){ statusValue = Int32.Parse(progress); }
            if( unlocked != "" ){ if( statusValue == 0 ){ statusValue = 1; } }

            return statusValue;
        }

        /* GROUP(Status) Returns the value needed for unlocking. */
        public int unlockRequirement()
        {
            int requiredForUnlock = 1;
            
            switch( type )
            {
                case "imauser"         : { requiredForUnlock = 10; } break;
                
                case "everyonesacritic":
                case "outofcontext":
                case "knowledgeseeker" :
                case "linkedlist"      :
                case "wordsmith"       :
                case "librarian"       :
                case "completionist"   : { requiredForUnlock =  5; } break;
            }

            return requiredForUnlock;
        }

        /* GROUP(Status) Returns whether the achievement has been unlocked or not. */
        public int isUnlocked()
        {
            if( status() == unlockRequirement() ){ return 1; }
            
            return 0;
        }

    }
}
