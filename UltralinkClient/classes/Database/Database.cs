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
    public class Database : ULBase
    {
        public string ID;
        public string name;

        public static Dictionary<string, Database> dbsID   = new Dictionary<string, Database>();
        public static Dictionary<string, Database> dbsName = new Dictionary<string, Database>();

        public Dictionary<string, string>urlIDs = new Dictionary<string, string>();

        public object this[string name]
        {
            get
            { // Debug.WriteLine("GET - " + name + "\n");
                if( ID != "-1" ){ populateDetails(); return objectStorage[name]; }
                return null;
            }
            set
            { // Debug.WriteLine("SET - " + name + " - " + value + "\n");
                if (this.ID != "-1") { populateDetails(); objectStorage[name] = value; }
            }
        }

        public override void populateDetail(string key, object value)
        {
            switch( key )
            {
                case "affiliateKeys":
                {
                         if( value == null   ){ this[key] = new JObject();                }
                    else if( value is string ){ this[key] = JObject.Parse((string)value); }
                                          else{ this[key] = value;                        }
                } break;
 
                default: { this[key] = value; } break;
            }
        }

        public object getDetail(string key)
        {
            switch( key )
            {
                case "affiliateKeys":
                {
                    return JObject.Parse( (string)this[key] );
                }
   
                default: { return this[key]; }
            }
        }

        /* GROUP(On Disk Databases) Returns the ID and name of every database in the Master. */
        public static List<Database> all()
        {
            JArray dbs = (JArray)Master.cMaster.APICall("0.9.1/db");

            if( dbs != null )
            {
                List<Database> result = new List<Database>();
                foreach( JToken db in dbs ){ result.Add( Database.DBWithIDName(db["ID"].ToString(), db["name"].ToString()) ); }
                return result;
            }
            else{ UltralinkAPI.commandResult(500, "Could not lookup databases"); }

            return null;
        }

        /* GROUP(On Disk Databases) name(A database name string.) Examines a newly proposed database name to see if it fits the criteria for database names: <ul><li>Cannot start with a number.</li><li>Must be 16 characters or less.</li><li>Must contain only lower case alphanumeric characters.</li></ul>  If <b>name</b> passes these tests and there is not already an existing database with this name, then it will return with success to indicate availability. */
        public static bool nameAvailable( string name )
        {
            Database.all();

            if (!(name.PregReplace(new[]{ "/[^0-9]+/" }, new[]{ "" }) == name) )
            {
                if ((name.Length <= 16) && (name.Length > 0))
                {
                    if( name.PregReplace(new []{ "/[^a-z0-9]+/" }, new[]{ "" }) == name )
                    {
                        Database theDB = DB(name);
                        if( theDB != null )
                        {
                            return true;
                        }
                        else { UltralinkAPI.commandResult(403, "Database " + name + " already exists."); }
                    }
                    else{ UltralinkAPI.commandResult(400, "Database name (" + name + ") must be lower case alphanumeric."); }
                }
                else { UltralinkAPI.commandResult(400, "Database name (" + name + ") cannot be longer than 16 characters."); }
            }
            else{ UltralinkAPI.commandResult(400, "Database name (" + name + ") cannot be a number alone."); }

            return false;
        }

        /* GROUP(On Disk Databases) name(A database name string.) Creates a new database with <b>name</b>. */
        public static JObject create( string name ){ return (JObject)APICallUp(new JObject{ ["create"] = name }, "Could not create database " + name); }

        /* GROUP(Database Listings) identifier(<database identifier>) Returns whether <b>identifer</b> is a string that legitimately identifies the Mainline database. */
        protected static bool mainlineIdentifier( string identifier )
        {
            if( (identifier == ""                          ) ||
                (identifier == "0"                         ) ||
                (identifier == Master.cMaster.masterDomain ) ||
                (identifier == null                        ) ||
                (identifier == "undefined"                 ) ||
                (identifier == "Mainline"                  ) )
            {
                return true;
            }

            return false;
        }

        /* GROUP(Database Listings) db(Database) Enters <b>db</b> into the database cache. */
        protected static Database enterDB( Database db )
        {
            dbsID[db.ID]     = db;
            dbsName[db.name] = db;

            return db;
        }

        protected static Database DBWithIDName( string ID, string name )
        {
                 if( dbsID[ID]     != null ){ return dbsID[ID];     }
            else if( dbsName[name] != null ){ return dbsName[name]; }
            else
            {
                Database db = new Database();
                db.loadByIDName( ID, name );

                return enterDB( db );
            }
        }

        /* GROUP(Database Listings) identifier(<database identifier>) Returns the database associated with <b>identifier</b>. */
        public static Database DB( string identifier = "" )
        {
            if( mainlineIdentifier( identifier ) ){ identifier = "0"; }

                 if( dbsID.ContainsKey(identifier)   ){ return dbsID[identifier];   }
            else if( dbsName.ContainsKey(identifier) ){ return dbsName[identifier]; }
            else
            {
                Database db = new Database();
                db.loadByIdentifer( identifier );
                return enterDB( db );
            }
        }

        /* GROUP(Database Listings) identifier(<database identifier>) Loads the database information for <b>indentifier</b>. */
        protected Database loadByIdentifer( string identifier = "")
        {
            int val;

            if( mainlineIdentifier( identifier ) )
            {
                ID   = "0";
                name = "Mainline";
            }
            else if( Int32.TryParse(identifier, out val) )
            {
                string theName = nameForDBID( identifier );

                if( theName != "" )
                {
                    ID   = identifier;
                    name = theName;
                }
                else{ return null; }
            }
            else
            {
                var db_ID = IDForDBName( identifier );

                if( db_ID != "-1" )
                {
                    ID   = db_ID;
                    name = identifier;
                }
                else{ return null; }
            }

            return this;
        }

        /* GROUP(Database Listings) ID(A database ID.) name(A database name.) Sets the information for the database from <b>ID</b> and <b>name</b>. */
        protected void loadByIDName( string theID, string theName )
        {
            if( theID == "0" ){ ID = "0";   name = "Mainline"; }
                          else{ ID = theID; name = theName;    }
        }

        /* GROUP(Working Databases) identifier(<database identifier>) Sets the current database to the one identified by <b>identifier</b>. */
        public static Database currentDB( string identifier = ""){ Database theDB = DB( identifier ); if( theDB != null ){ theDB.setCurrent(); return theDB; } return null; }

        /* GROUP(Working Databases) Sets this database to be the current database. */
        public void setCurrent(){ cDB = this; }

        /* GROUP(Information) db_ID(A database ID.) Returns the name of the database with the ID <b>db_ID</b>. */
        public static string nameForDBID( string db_ID ){ return APICallUp(new JObject{ ["nameForDBID"] = db_ID }, "Could not lookup database with ID " + db_ID).ToString(); }

        /* GROUP(Information) name(A database name.) Returns the ID of the database with the name <b>name</b>. */
        public static string IDForDBName( string name ){ return APICallUp(new JObject{ ["IDForDBName"] = name }, "Could not lookup database with name " + name).ToString(); }

        /* GROUP(Information) Returns a string describing this database. */
        public string description(){ return name + " [" + ID + "]"; }

        /* GROUP(Information) Returns a URL postfix string for use in identifying this database. */
        public string postfix(){ string dbPostfix = ""; if( ID != "0" ){ dbPostfix = name; } if( dbPostfix != "" ){ dbPostfix = "/" + dbPostfix; } return dbPostfix; }

        /* GROUP(Information) Returns the number of ultralinks currently in this database. */
        public int ultralinkCount() { return Int32.Parse(APICall("ultralinkCount", "Could not retrieve the ultralink count").ToString()); }

        /* GROUP(Information) Returns the remote roots that this Database has associated with it. */
        public JArray remoteRoots(){ return (JArray)APICall( "remoteRoots", "Failed to get the remote roots for " + description() ); }

        /* GROUP(Vanity Names) name(A vanity name string.) Returns the ultralink ID for a given vanity name or 0 if it does not exist. */
        public string lookupVanityName( string name ){ return APICall(new JObject{ ["lookupVanityName"] = name }, "Failed to get the vanity ID for " + name).ToString(); }

        /* GROUP(Vanity Names) ulID(An Ultralink ID.) Returns the vanity string for an ultralink or a null string if it does not exist. */
        public string lookupVanityDescription( string ulID ){ return APICall(new JObject{ ["lookupVanityDescription"] = ulID }, "Failed to get the vanity name for " + ulID).ToString(); }

        /* GROUP(Modification) sourceDatabase(Database) Changes the source database. A value of -1 indicates that there is no source database. */
        public bool changeSourceDatabase( string theSourceDatabase )
        {
            if( (theSourceDatabase != "-1") && (DB(theSourceDatabase) == null) ){ UltralinkAPI.commandResult(401, "Database " + theSourceDatabase + " does not exist"); }
            this["sourceDatabase"] = theSourceDatabase;

            if( APICall(new JObject { "sourceDatabase", theSourceDatabase }, "Could not change the source database for " + description()) != null ){ return true; } return false;
        }

        /* GROUP(Modification) update(An object with new affiliate key settings.) Sets the affilliate keys for this database to the given values. */
        public bool updateAffiliateKeys( JObject update )
        {
            bool changed = false; foreach( var x in update ){ string service = x.Key; string key = x.Value.ToString(); if( key != ((JObject)this["affiliateKeys"])[service].ToString() ){ changed = true; } ((JObject)this["affiliateKeys"])[service] = key; }

            if( changed == true )
            {
                if( APICall(new JObject { "updateAffiliateKeys", JsonConvert.SerializeObject(this["affiliateKeys"]) }, "Could not update the affiliate keys for " + description()) != null ){ changed = true; }
            }

            return changed;
        }

        /* GROUP(Modification) Destroys the database and all related data. */
        public bool nuke(){ if (APICall("nuke", "Could not nuke database " + description()) != null) { return true; } return false; }

        /* GROUP(Actions) initialCategory(Category string. Optionally set if you are creating a new Ultralink) needsReview(Review status number. 0 means no review needed)Creates a new ultrailnk in this database with the initial state optionally based on <b>initialCategory</b> and <b>needsReview</b>. */
        public Ultralink createUltralink( string initialCategory = "", int needsReview = 0 ){ return Ultralink.U("-1", this, initialCategory, needsReview); }

        /* GROUP(Actions) bannedWord(A word string.) Inserts a word into the autogenerated 'banned' table. */
        public bool banWord( string bannedWord ){ if (APICall(new JObject { ["banWord"] = bannedWord }, "Could not insert banned autogenerated word " + bannedWord) != null) { return true; } return false; }

        /* GROUP(Actions) newc(A content fragment.) contentURL(A URL string.) affiliateOverrides(An array of affiliate overrides.) Filters fragment content through this database. */
        public JObject UltralinksInContent( string newc, string contentURL, JObject affiliateOverrides = null ){ string ao = "true"; if( affiliateOverrides != null ){ ao = JsonConvert.SerializeObject(affiliateOverrides); } return (JObject)APICall(new JObject { ["UltralinksInContent"] = newc, ["contentURL"] = contentURL, ["affiliateOverrides"] = ao }, "Could not get the Ultralinks in content at " + contentURL); }

        /* GROUP(Content) unfilteredContent(A content fragment.) contentURL(A URL string.) contentTitle(The title of the URL.) hyperlinks(An array of hyperlinks present in the fragment.) Filters fragment content through this master (meaning through any source database and then this database). */
        public JObject ULFilterContent( string unfilteredContent, string contentURL, string contentTitle, JArray hyperlinks ){ return (JObject)APICall(new JObject{ ["ULFilterContent"] = unfilteredContent, ["contentURL"] = contentURL, ["contentTitle"] = contentTitle, ["hyperlinks"] = hyperlinks }, "Could not filter the content at " + contentURL); }

        /* GROUP(Authorization) Returns the auth level for this database for the current user. */
        public int auth(){ return authForUser( User.cUser); }
        /* GROUP(Authorization) user(User) Returns the auth level for this database for <b>user</b>. */
        public int authForUser( User user ){ return user.authForDB( this ); }

        /* GROUP(Websites) website(A website URL.) Returns the ID, time and blacklisted status for the given website. */
        public JObject websiteInfo( string website ){ return (JObject)APICall(new JObject{ ["websiteInfo"] = website }, "Couldn't get " + website + " information"); }

        /* GROUP(Websites) website(A website ID.) Returns the ID for <b>website</b> or 0 if it doesn't exist. */
        public string websiteID( string website ){ return APICall(new JObject{ ["website_ID"] = website }, "Could not lookup website " + website + " in " + description()).ToString(); }

        /* GROUP(Pages) URL(A page URL.) websiteID(A website ID.) title(The title of the page.) Returns information on the page at <b>URL</b> on <b>websiteID</b>. */
        public JObject pageInfo( string URL, string websiteID, string title ){ return (JObject)APICall(new JObject{ ["pageInfo"] = URL, ["websiteID"] = websiteID, ["title"] = title }, "Couldn't get " + URL + " information"); }

        /* GROUP(Categories) category(A category string.) Gets a list of all the subcategories under the given category string as well as the number of subcategories and ultralinks. Ordered by the count of ultralinks in the category descending. */
        public JArray categoryTree( string category ){ return (JArray)APICall(new JObject{ ["categoryTree"] = category }, "Couldn't get the category tree for " + category); }

        /* GROUP(Categories) existingCategory(A category string.) newCategory(A category string.) Modifies all applicable ultralinks to change an existing category to a given new category. */
        public bool changeCategory( string existingCategory, string newCategory ){ if (APICall(new JObject {["changeCategory"] = existingCategory,["newCategory"] = newCategory }, "Couldn't change existing category " + existingCategory + " to " + newCategory) != null) { return true; } return false; }

        /* GROUP(URLs) url_ID(A URL ID.) Returns the URL for the link associated with the ID <b>url_ID</b>. */
        public string getURL( string url_ID ){ return APICall(new JObject{ "url_ID", url_ID }, "Could not lookup the URL for url ID " + url_ID).ToString(); }

        /* GROUP(URLs) url(A URL string.) Returns the ID for <b>url</b>. */
        public string getURLID( string url )
        {
            if( urlIDs[url] != null ){ return urlIDs[url]; }
            else
            {
                string theURLID = APICall(new JObject{ ["url"] = url }, "Could not lookup the URL ID for url " + url).ToString();
                urlIDs[url] = theURLID;
                return theURLID;
            }
        }

        /* GROUP(Queries) URL(A URL string.) trimset(A character to trim on.) Returns the first ultralink that has <b>URL</b> attached to it. */
        public Ultralink ulFromURL( string URL, string trimset = "" ){ return Ultralink.U( APICall(new JObject{ ["ulFromURL"] = URL, ["trimset"] = trimset }, "Could not lookup the ultralink for URL " + URL).ToString() ); }

        /* GROUP(Queries) word(A word string.) case(Indicates whether the search should be case sensitive.) category(A category string.) recent(Boolean. If true, then restrict search to recent ultralinks.) Returns the first ultralink that has <b>word</b> attached to it. You can optionally use <b>case</b>, <b>category</b> and <b>recent</b> to further narrow down your results. */
        public Ultralink ulFromWord( string word, bool caseSensitive = false, string category = "", bool recent = false){ return Ultralink.U( APICall(new JObject{ ["ulFromWord"] = word, ["case"] = caseSensitive, ["category"] = category, ["recent"] = recent }, "Could not lookup the ultralink for word " + word).ToString() ); }

        /* GROUP(Queries) connection(A Connection type string.) offset(Pagination offset.) limit(Pagination limit.) Returns a set of ultralinks that have a connection string that begins with $connection ordered by primary instance count descending. */
        public JArray connectionUltralinks( string connection, int offset = 0, int limit = 100 ){ return (JArray)APICall(new JObject{ ["connectionUltralinks"] = connection, ["offset"] = offset, ["limit"] = limit }, "Could not lookup ultralinks for connection " + connection ); }

        /* GROUP(Queries) category(A category string.) offset(Pagination offset.) limit(Pagination limit.) Returns a set of ultralinks that have a category string that begins with $category ordered by primary instance count descending. */
        public JArray categoryUltralinks( string category, int offset = 0, int limit = 100 ){ return (JArray)APICall(new JObject { ["categoryUltralinks"] = category, ["offset"] = offset, ["limit"] = limit }, "Could not lookup ultralinks for category " + category ); }

        /* GROUP(Queries) query(A search string.) wordSearch(Search for <b>query</b> in words.) categorySearch(Search for <b>query</b> in categories.) exact(Boolean. If true the match cannot be a substring.) sortType(What way to sort the results.) includePages(Boolean. If true, include the pages that the Ultralink is on.) offset(Pagination offset.) limit(Pagination limit.) Returns a set of ultralinks based on a query string and various search attributes. Searches can examine ultralink words and category strings or both. Matches can be required to be exact or not. Sorting can be by instance count, exact matching, alphabetical word order, word length or alphabetical category order. Results can optinally include information on what pages the ultralinks resides as well. Results are paged at 100 results by default. */
        public JArray search( string query, bool wordSearch = true, bool categorySearch = true, bool exact = false, string sortType = "instanceCount", bool includePages = false, int offset = 0, int limit = 100 ){ return (JArray)APICallSub( "/ul", new JObject { ["search"] = query, ["wordSearch"] = wordSearch, ["categorySearch"] = categorySearch, ["sortType"] = sortType, ["exact"] = exact, ["includePages"] = includePages, ["offset"] = offset, ["limit"] = limit }, "Could not perform ultralink search" ); }

        /* GROUP(Queries) likeString(A LIKE string to match the URL against.) type(The like type to match against.) language(A langauge bias if any.) country(A country bias if any.) primaryLink(Indication if the link should be the primary one or not.) Returns the Ultralink IDs that have links that fit the given criteria. */
        public JArray linkQuery( string likeString = "", string type = "", string language = "", string country = "", string primaryLink = "" ){ return (JArray)APICallSub( "/ul", new JObject { ["linkQuery"] = likeString, ["type"] = type, ["language"] = language, ["country"] = country, ["primaryLink"] = primaryLink }, "Could not run the link query" ); }

        /* GROUP(Queries) word(A word string.) Returns the most recently modified ultrailnk within the last day that has the given word attached to it. */
        public Ultralink recentUltralink( string word ){ Ultralink ul = ulFromWord( word, false, "", true); if( ul != null ){ return ul; }else{ UltralinkAPI.commandResult( 404, "Could not find recent ultralink for " + word ); } return null; }

        /* PRIVATE GROUP(Queries) Returns the examination status for a given image or 'false' if it does not exist. */
        public string imageExaminationStatus( string image ){ return APICall(new JObject{ ["imageExaminationStatus"] = image }, "Could not look up image status for " + image ).ToString(); }

        /* GROUP(Jobs) creationParameters(A JSON object describing the Job.) Creates a new job based on the creation parameters passed in. */
        public JObject createJob( JObject creationParameters ){ return (JObject)APICallSub( "/jobs", new JObject { ["create"] = creationParameters }, "Could not create new job with query " + creationParameters["query"] + " and operation " + creationParameters["operation"] ); }

        /* GROUP(Jobs) Returns a complete set of all the job queries and operations currently defined in this database. */
        public JArray getJobQueriesOperations(){ return (JArray)APICallSub( "/jobs", "tools", "Couldn't get the queries and operations." ); }

        /* GROUP(Jobs) Returns a list of information on every job defined. */
        public JArray getJobsInfo(){ return (JArray)APICallSub( "/jobs", "", "Couldn't lookup the job list" ); }

        /* GROUP(Hardcoded) Returns a list of all the websites currently with custom url classifiers or selector overrides alone with the website ID and URL. */
        public JArray customized(){ return (JArray)APICall( "customized", "Could not retrieve the website customizations" ); }

        /* GROUP(Hardcoded) websiteID(A website ID.) overrides() Sets a given website's selector overrides to the set passed in. */
        public bool saveOverrides( string websiteID, JArray overrides ){ if (APICall(new JObject {["saveOverrides"] = overrides,["websiteID"] = websiteID }, "Could not save the website overrides") != null) { return true; } return false; }

        /* GROUP(Hardcoded) websiteID(A website ID.) classifiers() Sets a given website's url classifiers to the set passed in. */
        public bool saveClassifiers( string websiteID, JArray classifiers ){ if (APICall(new JObject {["saveClassifiers"] = classifiers,["websiteID"] = websiteID }, "Could not save the url classifiers") != null) { return true; } return false; }

        /* GROUP(Analytics) dataHash() timeScale(The time scale of the data we are looking at. Values can be <b>monthly</b>, <b>daily</b> or <b>hourly</b>.) timeDuration(The numeric length of the time slice that the data should examine in units defined by <b>timeScale</b>.) analyticsBreakdown(The type of analytics data that should be returned.) numericType(The type of numeric data that should be returned. Values can be <b>absolute</b> or <b>percent</b>.) statusType(A user status type to filter the data though. Values can be <b>active</b>, <b>enabled</b> or <b>installed</b>.) osType(A OS type to filter the data though. Values can be <b>all</b>, <b>mac</b>, <b>windows</b>, <b>linux</b>, <b>android</b> or <b>unknown</b>.) browserType(A browser to filter the data though. Values can be all, <b>safari</b>, <b>chrome</b>, <b>firefox</b>, <b>opera</b> or <b>operanext</b>.) authType(A user authentication status to filter the data though. Values can be <b>all</b>, <b>authenticated</b> or <b>anonymous</b>.) actionType(A action to filter the data though. Values can be <b>all</b>, <b>blackshadowauto</b>, <b>blackshadow</b> or <b>blueshadow</b>.) linkType(A link type to filter the data through. Valuse can be <b>all</b> or any valid link type in this database.) Returns a set of chart data based on the parameters passed in. Can leverage the analyticsCache with a resultant data hash. If the data has is identical to the one in the cache then it will block and wait on one of the analytics queues so that web services can long poll on this. */
        public JObject historicalAnalytics( string dataHash, string timeScale, string timeDuration, string analyticsBreakdown, string numericType, string statusType, string osType, string browserType, string authType, string actionType, string linkType ){ return (JObject)APICall(new JObject { ["historicalAnalytics"] = dataHash, ["timeScale"] = timeScale, ["timeDuration"] = timeDuration, ["analyticsBreakdown"] = analyticsBreakdown, ["numericType"] = numericType, ["statusType"] = statusType, ["osType"] = osType, ["browserType"] = browserType, ["authType"] = authType, ["actionType"] = actionType, ["linkType"] = linkType }, "Could not get the historical analytics" ); }

        /* GROUP(Analytics) association(An association identifier.) startDate(A starting time to the session as a UNIX timestamp.) endDate(An ending time to the session as a UNIX timestamp.) eventNum(The number of events expected in the session as given by <b>associationSessions</b>.) associationType(	The type of the above identifier if given.) dataHash(A resultant data hash so as to leverage the analytics cache if possible.) Returns a set of events for an association identifer of a given type from a start date to an end date with a known number of events. Based on these paramters and a hash of the resultant data it wil try to lean on the analyticsCache if possible. */
        public JArray associationHistory( string association, string startDate, string endDate, int eventNum, string associationType, string dataHash ){ return (JArray)APICall(new JObject { ["associationHistory"] = association, ["startDate"] = startDate, ["endDate"] = endDate, ["eventNum"] = eventNum, ["associationType"] = associationType, ["dataHash"] = dataHash }, "Could not get the association history" ); }

        /* GROUP(Analytics) associationType(The type of the above identifier if given.) association(An association identifier.) Returns a list of activity sessions for a given association type and identifier. Sessions are defined as activity clusters at least 60 minutes apart from each other. */
        public JArray associationSessions( string associationType, string association ){ return (JArray)APICall(new JObject { ["associationSessions"] = association, ["associationType"] = associationType }, "Could not get the association sessions" ); }

        /* GROUP(Analytics) pagePath(A URL fragment that defines the scope of desired data.) contentsType(Indicates what kind of content data is desired. Values can be <b>catpresent</b>, <b>catclicked</b>, <b>ulpresent</b> or <b>ulclicked</b>.) timeRestrict(Determines if the results should be restricted in any way. Values can be <b>cache</b> or <b>alltime</b>.) restrictToThis(A boolean indicating that results should only match the exact URL of <b>pagePath</b>.) timeScale(The time scale of the data we are looking at. Values can be <b>monthly</b>, <b>daily</b> or <b>hourly</b>.) timeDuration(The numeric length of the time slice that the data should examine in units defined by <b>timeScale</b>.) offset(Pagination offset.) limit(Pagination limit. Max <b>100</b>.) Gets ultralink content and interaction information within a given time range at a given URL path fragment which can also be the value "all". You can specify what kind of ultralink content you want and select prescence of ultralinks, categories or click data on both those as well. Can restrict some configurations to only look at data connected to what is in the current content cache through resultRestrict. Can also restrict the results to an exact URL path fragment match instead of including everything under it as well. Can be paged through an offset and limit. */
        public JArray myContents( string pagePath, string contentsType, string timeRestrict, string restrictToThis, string timeScale, string timeDuration, int offset = 0, int limit = 10 ){ return (JArray)APICall(new JObject { { "myContents", pagePath }, { "contentsType", contentsType }, { "resultRestrict", timeRestrict }, { "restrictToThis", restrictToThis }, { "timeScale", timeScale }, { "timeDuration", timeDuration }, { "offset", offset }, { "limit", limit } }, "Could not get contents analytics" ); }

        /* GROUP(Analytics) pagePath(A URL fragment that defines the scope of desired data.) orderBy(How to structure and order the results. Values can be <b>usage</b>, <b>hosted</b>, <b>pages</b> or <b>clicks</b>.) timeRestrict(Determines if the results should be restricted in any way. Values can be <b>cache</b> or <b>alltime</b>.) timeScale(The time scale of the data we are looking at. Values can be <b>monthly</b>, <b>daily</b> or <b>hourly</b>.) timeDuration(The numeric length of the time slice that the data should examine in units defined by <b>timeScale</b>.) search(A search string to restrict the entires under <b>pagePath</b> can match against.) offset(Pagination offset.) limit(Pagination limit. Max <b>100</b>.) Gets a statistical look within a time period at a given URL Path fragment which can also be "". Results can be ordered and organized by usage frequency, whether or not the ultralinks are hosted natively, number of pages or numbers of clicks. Can restrict some configurations to only look at data connected to what is in the current content cache. Can also restrict results to be limited to a search as we well. Can be paged by a given offset and limit. */
        public JArray myWebsites( string pagePath, string orderBy, string timeRestrict, string timeScale, string timeDuration, string search = "", int offset = 0, int limit = 10 ){ return (JArray)APICall(new JObject { ["myWebsites"] = pagePath, ["orderBy"] = orderBy, ["resultRestrict"] = timeRestrict, ["timeScale"] = timeScale, ["timeDuration"] = timeDuration, ["search"] = search, ["offset"] = offset, ["limit"] = limit }, "Could not get website analytics" ); }

        /* GROUP(Holding Tank) Returns the entries currently in the holding tank. */
        public JArray holdingTank(){ return (JArray)APICall( "holdingTank", "Could note retrieve the web holding tank rows" ); }

        /* GROUP(Holding Tank) Returns the number of items currently in the holding tank. */
        public int holdingTankCount(){ return ((JValue)APICall( "holdingTankCount", "Could not lookup web holding tank count" )).ToObject<int>(); }

        /* GROUP(Holding Tank) category(A category string.) URL(The URL for an initial link to be attached.) type(The link type of above URL.) word(A an initial word to be attached.) caseSensitive(Case-sensitivty of the above word. 1 for case-sensitive, 0 otherwise.) Submits information about a proposed ultralink into the holding tank for review. */
        public bool submitUltralink( string category, string URL, string type, string word, string caseSensitive = "false" ){ if (APICall(new JObject {["submitUltralink"] = category,["URL"] = URL,["urlType"] = type,["word"] = word,["caseSensitive"] = caseSensitive }, "Could not submit the ultralink") != null) { return true; } return false; }

        /* GROUP(Holding Tank) resolution(The descision string whether to <b>accept</b> or <b>reject</b> the submitted Ultralink.) category(A category string.) URL(The URL for an initial link to be attached.) urlType(The link type of above URL.) word(A an initial word to be attached.) contributor(<user identifier>) caseSensitive(Case-sensitivty of the above word. 1 for case-sensitive, 0 otherwise.) Removes the submission entry for the new ultralink. If the resolution is 'accept' then it creates the suggested ultralink. */
        public Ultralink resolveNewUltralink( string resolution, string category, string URL, string urlType, string word, string contributor, string caseSensitive = "false" ){ return Ultralink.U( APICall(new JObject { ["resolveNewUltralink"] = resolution, ["contributor"] = contributor, ["category"] = category, ["URL"] = URL, ["urlType"] = urlType, ["word"] = word, ["caseSensitive"] = caseSensitive }, "Could not resolve the submitted ultralink" ).ToString(), this ); }

        public object APICallSub( string sub, object fields, string error )
        {
            object result = Master.cMaster.APICall("0.9.1/db/" + ID + sub, fields);
            if( result != null ){ return result; }else{ UltralinkAPI.commandResult( 500, error ); }

            return null;
        }

        public object APICall( object fields, string error ){ return APICallSub( "", fields, error ); }

        public static object APICallUp( object fields, string error )
        {
            object result = Master.cMaster.APICall("0.9.1/db", fields);
            if( result != null ){ return result; }else{ UltralinkAPI.commandResult( 500, error ); }

            return null;
        }

        public static Database cDB = DB();
    }
}
