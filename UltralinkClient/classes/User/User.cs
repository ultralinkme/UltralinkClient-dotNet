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
    public class User : ULBase
    {
        public string access_token = "";

        public string ID;
        public string email;

        protected Dictionary<string, Achievement> achievements = new Dictionary<string, Achievement>();
        protected List<Database>                  databases    = new List<Database>();
        protected Dictionary<string, int?>        dbAuth       = new Dictionary<string, int?>();

        protected static string anonymousUser = "(anonymous)";

        public static Dictionary<string, User>    usersID = new Dictionary<string, User>();
        public static Dictionary<string, User> usersEmail = new Dictionary<string, User>();

        public object this[string name]
        {
            get
            { // Debug.WriteLine("GET - " + name + "\n");
                if( ID != "-1" ){ populateDetails(); return objectStorage[name]; }
                return null;
            }
            set
            { // Debug.WriteLine("SET - " + name + " - " + value + "\n");
                if( ID != "-1" ){ populateDetails(); objectStorage[name] = value; }
            }
        }

        protected void populateDetails(){ base.populateDetails("0.9.1/user/" + ID); }

        public override void populateDetail( string key, object value )
        {
            switch( key )
            {
                case "billingInfo":
                case "paymentAllocation":
                case "grants":
                case "settings":
                {
                         if( value == null   ){ this[key] = new JObject();                }
                    else if( value is string ){ this[key] = JObject.Parse((string)value); }
                                          else{ this[key] = value;                        }
                }
                break;

                default: { this[key] = value; } break;
            }
        }

        public object getDetail( string key )
        {
            switch( key )
            {
                case "billingInfo":
                case "paymentAllocation":
                case "grants":
                case "settings":
                {
                    return JObject.Parse( (string)this[key] );
                }

                default: { return this[key]; }
            }
        }

        /* GROUP(Class Functions) Returns a list of all the users in the system. */
        public static JArray allUsers()
        {
            JArray users = (JArray)Master.cMaster.APICall("0.9.1/user");
            if(users != null ){ return users; }else { UltralinkAPI.commandResult(500, "Could not retrieve the accounts"); }

            return null;
        }

        /* GROUP(Class Functions) text(A search string.) minAuth(An integer for the minimum auth level.) Returns a list of users based on a search string and minimum authorization level. */
        public static JArray accountSuggestion( string text, int minAuth )
        {
            JArray suggestions = (JArray)Master.cMaster.APICall("0.9.1/user", new JObject { ["accountSuggestion"] = text, ["minAuth"] = minAuth } );
            if( suggestions != null ){ return suggestions; }else{ UltralinkAPI.commandResult(500, "Couldn't lookup account suggestion for " + text + " and minAuth " + minAuth); }

            return null;
        }

        /* GROUP(Class Functions) identifier(<user identifier>) at(A master access_token.) Loads the user identified by <b>identifier</b>. */
        public static User U( string identifier = "", string at = "" )
        {
            if( (identifier == "") || (identifier == "0") || (identifier == null) || (identifier == "undefined") ){ identifier = "0"; }

            if( usersID.ContainsKey(identifier) ){ return usersID[identifier]; }
            else
            {
                if( usersEmail.ContainsKey(identifier) ){ return usersEmail[identifier]; }
                else
                {
                    User user = new User();

                    if( at != "" ){ user.access_token = at; }
                    user.loadByIdentifer(identifier);
                    User.enterUser( user );

                    return user;
                }
            }
        }

        protected static User UWithIDEmail( string ID, string email )
        {
            User existingUser = User.usersID[ID];
            if (existingUser != null) { return existingUser; }
            else
            {
                existingUser = User.usersEmail[email];
                if (existingUser != null) { return existingUser; }
                else
                {
                    User user = new User();

                    user.loadByIDEmail(ID, email);
                    User.enterUser(user);

                    return user;
                }
            }
        }

        protected static User enterUser( User user )
        {
            User.usersID[user.ID]       = user;
            User.usersEmail[user.email] = user;

            return user;
        }

        protected User loadByIdentifer( string identifier = "" )
        {
            int val;

            if( (identifier == "") || (identifier == "0") || (identifier == "undefined") )
            {
                ID    = "0";
                email = User.anonymousUser;
            }
            else if( Int32.TryParse(identifier, out val) )
            {
                ID = identifier;

                JValue theEmail = (JValue)Master.cMaster.APICall("0.9.1/user/" + ID, "email", access_token);
                if(theEmail != null )
                {
                    email = theEmail.ToString();
                }
                else{ UltralinkAPI.commandResult(500, "Could not lookup user " + identifier); }
            }
            else
            {
                email = identifier;

                JValue theID = (JValue)Master.cMaster.APICall("0.9.1/user/" + email, "ID", access_token);
                if(theID != null )
                {
                    ID = theID.ToString();
                }
                else{ UltralinkAPI.commandResult(500, "Could not lookup user " + identifier); }
            }

            return this;
        }

        protected User loadByIDEmail( string theID, string theEmail )
        {
            if( theID == "0" ){ ID = "0";   email = User.anonymousUser; }
                          else{ ID = theID; email = theEmail;           }
            return this;
        }

        /* GROUP(Class Functions) identifier(<user identifier>) Sets the current user to the one identified by <b>identifier</b>. */
        public static User currentU( string identifier = "" ){ User theUser = User.U(identifier); if( theUser != null ){ theUser.setCurrent(); return theUser; } return null; }

        /* GROUP(Information) Returns a human-readable description string about this User. */
        public string description(){ return email + " [" + ID + "]"; }

        /* GROUP(Information) Returns the URL to this user's image. */
        public string image() { return APICall("image", "Could not retrieve the user image").ToString(); }

        /* GROUP(Information) Returns this user's edit count for today. */
        public int todaysEditCount() { return Int32.Parse(APICall("todaysEditCount", "Failed to get todaysEditCount").ToString()); }

        /* GROUP(Information) todaysCount(Positive integer.) Returns whether <b>todaysCount</b> is under this user's daily edit limit. */
        public bool underEditLimit( int todaysCount = 0 ){ if( todaysCount == 0 ){ todaysCount = todaysEditCount(); } if( todaysCount < Int32.Parse((string)this["dailyEditLimit"]) ){ return true; } return false; }

        /* GROUP(Information) Returns this user's set of applications. */
        public JArray applications(){ return (JArray)APICallSub("/applications", "", "Could not retrieve applications"); }

        /* GROUP(Auth) Sets this user's authorization level up a notch. */
        public bool promote(){ if (APICall("promote", "Could not promote user " + description()) != null) { return true; } return false; }

        /* GROUP(Auth) Sets this user's authorization level down a notch. */
        public bool demote(){ if (APICall("demote", "Could not demote user " + description()) != null) { return true; } return false; }

        /* GROUP(Auth) db(Database) Returns whether this user has any authorization level on database <b>db</b>. */
        protected bool getDBAuth( Database db )
        {
            if( ID == "0" ){ return false; }
            else
            {
                if( db.ID == "0" )
                {
                    dbAuth[db.ID] = Int32.Parse((string)this["mainlineAuth"]);
                    return true;
                }
                else
                {
                    dbAuth[db.ID] = Int32.Parse( APICall(new JObject{ ["authForDB"] = db.ID }, "Could not retrieve the db " + db.description() + " auth").ToString() );
                    return true;
                }
            }
        }

        /* GROUP(Auth) db(Database) Returns the authorization level this user has on database <b>db</b>. */
        public int authForDB( object theDB )
        {
            Database db;

            if( theDB is string ){ db = Database.DB((string)theDB); }
                             else{ db = (Database)theDB;            }
            
            if( !dbAuth.ContainsKey(db.ID) ){ if( !getDBAuth( db ) ){ return 0; } }
            return (int)dbAuth[db.ID];
        }

        /* GROUP(Auth) Returns the authorization level for this user on the default database. */
        public int authForDefaultDB(){ Database db = Database.DB((string)this["defaultDatabase"]); if( db != null ){ return authForDB( db ); } return 0; }

        /* GROUP(Auth) db(Database) auth(A auth level integer.) Sets the authorization level for this user to <b>auth</b> on database <b>db</b>. */
        public bool setAuthForDB( Database db, int auth ){ if (APICall(new JObject {["setAuthForDB"] = db.ID,["auth"] = auth }, "Couldn't set user auth to " + auth + " for " + db.description()) != null) { return true; } return false; }

        /* GROUP(Notifications) Returns all the current notifications for this user. */
        public JArray notifications(){ return (JArray)APICallSub("/notifications", "", "Could not get notifications"); }

        /* GROUP(Notifications) nID(<notification identifier>) Returns the notification for the given ID for this user. */
        public JObject getNotification( int nID ){ return (JObject)APICallSub("/notifications/" + nID, "", "Could not get notification " + nID); }

        /* GROUP(Notifications) nID(<notification identifier>) Dismisses the notification for the given ID for this user. */
        public bool dismissNotification( int nID ){ if (APICallSub("/notifications/" + nID, "dismiss", "Could not dismiss notification" + nID) != null) { return true; } return false; }

        /* GROUP(Achievements) type(achievement type.) Returns whether this user has unlocked an achievement of <b>type</b>. */
        public bool hasAchievement(string type) { if( ID != "0" ){ if( getAchievement(type).isUnlocked() == 0 ){ return false; } return true; } return false; }

        /* GROUP(Achievements) type(achievement type.) Returns the achievement of <b>type</b> for this user. */
        public Achievement getAchievement( string type ){ if( ID != "0" ){ if( achievements[type] != null){ achievements[type] = Achievement.A( type, this); } return achievements[type]; } return null; }

        /* GROUP(Achievements) type(achievement type.) Returns the status for the achievement of <b>type</b> for this user. */
        public int getAchievementStatus( string type ){ if( ID != "0" ){ return getAchievement( type ).status(); } return 0; }

        /* GROUP(Achievements) Returns an array containing all the achievements for this user. */
        public JObject getAllAchievements()
        {
            if( ID != "0" )
            {
                JObject theAchievements = new JObject();

                foreach( Achievement theA in Achievement.allAchievementsForUser( this ) )
                {
                    string theUnlocked = ""; if( theA.unlocked == "1" ){ theUnlocked = theA.time; }

                    theAchievements[theA.type] = new JObject{ ["progress"] = theA.progress, ["unlocked"] = theUnlocked };
                }

                return theAchievements;
            }

            return null;
        }

        /* GROUP(Actions) Set this user to be the current user. */
        public void setCurrent(){ User.cUser = this; }

        /* GROUP(Actions) Returns an array containing information on all the databases this user has permissions to. */
        public JArray getDatabases(){ return (JArray)APICall("databases", "Could not get databases for " + description()); }

        /* GROUP(Actions) nuDefault(<database identifier>) Sets the default database for this user to <b>nuDefault</b>. */
        public bool setDefaultDatabase( string nuDefault ){ if (APICall(new JObject {["setDefaultDatabase"] = nuDefault }, "Attempted to change the defaultDatabase to " + nuDefault) != null) { return true; } return false; }

        /* GROUP(Actions) update(A JSON object with the new user info.) Updates this use accounts name and/or description ID. */
        public bool update( string update ){ if (APICall(new JObject {["update"] = update }, "Could not update information for " + description()) != null) { return true; } return false; }

        /* GROUP(Actions) deviceID(Device ID string.) type(Device type string.) Register's the device identifed by <b>deviceID</b> of <b>type</b> to this user. */
        public bool registerDevice( string deviceID, string type ){ if (APICallSub("/notifications", new JObject {["registerDevice"] = deviceID,["type"] = type }, "Could not register " + type + " device " + deviceID + " for " + description()) != null) { return true; } return false; }

        /* GROUP(Jobs) theDB(Database) Returns all this user's jobs. */
        public JArray jobs( Database theDB ){ return (JArray)APICallSub("/jobs/" + theDB.ID, "", "Could not lookup jobs on " + theDB.description()); }

        /* GROUP(Jobs) theDB(Database) Returns the count of all unfinished jobs for this user. */
        public int jobsCount( Database theDB ){ return ((JValue)APICallSub("/jobs/" + theDB.ID, "count", "Could not lookup job count on " + theDB.description() + " for " + description())).ToObject<int>(); }

        /* GROUP(Jobs) theDB(Database) Returns a list of the potential jobs this user can be assigned to. */
        public JArray potentialJobs( Database theDB ){ return (JArray)APICallSub("/jobs/" + theDB.ID, "potentialJobs", "Could not lookup potential jobs on " + theDB.description() + " for " + description()); }

        public object APICall( object fields, string error ){ return APICallSub("", fields, error); }
        public object APICallSub( string sub, object fields, string error )
        {
            object result = Master.cMaster.APICall("0.9.1/user/" + this.ID + sub, fields);
            if (result != null) { return result; }else{ UltralinkAPI.commandResult( 500, error ); }

            return null;
        }

        public static User cUser = U();
    }
}
