// Copyright © 2016 Ultralink Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UL
{
    public class Ultralink
    {
        protected Dictionary<string, object> objectStorage = new Dictionary<string, object>();

        private Dictionary<string, object> preload = new Dictionary<string, object>();
        private Dictionary<string, bool?> fieldLocks = new Dictionary<string, bool?>();

        public Database db;
        public long ID;

        private bool dirtyNeedsReview = false;

        public object this[string name]
        {
            get { populateDetails(name); return objectStorage[name]; }
            set { populateDetails(name); objectStorage[name] = value; }
        }

        /* GROUP(Class Functions) ID(<ultralink identifier> or -1 which indicates a new Ultralink) db(Database, <database identifier> or "" which indicates cDB) initialCategory(Category string. Optionally set if you are creating a new Ultralink) needsReview(Review status number. 0 means no review needed) Produces an ultralink object for the parameters specified.<br><br>Call with the <b>ID</b> parameter (and <b>db</b> if you need to) to simply get an object representing an Ultralink.<br><br>Call with no parameters to create a new Ultralink in cDB (can fill in <b>initialCategory</b> or <b>needsReview</b> if desired).*/
        public static Ultralink U( object ID = null, object db = null, string initialCategory = "", int needsReview = 0 )
        {
            if( ID == null ){ ID = -1L; }

            Ultralink u = new Ultralink();

            if( db == null ){ u.db = Database.cDB; }
            else
            {
                     if( db is string   ){ u.db = Database.DB((string)db); }
                else if( db is Database ){ u.db = (Database)db;            }
            }

            if( ID is string )
            {
                long val;
                if( long.TryParse((string)ID, out val) ){ ID = val; }
                else{ ID = long.Parse(u.APICall(new JObject { ["lookupVanityName"] = (string)ID }, "Could not lookup vanity name for " + ID).ToString()); };
            }
            else if( ID is int )
            {
                ID = Convert.ToInt64( ID );
            }

            u.ID = (long)ID;

            if( needsReview     != 0  ){ u.setNeedsReview(needsReview);          }
            if( initialCategory != "" ){ u.setCategory(initialCategory, "true"); }

            return u;
        }

        /* GROUP(Class Functions) ID(<ultralink identifier>) db(Database, <database identifier> or "" which indicates cDB) Immediately checks for the existance of the Ultralink specified by <b>ID</b> and returns it. Errors out if it does not exist. */
        public static Ultralink existingU(string ID, object db = null)
        {
            Ultralink ul = Ultralink.U(ID, db);
            if (!ul.doesExist()) { UltralinkAPI.commandResult(404, ul.description() + " does not exist."); }
            return ul;
        }

        /*public void __destruct()
        {
            if (property_exists( this, "categories"))
            {
                if( categoriesDead != null ){ unset( categoriesDead ); }
                if( categories     != null ){ unset( categories );     }
            }

            if (property_exists( this, "words"))
            {
                if( wordsDead != null ){ unset( wordsDead ); }
                if( words     != null ){ unset( words );     }
            }

            if (property_exists( this, "links"))
            {
                if( linksDead != null ){ unset( linksDead ); }
                if( links     != null ){ unset( links );     }
            }

            if (property_exists( this, "connections"))
            {
                if( connectionsDead != null ){ unset( connectionsDead ); }
                if( connections     != null ){ unset( connections );     }
            }

            if (property_exists( this, "pageFeedback"))
            {
                if( pageFeedbackDead != null ){ unset( pageFeedbackDead ); }
                if( pageFeedback     != null ){ unset( pageFeedback );     }
            }

            if( db != null ){ unset( db ); }
        }*/

        protected object populateDetails(string fieldName)
        {
            fieldName = fieldName.Replace("Dead", "");

            if( !fieldLocks.ContainsKey(fieldName) )
            {
                fieldLocks[fieldName] = true;

                switch (fieldName)
                {
                    case "words":
                        {
                            if( ID == -1 ){ this["words"] = new List<Word>(); } else { if( preload.ContainsKey("words") ) { this["words"] = preload["words"]; preload.Remove("words"); } else { this["words"] = Word.getWords(this); } }
                            this["wordsDead"] = new List<Word>();

                            return this["words"];
                        }

                    case "categories":
                        {
                            if( ID == -1 ){ this["categories"] = new List<Category>(); } else { if( preload.ContainsKey("categories") ) { this["categories"] = preload["categories"]; preload.Remove("categories"); } else { this["categories"] = Category.getCategories(this); } }
                            this["categoriesDead"] = new List<Category>();

                            return this["categories"];
                        }

                    case "links":
                        {
                            if( ID == -1 ){ this["links"] = new List<Link>(); } else { if( preload.ContainsKey("links") ) { this["links"] = preload["links"]; preload.Remove("links"); } else { this["links"] = Link.getLinks(this); } }
                            this["linksDead"] = new List<Link>();

                            return this["links"];
                        }

                    case "connections":
                        {
                            this["connections"] = new Dictionary<string, Connection>();
                            if( ID != -1 ){ if( preload.ContainsKey("connections") ){ this["connections"] = preload["connections"]; preload.Remove("connections"); } else { foreach (Connection connection in Connection.getConnections(this)) { ((Dictionary<string, Connection>)this["connections"])[connection.hashString()] = connection; } } }
                            this["connectionsDead"] = new Dictionary<string, Connection>();

                            return this["connections"];
                        }

                    case "pageFeedback":
                        {
                            if( ID == -1 ){ this["pageFeedback"] = new List<PageFeedback>(); } else { if( preload.ContainsKey("pageFeedback") ){ this["pageFeedback"] = preload["pageFeedback"]; preload.Remove("pageFeedback"); } else { this["pageFeedback"] = PageFeedback.getPageFeedback(this); } }
                            this["pageFeedbackDead"] = new List<PageFeedback>();

                            return this["pageFeedback"];
                        }

                    case "time":
                        {
                            if( ID == -1 ){ this["time"] = 0; }
                            else
                            {
                                if( preload.ContainsKey("time") ){ this["time"] = preload["time"]; preload.Remove("time"); }
                                else { this["time"] = APICall("time", "Could not lookup time for " + description()).ToString(); }
                            }

                            return this["time"];
                        }

                    case "needsReview":
                        {
                            if( ID == -1 ){ this["needsReview"] = 0; }
                            else
                            {
                                if( preload.ContainsKey("needsReview") ){ this["needsReview"] = preload["needsReview"]; preload.Remove("needsReview"); }
                                else { this["needsReview"] = ((JValue)APICall("needsReview", "Could not lookup needsReview for " + description())).ToObject<int>(); }
                            }

                            return this["needsReview"];
                        }
                }
            }

            return null;
        }

        /* GROUP(Information) Returns whether this ultralink exists in the master's storage. */
        public bool doesExist()
        {
            if( ID != -1 )
            {
                JObject ulObject = (JObject)APICall("", "Could not test for existence for " + description());

                List<Word> theWords = new List<Word>(); if (ulObject["words"] != null) { foreach (JObject word in ulObject["words"]) { theWords.Add(Word.wordFromObject(this, word)); } }
                List<Category> theCategories = new List<Category>(); if (ulObject["category"] != null) { theCategories.Add(Category.categoryFromObject(this, new JObject { ["category"] = ulObject["category"], ["primaryCategory"] = "1" })); }
                if (ulObject["extraCategories"] != null) { foreach (JValue category in ulObject["extraCategories"]) { theCategories.Add(Category.categoryFromObject(this, new JObject { ["category"] = category.ToString(), ["primaryCategory"] = "0" })); } }
                List<Link> theLinks = new List<Link>(); if (ulObject["urls"] != null) { foreach (var x in (JObject)ulObject["urls"]) { string type = x.Key; foreach (JObject link in ulObject["urls"][type]) { link["type"] = type; theLinks.Add(Link.linkFromObject(this, link)); } } }
                List<Connection> theConnections = new List<Connection>(); if (ulObject["connections"] != null) { foreach (JObject connection in ulObject["connections"]) { theConnections.Add(Connection.connectionFromObject(this, connection)); } }
                List<PageFeedback> thePageFeedback = new List<PageFeedback>(); if (ulObject["pageFeedback"] != null) { foreach (JObject pf in ulObject["pageFeedback"]) { thePageFeedback.Add(PageFeedback.pageFeedbackFromObject(this, pf)); } }

                preload["words"]        = theWords;
                preload["categories"]   = theCategories;
                preload["links"]        = theLinks;
                preload["connections"]  = theConnections;
                preload["pageFeedback"] = thePageFeedback;

                preload["time"]        = ulObject["time"];
                preload["needsReview"] = ulObject["needsReview"];

                return true;
            }

            return false;
        }

        /* GROUP(Information) Returns a string that can be used to identify this ultralink. */
        public string indicatorString() { return db.ID + "." + ID; }

        /* GROUP(Information) Returns a string describing this ultralink. */
        public string description() { return "Ultralink " + db.name + "/" + ID; }

        /* PRIVATE GROUP(Representations) toArray(Array to write the preview info to.) reducedFormat(Boolean. If true, writes back preview info in a reduced format) Adds the preview information returned from previewInfo to the provided associative array. */
        public void addPreviewInfo(JObject toArray, bool reducedFormat = false)
        {
            string needsReviewSignifier = "needsReview";
            string categorySignifier    = "category";
            string primaryWordSignifier = "primaryWord";
            string imageSignifier       = "image";
            string metaInfoSignifier    = "metaInfo";

            if (reducedFormat)
            {
                needsReviewSignifier = "nr";
                categorySignifier    = "cat";
                primaryWordSignifier = "pri";
                imageSignifier       = "img";
                metaInfoSignifier    = "meta";
            }

            JObject pi = previewInfo(reducedFormat);

            if( pi[needsReviewSignifier] != null ){ toArray[needsReviewSignifier] = pi[needsReviewSignifier]; }
            //if( pi[primaryWordSignifier]            != null ){ toArray[primaryWordSignifier] = pi[primaryWordSignifier]; }
            if( pi[primaryWordSignifier].ToString() != "") { toArray[primaryWordSignifier] = pi[primaryWordSignifier]; }
            if( pi[categorySignifier]    != null ){ toArray[categorySignifier]    = pi[categorySignifier];    }
            if( pi[imageSignifier]       != null ){ toArray[imageSignifier]       = pi[imageSignifier];       }
            if( pi[metaInfoSignifier]    != null ){ toArray[metaInfoSignifier]    = pi[metaInfoSignifier];    }
        }

        /* GROUP(Representations) reducedFormat(Boolean. If true, returns preview info in a reduced format) Returns the set of information sufficient to presenting a small preview of an ultralink: primary word, primary category, primary image, primary image meta info and whether it needs review or not. */
        public JObject previewInfo(bool reducedFormat = false) { return (JObject)APICall(new JObject { ["previewInfo"] = reducedFormat }, "Could not get preview info for " + description()); }

        /* GROUP(Representations) Returns a string representing the status of this ultralink at this point in time. */
        public string statusRecord()
        {
            JObject record = objectify(true, true);
            record.Remove("time");
            return JsonConvert.SerializeObject(record);
        }

        /* GROUP(Representations) withPageFeedback(Boolean. True will add all page feedback for this Ultralink to the result.) addConnectionInfo(Boolean. True will add preview info to every Connection.) addAffiliateKeys(Boolean. True will add the Database affiliate keys to the result.) removeDefaultValues(Boolean. True will remove attributes that are set to the default values.) addPermissions(Boolean. True will add the relevant permission data for cUser on this Ultralink.) reducedFormat(Boolean. If true, returns the object in a reduced format) Returns a JSON string representation of this ultralink. */
        public string json(bool withPageFeedback = false, bool addConnectionInfo = false, bool addAffiliateKeys = false, bool addPermissions = false, bool removeDefaultValues = false, bool reducedFormat = false) { return JsonConvert.SerializeObject(objectify(withPageFeedback, addConnectionInfo, addAffiliateKeys, addPermissions, removeDefaultValues, reducedFormat)); }

        /* GROUP(Representations) withPageFeedback(Boolean. True will add all page feedback for this Ultralink to the result.) addConnectionInfo(Boolean. True will add preview info to every Connection.) addAffiliateKeys(Boolean. True will add the Database affiliate keys to the result.) removeDefaultValues(Boolean. True will remove attributes that are set to the default values.) addPermissions(Boolean. True will add the relevant permission data for cUser on this Ultralink.) reducedFormat(Boolean. If true, returns the object in a reduced format) Returns a serializable object representation of the ultralink. Parameters indicate what sets of additional information should be included and in what format.<br><br><b>addAffiliateKeys</b> and <b>addPermissions</b> are usually only used when visually editing the Ultralink.*/
        public JObject objectify(bool withPageFeedback = false, bool addConnectionInfo = false, bool addAffiliateKeys = false, bool addPermissions = false, bool removeDefaultValues = false, bool reducedFormat = false)
        {
            // First test to see if the ultralink is even there still
            if (!doesExist()) { return null; }

            string needsReviewSignifier = "needsReview";
            string categorySignifier    = "category";

            string wordSignifier                 = "word";
            string caseSensitiveSignifier        = "caseSensitive";
            string primarySignifier              = "primaryWord";
            string commonalityThresholdSignifier = "commonalityThreshold";

            string urlSignifier      = "URL";
            string languageSignifier = "language";
            string countrySignifier  = "country";
            string metaInfoSignifier = "metaInfo";

            string connectionSignifier = "connection";

            //string primaryWordSignifier = "primaryWord";
            string primaryLinkSignifier = "primaryLink";
            //string imageSignifier = "image";

            if (reducedFormat)
            {
                needsReviewSignifier = "nr";
                categorySignifier    = "cat";

                caseSensitiveSignifier        = "cs";
                primarySignifier              = "pri";
                commonalityThresholdSignifier = "ct";

                languageSignifier = "lang";
                countrySignifier  = "geo";
                metaInfoSignifier = "meta";

                connectionSignifier = "con";

                //primaryWordSignifier = "pri";
                primaryLinkSignifier = "pri";
                //imageSignifier = "img";
            }

            // Categories
            string thePrimaryCategoryString = "";
            Category thePrimaryCategory = getPrimaryCategory(); if (thePrimaryCategory != null) { thePrimaryCategoryString = thePrimaryCategory.categoryString; } else { thePrimaryCategoryString = Category.defaultCategory; }

            JObject fullResult = new JObject { ["ID"] = ID, ["time"] = this["time"].ToString(), [needsReviewSignifier] = ((JValue)this["needsReview"]).ToObject<int>(), [categorySignifier] = thePrimaryCategoryString };

            if (removeDefaultValues)
            {
                if (fullResult[needsReviewSignifier] != null) { fullResult.Remove(needsReviewSignifier); }
                if (fullResult[categorySignifier].ToString() == Category.defaultCategory) { fullResult.Remove(categorySignifier); }
            }

            List<string> fullExtraCategories = new List<string>();
            foreach (Category category in (List<Category>)this["categories"])
            {
                if (category.primaryCategory != null) { fullExtraCategories.Add(category.categoryString); }
            }
            if (!removeDefaultValues || (fullExtraCategories.Count > 0)) { fullResult["extraCategories"] = JsonConvert.SerializeObject(fullExtraCategories); }

            // Words
            List<JObject> fullWords = new List<JObject>();
            foreach( Word word in (List<Word>)this["words"] )
            {
                JObject theWord = new JObject { { wordSignifier, word.wordString }, { caseSensitiveSignifier, Int32.Parse(word.caseSensitive) }, { primarySignifier, Int32.Parse(word.primaryWord) }, { commonalityThresholdSignifier, Int32.Parse(word.commonalityThreshold) } };

                if (removeDefaultValues)
                {
                    if( theWord[caseSensitiveSignifier]        == null ){ theWord.Remove(caseSensitiveSignifier);        }
                    if( theWord[primarySignifier]              == null ){ theWord.Remove(primarySignifier);              }
                    if( theWord[commonalityThresholdSignifier] == null ){ theWord.Remove(commonalityThresholdSignifier); }
                }

                fullWords.Add(theWord);
            }
            if (!removeDefaultValues || (fullWords.Count > 0)) { fullResult["words"] = JsonConvert.SerializeObject(fullWords); }

            // Links
            Dictionary<string, JArray> fullURLs = new Dictionary<string, JArray>();
            foreach( Link link in (List<Link>)this["links"] )
            {
                if( !fullURLs.ContainsKey(link.type) ){ fullURLs[link.type] = new JArray(); }

                string theMetaInfo = JsonConvert.SerializeObject(link.metaInfo); if( theMetaInfo == "\"\"" ){ theMetaInfo = ""; }
                JObject theLink = new JObject{ { "ID", Int32.Parse(link.url_ID) }, { urlSignifier, link.url }, { languageSignifier, link.language }, { countrySignifier, link.country }, { primarySignifier, Int32.Parse(link.primaryLink) }, { metaInfoSignifier, theMetaInfo } };

                if( removeDefaultValues )
                {
                    if( theLink[languageSignifier]    == null ){ theLink.Remove(languageSignifier);    }
                    if( theLink[countrySignifier]     == null ){ theLink.Remove(countrySignifier);     }
                    if( theLink[primaryLinkSignifier] == null ){ theLink.Remove(primaryLinkSignifier); }
                    if( theLink[metaInfoSignifier]    == null ){ theLink.Remove(metaInfoSignifier);    }
                }

                fullURLs[link.type].Add(theLink);
            }
            if (!removeDefaultValues || (fullURLs.Count > 0) ){ fullResult["urls"] = JsonConvert.SerializeObject(fullURLs); }


            //Connections
            List<JObject> fullConnections = new List<JObject>();
            foreach( var x in (Dictionary<string,Connection>)this["connections"] )
            {
                Connection connection = x.Value;

                JObject theConnection = new JObject{ { "aID", connection.ulA.ID }, { connectionSignifier, connection.connection }, { "bID", connection.ulB.ID } };

                // Get useful preview data for visualizing the connections to this ultralink
                if( addConnectionInfo )
                {
                    connection.getOtherConnection( this ).addPreviewInfo( theConnection, reducedFormat);
                }

                if( removeDefaultValues )
                {
                    if( theConnection[connectionSignifier] != null ){ theConnection.Remove( connectionSignifier ); }
                }

                fullConnections.Add( theConnection );
            }
            if( !removeDefaultValues || (fullConnections.Count > 0) ){ fullResult["connections"] = JsonConvert.SerializeObject( fullConnections ); }

            // Page Feedback
            if( withPageFeedback )
            {
                List<JObject> fullPageFeedback = new List<JObject>();

                foreach( PageFeedback pf in (List<PageFeedback>)this["pageFeedback"] )
                {
                    fullPageFeedback.Add( new JObject{ ["page_ID"] = pf.page_ID, ["word"] = pf.word, ["feedback"] = pf.feedback } );
                }
                if( !removeDefaultValues || (fullPageFeedback.Count > 0) ){ fullResult["pageFeedback"] = JsonConvert.SerializeObject( fullPageFeedback ); }
            }

            if( addAffiliateKeys )
            {
                fullResult["amazonAffiliateTag"] = "";
                fullResult["linkshareID"]        = "";
                fullResult["phgID"]              = "";
                fullResult["eBayCampaign"]       = "";

                JObject affiliateKeys = (JObject)db["affiliateKeys"];

                if( affiliateKeys["amazonAffiliateTag"] == null ){ fullResult["amazonAffiliateTag"] = ""; }else{ fullResult["amazonAffiliateTag"] = affiliateKeys["amazonAffiliateTag"]; }
                if( affiliateKeys["linkshareID"]        == null ){ fullResult["linkshareID"]        = ""; }else{ fullResult["linkshareID"]        = affiliateKeys["linkshareID"];        }
                if( affiliateKeys["phgID"]              == null ){ fullResult["phgID"]              = ""; }else{ fullResult["phgID"]              = affiliateKeys["phgID"];              }
                if( affiliateKeys["eBayCampaign"]       == null ){ fullResult["eBayCampaign"]       = ""; }else{ fullResult["eBayCampaign"]       = affiliateKeys["eBayCampaign"];       }
            }

            if( addPermissions )
            {
                if ((User.cUser.ID != "0") && (db.ID == "0") && (Int32.Parse(User.cUser["mainlineAuth"].ToString()) == Auth.authLevels["Contributor"]))
                {
                    fullResult["grants"] = JsonConvert.SerializeObject( User.cUser["grants"] );

                    int currentDailyEditCount = User.cUser.todaysEditCount();

                    //fullResult["hasSufficientGrant"]    = User.cUser.hasSufficientGrantForUltralinks( ID );
                    fullResult["underEditLimit"]        = User.cUser.underEditLimit( currentDailyEditCount );
                    //fullResult["underImpactLimit"]      = User.cUser.underImpactLimit( ID );

                    fullResult["currentDailyEditCount"] = currentDailyEditCount;
                    fullResult["currentDailyEditLimit"] = Int32.Parse(User.cUser["dailyEditLimit"].ToString());
                }
                else
                {
                    fullResult["auth"] = User.cUser.authForDB( db );
                }
            }

            return fullResult;
        }

        /* GROUP(Analytics) Returns the number of websites this Ultralink is currently found on. */
        public int websiteCount(){ return ((JValue)APICall( "websiteCount", "Could not get the website count for " + description() )).ToObject<int>(); }

        /* GROUP(Analytics) Returns the number of pages this Ultralink is currently found on. */
        public int pageCount(){ return ((JValue)APICall( "pageCount", "Could not get the page count for " + description() )).ToObject<int>(); }

        /* GROUP(Analytics) Returns the number of instances this Ultralink are currently found. */
        public int instanceCount(){ return ((JValue)APICall( "instanceCount", "Could not get the instance count for " + description() )).ToObject<int>(); }

        /* GROUP(Analytics) Returns a list of users who have made manual contributions to this Ultralink. */
        public JArray contributors(){ return (JArray)APICall( "contributors", "Could not get the contributors for " + description() ); }

        /* GROUP(Analytics) Returns some statistical information about this Ultralink's occurrences. */
        public JArray stats(){ return (JArray)APICall( "stats", "Could not retrieve stats for " + description() ); }

        /* GROUP(Analytics) timeScale(The time scale of the data we are looking at. Values can be <b>monthly</b>, <b>daily</b> or <b>hourly</b>.) timeDuration(The numeric length of the time slice that the data should examine in units defined by <b>timeScale</b>.) Returns chart data for historical occurrence counts for a specified time period. */
        public JArray occurrences( string timeScale, string timeDuration ){ return (JArray)APICall( new JObject{ ["occurrences"] = "", ["timeScale"] = timeScale, ["timeDuration"] = timeDuration }, "Could not retrieve occurrences for " + description() ); }

        /* GROUP(Analytics) pagePath(A URL path fragment determing the scope of the results.) restrictToThis(Boolean. Indicates whether the results should be restricted to only the exact pagePath) timeRestrict(Determines if the results should be restricted in any way. Values can be cache or alltime.) timeScale(The time scale of the data we are looking at. Values can be <b>monthly</b>, <b>daily</b> or <b>hourly</b>.) timeDuration(The numeric length of the time slice that the data should examine in units defined by <b>timeScale</b>.) getAggregation(Boolean. Determines if the extra aggreggation information should be include) Returns a set of data outlining click activity for this ultralink in a specifc URL path fragment within a specific time span. Can set whether the results should be restricted to only data connected to what is in the current content cache. Can restrict the results to only the exact path instead of all the paths under it. Can also add specific data on aggreggation. */
        public JArray path( string pagePath, string restrictToThis, string timeRestrict, string timeScale, string timeDuration, string getAggregation ){ return (JArray)APICall( new JObject{ ["path"] = "", ["timeScale"] = timeScale, ["timeDuration"] = timeDuration, ["pagePath"] = pagePath, ["restrictToThis"] = restrictToThis, ["resultRestrict"] = timeRestrict, ["getAggregation"] = getAggregation }, "Could not retrieve path for " + description() ); }

        /* GROUP(Analytics) website_ID(A website ID.) offset(Pagination offset.) limit(Pagination limit. Default: <b>100</b>, Max: <b>1000</b>.) Returns a list of pages on a given website that this ultralink is known to be on. */
        public JArray instancePages( string website_ID, int offset = 0, int limit = 100 )
        {
            if( limit == 0 ){ limit = 100; } if( limit > 1000 ){ limit = 1000; }

            return (JArray)APICall( new JObject{ ["instancePages"] = website_ID, ["offset"] = offset, ["limit"] = limit }, "Could not get the instance pages for " + description() );
        }

        /* GROUP(Analytics) offset(Pagination offset.) limit(Pagination limit. Default: <b>100</b>, Max: <b>1000</b>.) Returns a list of websites that this ultralink is known to be on. */
        public JArray instanceWebsites( int offset = 0, int limit = 100 )
        {
            if( limit == 0 ){ limit = 100; } if( limit > 1000 ){ limit = 1000; }

            return (JArray)APICall( new JObject{ ["instanceWebsites"] = "", ["offset"] = offset, ["limit"] = limit }, "Could not get the instance websites for " + description() );
        }

        /* GROUP(Analytics) commons(An array of commonality description objects describing the calculations.) pullLinkType(A link type determining what link should be pulled out and included in the answer sets.) getIntersect(A boolean indicating if an intersection array of all the commonality sets should also be returned.) Returns a result set for each commonality description objects passed in. Returns resultant sets that include the desired link type. Can optionally return an intersection of the resultant sets as well. Resultant sets sorted by commonality value descending. */
        public JArray connectionCommon( JObject commons, string pullLinkType, string getIntersect = "false" ){ return (JArray)APICall( new JObject{ ["connectionCommon"] = commons, ["uID"] = ID, ["pullLinkType"] = pullLinkType, ["getIntersect"] = getIntersect }, "Could not get the connection common for " + description() ); }
        
        /* GROUP(Analytics) Returns a set of ultralinks that have a common word with this one. */
        public JArray wordCommon(){ return (JArray)APICall( "wordCommon", "Could not get common ultralinks for " + description() ); }

        /* GROUP(Analytics) Returns a top 20 list of ultralinks that appear in the same fragments as this one ordered by occurrence number descending. */
        public JArray related(){ return (JArray)APICall( "related", "Could not get related ultralinks for " + description() ); }

        /* GROUP(Actions) nr(Review status number. 0 means no review needed) Sets this ultralink"s needsReview value. */
        public void setNeedsReview( int nr = 0 ){ if( nr != (int)this["needsReview"] ){ dirtyNeedsReview = true; } this["needsReview"] = nr; }

        /* GROUP(Actions) modificationID(A determination status. Values can be <b>GOOD</b>, <b>BAD</b> and <b>REVERTED</b>.) determination() Sets the status of the specified modification and sets the state of the ultralink to what it was before the modification if the determination is "REVERTED". */
        public bool modificationDetermination( string modificationID, string determination ){ if (APICall(new JObject { { "modificationDetermination", modificationID }, { "determination", determination } }, "Could not set determination on " + description()) != null) { return true; } return false; }

        /* GROUP(Actions) destDB(<database identifier>) Creates a copy of this ultralink in another specified database. */
        public void copyIntoDB( object destDB )
        {
            Ultralink nuUL = Ultralink.U( "-1", destDB );

            foreach( Word word         in     (List<Word>)this["words"]      ){ nuUL.setWord( word.wordString, word.caseSensitive, word.primaryWord, word.commonalityThreshold ); }
            foreach( Category category in (List<Category>)this["categories"] ){ nuUL.setCategory( category.categoryString, category.primaryCategory ); }
            foreach( Link link         in     (List<Link>)this["links"]      ){ nuUL.setLink( link.url, link.type, link.language, link.country, link.primaryLink, link.metaInfo ); }

            nuUL.sync();
        }

        /* GROUP(Actions) mergeIDs(A JSON object listing the IDs of the ultralinks to merge into this one.) Merges all the given ultralinks into this one. */
        public bool merge( JArray mergeIDs ){ return (bool)APICall( new JObject{ "merge", mergeIDs }, "Could not perform merge into " + description() ); }

        /* GROUP(Actions) Removes everything from this ultralink. */
        public void blankSlate( bool pageFeedbackToo = false )
        {
            removeAllWords();
            removeAllCategories();
            removeAllLinks();
            removeAllConnections();

            if( pageFeedbackToo ){ removeAllPageFeedback(); }
        }

        /* GROUP(Actions) nuState(A JSON object representing the Ultralink state.) Sets the data for this ultralink from the information found in <b>nuState</b>. */
        public void setFromObject( JObject nuState )
        {
            if( nuState["time"] != null ){ this["time"] = nuState["time"]; }
            this["needsReview"] = nuState["needsReview"];

            blankSlate( true );

            // Words
            if( nuState["words"] != null ){ foreach( JObject word in nuState["words"] ){ setWordFromObject( word ); } }

            // Categories
            if( (string)nuState["category"] != Category.defaultCategory ){ setCategory( nuState["category"].ToString(), "true" ); }
            if( nuState["extraCategories"] != null ){ foreach( JObject category in nuState["extraCategories"] ){ setCategory( category.ToString() ); } }

            // Links
            if( nuState["urls"] != null ){ foreach( var x in (JObject)nuState["urls"] ){ string type = x.Key; JArray tlink = (JArray)x.Value; foreach( JObject link in tlink ){ link["type"] = type; setLinkFromObject( link ); } } }

            // Connections
            if( nuState["connections"] != null ){ foreach( JObject connection in nuState["connections"] ){ setConnectionFromObject( connection ); } }

            // Page Feedback
            if( nuState["pageFeedback"] != null ){ foreach( JObject pf in nuState["pageFeedback"] ){ setPageFeedbackFromObject( pf ); } }
        }

        /* GROUP(Actions) nuState(A JSON object representing the Ultralink state.) Sets the data for this ultralink from the information found in <b>nuState</b> and syncs the changes to disk. */
        public void syncFromObject( JObject nuState )
        {
            setFromObject( nuState );
            sync();
        }

        /* GROUP(Actions) Prints currently un-sync'd changes. */
        public void printCurrentModifications()
        {
            if( dirtyNeedsReview )
            {
                Debug.WriteLine( "\tDIFFERENCE: (" + ID + ") - needsReview " + this["needsReview"] );
            }

            if( this["categories"] != null )
            {
                foreach( Category theCategory in (List<Category>)this["categoriesDead"] ){ Debug.WriteLine( "  DIFFERENCE: (" + ID + ") - " + theCategory.description() + " present here but not in master" ); }
                foreach( Category theCategory in (List<Category>)this["categories"]     ){ if( theCategory.dirty ){ Debug.WriteLine( "  DIFFERENCE: (" + ID + ") - " + theCategory.description() + " not found" ); } }
            }

            if( this["words"] != null )
            {
                foreach( Word theWord in (List<Word>)this["wordsDead"] ){ Debug.WriteLine("  DIFFERENCE: (" + ID + ") - " + theWord.description() + " present here but not in master" ); }
                foreach( Word theWord in (List<Word>)this["words"]     ){ if( theWord.dirty ){ Debug.WriteLine( "  DIFFERENCE: (" + ID + ") - " + theWord.description() + " not found" ); } }
            }

            if( this["links"] != null )
            {
                foreach( Link theLink in (List<Link>)this["linksDead"] ){ Debug.WriteLine( "  DIFFERENCE: (" + ID + ") - " + theLink.description() + " present here but not in master" ); }
                foreach( Link theLink in (List<Link>)this["links"]     ){ if( theLink.dirty ){ Debug.WriteLine( "  DIFFERENCE: (" + ID + ") - " + theLink.description() + " not found" ); } }
            }

            if( this["connections"] != null )
            {
                foreach( var x in (Dictionary<string,Connection>)this["connectionsDead"] ){ Connection theConnection = x.Value; Debug.WriteLine( "  DIFFERENCE: (" + ID + ") - " + theConnection.description() + " present here but not in master" ); }
                foreach( var x in (Dictionary<string,Connection>)this["connections"]     ){ Connection theConnection = x.Value; if ( theConnection.dirty ){ Debug.WriteLine( "  DIFFERENCE: (" + ID + ") - " + theConnection.description() + " not found" ); } }
            }

            if( this["pageFeedback"] != null )
            {
                foreach( PageFeedback thePageFeedback in (List<PageFeedback>)this["pageFeedbackDead"] ){ Debug.WriteLine( "  DIFFERENCE: (" + ID + ") - " + thePageFeedback.description() + " present here but not in master" ); }
                foreach( PageFeedback thePageFeedback in (List<PageFeedback>)this["pageFeedback"]     ){ if( thePageFeedback.dirty ){ Debug.WriteLine( "  DIFFERENCE: (" + ID + ") - " + thePageFeedback.description() + " not found" ); } }
            }
        }

        /* GROUP(Actions) outputDifference(Boolean. If true, then echo the differences from the previous state.) Syncs the changes to this ultralink to disk in an efficient manner. */
        public int sync( bool outputDifference = false )
        {
            if( User.cUser.authForDB( db ) <= Auth.authLevels["Contributor"] )
            {
                JValue theID = (JValue)Master.cMaster.APICall("0.9.1/db/" + db.ID + "/ul/" + ID, new JObject { ["modify"] = json() });
                if( theID != null )
                {
                    ID = theID.ToObject<long>();
                }
                else{ UltralinkAPI.commandResult( 500, "Could not create a new ultralink in " + db.description() ); }
            }
            else
            {
                Dictionary<string,int> wordDifferential         = new Dictionary<string,int>();
                Dictionary<Int64, int> connectionDifferential   = new Dictionary<Int64,int>();
                Dictionary<string,int> pageFeedbackDifferential = new Dictionary<string,int>();

                bool needsReviewDifferent = false;
                bool categoriesDifferent  = false;
                bool urlsDifferent        = false;

                // Freshly created ultralink, here on this machine
                if( ID == -1 )
                {
                    string categoryString = Category.defaultCategory; Category primaryCategory = getPrimaryCategory(); if( primaryCategory != null ){ categoryString = primaryCategory.categoryString; }

                    long? theID = (long?)Master.cMaster.APICall("0.9.1/db/" + db.ID + "/ul", new JObject {["create"] = "",["category"] = categoryString,["needsReview"] = (int)this["needsReview"] });
                    if( theID != null )
                    {
                        ID = (long)theID;
                    }
                    else{ UltralinkAPI.commandResult( 500, "Could not create a new ultralink in " + db.description() ); }
                }

                if( dirtyNeedsReview )
                {
                    APICall( new JObject{ ["reviweStatus"] = (string)this["needsReview"] }, "Could not sync the state of needsReview on " + description() );

                    dirtyNeedsReview = false;
                    needsReviewDifferent = true;

                    if( outputDifference ){ Debug.WriteLine("\tDIFFERENCE: (" + ID + ") - needsReview " + this["needsReview"]); }
                }

                if( this["categories"] != null )
                {
                    foreach( Category theCategory in (List<Category>)this["categoriesDead"] ){ theCategory.nuke(); ((List<Category>)this["categoriesDead"]).Remove(theCategory); if( outputDifference ){ Debug.WriteLine("\tDIFFERENCE: (" + ID + ") - " + theCategory.description() + " present here but not in master" ); } categoriesDifferent = true; }
                    foreach( Category theCategory in (List<Category>)this["categories"]     ){ if( theCategory.sync() ){ if( outputDifference ){ Debug.WriteLine("\tDIFFERENCE: (" + ID + ") - " + theCategory.description() + " not found" ); } categoriesDifferent = true; } }
                }

                if( this["words"] != null )
                {
                    foreach( Word theWord in (List<Word>)this["wordsDead"] ){ theWord.nuke(); ((List<Word>)this["wordsDead"]).Remove(theWord); if( outputDifference ){ Debug.WriteLine("\tDIFFERENCE: (" + ID + ") - " + theWord.description() + " present here but not in master" ); } wordDifferential[theWord.wordString] = 1; }
                    foreach( Word theWord in (List<Word>)this["words"]     ){ if( theWord.sync() ){ if( outputDifference ){ Debug.WriteLine("\tDIFFERENCE: (" + ID + ") - " + theWord.description() + " not found" ); } wordDifferential[theWord.wordString] = 1;  } }
                }

                if( this["links"] != null )
                {
                    foreach( Link theLink in (List<Link>)this["linksDead"] ){ theLink.nuke(); ((List<Link>)this["linksDead"]).Remove(theLink); if( outputDifference ){ Debug.WriteLine("\tDIFFERENCE: (" + ID + ") - " + theLink.description() + " present here but not in master" ); } urlsDifferent = true; }
                    foreach( Link theLink in (List<Link>)this["links"]     ){ if( theLink.sync() ){ if( outputDifference ){ Debug.WriteLine("\tDIFFERENCE: (" + ID + ") - " + theLink.description() + " not found" ); } urlsDifferent = true; } }
                }

                if( this["connections"] != null )
                {
                    foreach( var x in (Dictionary<string,Connection>)this["connectionsDead"] ){ string conHash = x.Key; Connection theConnection = x.Value; theConnection.nuke(); ((Dictionary<string, Connection>)this["connectionsDead"]).Remove(conHash); if( outputDifference ){ Debug.WriteLine("\tDIFFERENCE: (" + ID + ") - " + theConnection.description() + " present here but not in master" ); } connectionDifferential[theConnection.getOtherConnection(this).ID] = 1; }
                    foreach( var x in (Dictionary<string,Connection>)this["connections"]     ){ string conHash = x.Key; Connection theConnection = x.Value; if ( theConnection.sync() ){ if( outputDifference ){ Debug.WriteLine("\tDIFFERENCE: (" + ID + ") - " + theConnection.description() + " not found" ); } connectionDifferential[theConnection.getOtherConnection(this).ID] = 1; } }
                }

                if( this["pageFeedback"] != null )
                {
                    foreach( PageFeedback thePageFeedback in (List<PageFeedback>)this["pageFeedbackDead"] ){ thePageFeedback.nuke(); ((List<PageFeedback>)this["pageFeedbackDead"]).Remove(thePageFeedback); ; if( outputDifference ){ Debug.WriteLine("\tDIFFERENCE: (" + ID + ") - " + thePageFeedback.description() + " present here but not in master" ); } pageFeedbackDifferential[thePageFeedback.page_ID] = 1; }
                    foreach( PageFeedback thePageFeedback in (List<PageFeedback>)this["pageFeedback"]     ){ if( thePageFeedback.sync() ){ if( outputDifference ){ Debug.WriteLine("\tDIFFERENCE: (" + ID + ") - " + thePageFeedback.description() + " not found" ); } pageFeedbackDifferential[thePageFeedback.page_ID] = 1; } }
                }
                
                int wordsChanged        =          (wordDifferential.Count > 0) ?  1 : 0;
                int categoriesChanged   =                 (categoriesDifferent) ?  2 : 0;
                int urlsChanged         =              (urlsDifferent ==  true) ?  4 : 0;
                int connectionsChanged  =    (connectionDifferential.Count > 0) ?  8 : 0;
                int pageFeedbackChanged =  (pageFeedbackDifferential.Count > 0) ? 16 : 0;
                int needsReviewChanged  =                (needsReviewDifferent) ? 32 : 0;

                return wordsChanged + categoriesChanged + urlsChanged + connectionsChanged + pageFeedbackChanged + needsReviewChanged;
            }

            return 0;
        }

        /* GROUP(Actions) Deletes this ultralink. */
        public bool nuke(){ if (APICall("nuke", "Could not nuke " + description()) != null) { return true; } return false; }

    // Words

        /* GROUP(Words) Returns the primary word if set. If not, returns the first word listed. */
        public Word getFirstWord(){ Word word = getPrimaryWord(); if( word != null ){ if( ((List<Word>)this["words"]).Count > 0 ){ word = ((List<Word>)this["words"])[0]; } } return word; }

        /* GROUP(Words) Returns the primary word or null if not set. */
        public Word getPrimaryWord(){ foreach( Word theWord in (List<Word>)this["words"] ){ if( theWord.primaryWord == "true" ){ return theWord; } } return null; }

        /* GROUP(Words) string(A word string.) nuke(Boolean. If true then remove the Word object found from the Ultralink.) Returns the word on this ultralink associated with <b>string</b>. */
        public Word getWord( string wordString, bool nuke = false ){ foreach( Word theWord in (List<Word>)this["words"] ){ if( theWord.wordString == wordString ){ if( nuke ){ ((List<Word>)this["words"]).Remove(theWord); } return theWord; } } return null; }

        /* GROUP(Words) string(A word string.) caseSensitive(Boolean. 1 indicates that this Word is case sensitive.) primaryWord(Boolean. 1 indicates that this word is the primary on this Ultralink.) commonalityThreshold(A number indicating the commonality threshold.) nuke(Boolean. If true then remove the Word object found from the Ultralink.) Returns the word on this ultralink associated with the parameters. */
        public Word getWordFull( string wordString, string caseSensitive, string primaryWord, string commonalityThreshold, bool nuke = false ){ foreach( Word theWord in (List<Word>)this["words"] ){ if( (theWord.wordString == wordString) && (theWord.caseSensitive == caseSensitive) && (theWord.primaryWord == primaryWord) && (theWord.commonalityThreshold == commonalityThreshold) ){ if( nuke ){ ((List<Word>)this["words"]).Remove(theWord); } return theWord; } } return null; }

        /* GROUP(Words) o(A JSON object representing the Word.) Sets a word on this ultralink based on the information in <b>o</b>. */
        public Word setWordFromObject( JObject o )
        {
            if( o["word"] != null )
            {
                string caseSensitive        = null;
                string primaryWord          = null;
                string commonalityThreshold = null;

                if( o["caseSensitive"]        != null ){ caseSensitive        = o["caseSensitive"].ToString();        }
                if( o["primaryWord"]          != null ){ primaryWord          = o["primaryWord"].ToString();          }
                if( o["commonalityThreshold"] != null ){ commonalityThreshold = o["commonalityThreshold"].ToString(); }

                return setWord( o["word"].ToString(), caseSensitive, primaryWord, commonalityThreshold );
            }

            return null;
        }

        /* GROUP(Words) string(A word string.) caseSensitive(Boolean. 1 indicates that this Word is case sensitive.) primaryWord(Boolean. 1 indicates that this word is the primary on this Ultralink.) commonalityThreshold(A number indicating the commonality threshold.) Sets a word on this ultralink. Adds it if it does exist yet and modifies it if it does. */
        public Word setWord( string wordString, string caseSensitive = null, string primaryWord = null, string commonalityThreshold = null )
        {
            Word theWord = getWord(wordString);
            if( theWord != null )
            {
                if( caseSensitive        != null ){ theWord.caseSensitive        = caseSensitive ;       }
                if( primaryWord          != null ){ theWord.primaryWord          = primaryWord;          }
                if( commonalityThreshold != null ){ theWord.commonalityThreshold = commonalityThreshold; }

                return theWord;
            }

            if( caseSensitive        == null ){ caseSensitive        = "0"; }
            if( primaryWord          == null ){ primaryWord          = "0"; }
            if( commonalityThreshold == null ){ commonalityThreshold = "0"; }

            Word newWord = Word.W( this, wordString, caseSensitive, primaryWord, commonalityThreshold );
            newWord.dirty = true;

            foreach( Word deadWord in (List<Word>)this["wordsDead"] )
            {
                if( newWord.isEqualTo( deadWord ) )
                {
                    ((List<Word>)this["wordsDead"]).Remove(deadWord);
                    ((List<Word>)this["words"]).Add( deadWord );
                    return deadWord;
                }
            }

            ((List<Word>)this["words"]).Add(newWord);
            return newWord;
        }

        /* GROUP(Words) w(Word or a word string.) Removes word or string <b>w</b>. */
        public void removeWord( object w )
        {
            if( w is Word ){ w = ((Word)w).wordString; }
            ((List<Word>)this["wordsDead"]).Add( getWord((string)w, true) );
        }

        /* GROUP(Words) Removes all existing words from this ultralink. */
        public void removeAllWords(){ ((List<Word>)this["wordsDead"]).Concat(((List<Word>)this["word"])); this["words"] = new List<Word>(); }

        /* GROUP(Words) w(Word or a word string.) nuWord(Word or a word string.) caseSensitive(Boolean. 1 indicates that this Word is case sensitive.) primaryWord(Boolean. 1 indicates that this word is the primary on this Ultralink.) commonalityThreshold(A number indicating the commonality threshold.) Replaces word <b>w</b> with a new word. */
        public void replaceWord( object w, object nuWord, string caseSensitive = "0", string primaryWord = "0", string commonalityThreshold = "0" )
        {
            removeWord( w );

            if( nuWord is Word )
            {
                ((Word)nuWord).dirty = true;
                ((List<Word>)this["words"]).Add((Word)nuWord);
            }
            else{ setWord( (string)nuWord, caseSensitive, primaryWord, commonalityThreshold ); }
        }

    // Categories

        /* GROUP(Categories) Returns the primary category for this ultralink or returns null if it doesn't exist. */
        public Category getPrimaryCategory(){ foreach( Category theCategory in (List<Category>)this["categories"] ){ string pc = theCategory.primaryCategory; if( pc != null ){ return theCategory; } } return null; }

        /* GROUP(Categories) string(A category string.) nuke(Boolean. If true then remove the Category object found from the Ultralink.) Returns the category on this ultralink identified by <b>string</b>. */
        public Category getCategory( string categoryString, bool nuke = false ){ foreach( Category theCategory in (List<Category>)this["categories"] ){ if( theCategory.categoryString == categoryString ){ if( nuke ){ ((List<Category>)this["categories"]).Remove(theCategory); } return theCategory; } } return null; }

        /* GROUP(Categories) string(A category string.) primaryCategory(Indicates whether this new Category should be the primary.) Sets a category based on <b>string</b>. Adds it if it doens't exist and modifies it if it does. */
        public Category setCategory( string categoryString, string primaryCategory = null )
        {
            Category theCategory = getCategory(categoryString);
            if( theCategory != null )
            {
                if( primaryCategory != null ){ theCategory.primaryCategory = primaryCategory; }

                return theCategory;
            }

            if( primaryCategory == null ){ primaryCategory = "0"; }

            Category newCategory = Category.C( this, categoryString, primaryCategory );
            newCategory.dirty = true;

            foreach( Category deadCategory in (List<Category>)this["categoriesDead"] )
            {
                if( newCategory.isEqualTo( deadCategory ) )
                {
                    ((List<Category>)this["categoriesDead"]).Remove(deadCategory);
                    ((List<Category>)this["categories"]).Add(deadCategory);
                    return deadCategory;
                }
            }

            ((List<Category>)this["categories"]).Add(newCategory);
            return newCategory;
        }

        /* GROUP(Categories) c(Category or category string.) Removes the category <b>c</b>. */
        public void removeCategory( object c )
        {
            if( c is Category ){ c = ((Category)c).categoryString; }
            ((List<Category>)this["categoriesDead"]).Remove( getCategory((string)c, true) );
        }

        /* GROUP(Categories) Removes all categories from this ultralink. */
        public void removeAllCategories(){ ((List<Category>)this["categoriesDead"]).Concat(((List<Category>)this["categories"]));  this["categories"] = new List<Category>(); }

        /* GROUP(Categories) c(Category or category string.) nuCategory(A category string.) primaryCategory(Indicates whether this new Category should be the primary.) Replaces category <b>c</b> with the passed values. */
        public void replaceCategory( object c, object nuCategory, string primaryCategory = "0" )
        {
            removeCategory( c );

            if( nuCategory is Category )
            {
                ((Category)nuCategory).dirty = true;
                ((List<Category>)this["categories"]).Add((Category)nuCategory);
            }
            else{ setCategory( (string)nuCategory, primaryCategory ); }
        }

    // Links

        /* GROUP(Links) type(A link type.) language(A language code string.) country(A country code string.) primaryLink(Indicates that the links are primary) Returns a set of links that fit the criteria of the parameters passed in. */
        public List<Link> queryLinks( string type = null, string language = null, string country = null, string primaryLink = null )
        {
            List<Link> filteredResults = new List<Link>();

            foreach( Link link in (List<Link>)this["links"] )
            {
                bool isOK = true;

                if( ( ( type        != null ) && (type        != link.type)        ) ||
                    ( ( language    != null ) && (language    != link.language)    ) ||
                    ( ( country     != null ) && (country     != link.country)     ) ||
                    ( ( primaryLink != null ) && (primaryLink != link.primaryLink) ) ){ isOK = false; }

                if( isOK ){ filteredResults.Add( link ); }
            }

            return filteredResults;
        }

        /* GROUP(Links) url_ID(A URL or URL ID.) type(A link type.) nuke(Boolean. If true then remove the Link object found from the Ultralink.) Returns the link attached to this ultralink identified by <b>url_ID</b>. */
        public Link getLink( string url_ID, string type = null, bool nuke = false )
        {
            int val;
            if( Int32.TryParse(url_ID, out val) )
            {
                if( type == null ){ type = LinkTypes.detectLinkType( url_ID ); }
                url_ID = db.getURLID( url_ID );
            }

            foreach( Link theLink in (List<Link>)this["links"] )
            {
                if( (theLink.url_ID == url_ID) &&
                    ((theLink.type == type  ) || (type == null)) )
                {
                    if( nuke ){ ((List<Link>)this["links"]).Remove(theLink); }

                    return theLink;
                }
            }

            return null;
        }

        /* GROUP(Links) o(A JSON object representing the Link.) Sets a link on this ultralink based on the information in <b>o</b>. */
        public Link setLinkFromObject( JObject o )
        {
            if( (o["ID"] != null) && (o["type"] != null) )
            {
    //            type        = null;
                string language    = null;
                string country     = null;
                string primaryLink = null;
                string metaInfo    = null;

    //            if( o["type"]        != null ){ type        = o["type"];        }
                if( o["language"]    != null ){ language    = o["language"].ToString();    }
                if( o["country"]     != null ){ country     = o["country"].ToString();     }
                if( o["primaryLink"] != null ){ primaryLink = o["primaryLink"].ToString(); }
                if( o["metaInfo"]    != null ){ metaInfo    = o["metaInfo"].ToString();    }

                return setLink( o["ID"].ToString(), o["type"].ToString(), language, country, primaryLink, metaInfo );
            }

            return null;
        }

        /* GROUP(Links) url(A URL or URL ID.) type(A link type.) language(A language code string.) country(A country code string.) primaryLink(Indicates that the links are primary) metaInfo(A JSON object descriing extra meta info about the Link.) Sets a link on this ultralink. Adds it if it doesn't exist and modifies it if it does. */
        public Link setLink( string url, string type = null, string language = null, string country = null, string primaryLink = null, object metaInfo = null )
        {
            int val;
            string url_ID = url; if( !Int32.TryParse(url, out val) ){ url_ID = db.getURLID( url ); if( type == null ){ type = LinkTypes.detectLinkType( url ); } }

            Link theLink = getLink(url_ID, type);
            if( theLink != null )
            {
//                if( type        != null ){ theLink.type = type;               }
                if( language    != null ){ theLink.language    = language;    }
                if( country     != null ){ theLink.country     = country;     }
                if( primaryLink != null ){ theLink.primaryLink = primaryLink; }
                if( metaInfo    != null ){ theLink.metaInfo    = (JObject)metaInfo;    }

                return theLink;
            }

    //        if( is_null(type)        ){ type        = detectLinkType( l.url ); }
            if( language    == null ){ language    =  "";                  }
            if( country     == null ){ country     =  "";                  }
            if( primaryLink == null ){ primaryLink = "0";                  }
            if( metaInfo    == null ){ metaInfo    =  "";                  }

            Link newLink = Link.L( this, url_ID, type, language, country, primaryLink, metaInfo );
            newLink.dirty = true;

            foreach( Link deadLink in (List<Link>)this["linksDead"] )
            {
                if( newLink.isEqualTo( deadLink ) )
                {
                    ((List<Link>)this["linksDead"]).Remove(deadLink);
                    ((List<Link>)this["links"]).Add(deadLink);
                    return deadLink;
                }
            }

            ((List<Link>)this["links"]).Add(newLink);
            return newLink;
        }

        /* GROUP(Links) link(Link or URL string.) type(A link type.) Removes <b>link</b>. Can optionally specify the <b>type</b>. */
        public void removeLink( object link, string type = null )
        {
            if( link is string )
            {
                Link nulink = getLink((string)link, type);
                if( nulink != null )
                {
                    link = nulink;
                }
                else{ UltralinkAPI.commandResult( 404, "No link: " + link + " found on " + description() ); }
            }

            Link theLink = getLink(((Link)link).url_ID, ((Link)link).type, true);
            if( theLink != null ){ ((List<Link>)this["linksDead"]).Add(theLink); }
        }

        /* GROUP(Links) type(A link type.) Removes all links of <b>type</b> from this ultralink. */
        public void removeLinksOfType( string type )
        {
            List<Link> deadLinks = new List<Link>();

            foreach( Link link in (List<Link>)this["links"]     ){ if( link.type == type ){ ((List<Link>)this["linksDead"]).Add(link); } }
            foreach( Link link in (List<Link>)this["linksDead"] ){ removeLink( link ); }
        }

        /* GROUP(Links) Removes all links from this ultralink. */
        public void removeAllLinks(){ ((List<Link>)this["linksDead"]).Concat(((List<Link>)this["links"])); this["links"] = new List<Link>(); }

        /* GROUP(Links) link(Link or URL string.) nuURL(A URL or URL ID.) type(A link type.) Replaces <b>link</b> with another link based on <b>nuURL</b>. */
        public void replaceLinkURL( object link, string nuURL, string type = null )
        {
            if( link is string ){ link = getLink( (string)link, type ); }

            if( link != null )
            {
                setLink( nuURL, ((Link)link).type, ((Link)link).language, ((Link)link).country, ((Link)link).primaryLink, ((Link)link).metaInfo );
                removeLink( link );
            }
        }

    // Connections

        /* GROUP(Connections) ulA(Ultralink or ultralink ID.) ulB(Ultralink or ultralink ID.) nuke(Boolean. If true then remove the Connection object found from the Ultralink.) Returns the connection associated with ultralinks or IDs <b>ulA</b> and <b>ulB</b>. */
        public Connection getConnection( object ulA, object ulB, bool nuke = false )
        {
            if( ulA is string ){ ulA = Ultralink.U( (string)ulA, db ); }
            if( ulB is string ){ ulB = Ultralink.U( (string)ulB, db ); }

            string conHash = ((Ultralink)ulA).db.ID + "_" + ((Ultralink)ulA).ID + "_" + ((Ultralink)ulB).db.ID + "_" + ((Ultralink)ulB).ID;

            if( ((Dictionary<string,Connection>)this["connections"])[ conHash ] != null )
            {
                Connection theConnection = ((Dictionary<string, Connection>)this["connections"])[ conHash ];
                if( nuke ){ ((Dictionary<string, Connection>)this["connections"]).Remove( conHash ); }
                return theConnection;
            }

            return null;
        }

        /* GROUP(Connections) connection(A connection type string.) Returns an array of connections on this ultralink that have the connection type string <b>connection</b>. */
        public List<Connection> queryConnections( string connection )
        {
            List<Connection> filteredResults = new List<Connection>();

            foreach( var x in (Dictionary<string,Connection>)this["connections"] )
            {
                Connection theConnection = x.Value;
                if ( theConnection.connection == connection ){ filteredResults.Add( theConnection ); }
            }

            return filteredResults;
        }

        /* GROUP(Connections) o(A JSON object representing the Connection.) Sets a connection on this ultralink based on the information in <b>o</b>. */
        public Connection setConnectionFromObject( JObject o )
        {
            string aID        = null;
            string bID = null;
            string connection = null;

            if( o["aID"]        != null ){ aID        = o["aID"].ToString();        }
            if( o["bID"]        != null ){ bID        = o["bID"].ToString();        }
            if( o["connection"] != null ){ connection = o["connection"].ToString(); }

            return setConnection( aID, bID, connection );
        }

        /* GROUP(Connections) ulAIn(Connection or ultralink ID) ulBIn(Connection or ultralink ID) connection(A connection type string) otherDid(Boolean. If true, sets the Connection on the other Ultralink.) Sets a connection for the ultralinks or IDs <b>ulAIn</b> and <b>ulBIn</b>. Adds it if it doesn't exist and modifies it if it doesn't. */
        public Connection setConnection( object ulAIn, object ulBIn, string connection = "", bool otherDid = false )
        {
            Ultralink ulA = null;
            Ultralink ulB = null;

            if( ulAIn is string ){ if( (string)ulAIn == "-1" ){ ulA = this; }else{ ulA = Ultralink.U( (string)ulAIn, db ); } }else{ ulA = (Ultralink)ulAIn; }
            if( ulBIn is string ){ if( (string)ulBIn == "-1" ){ ulB = this; }else{ ulB = Ultralink.U( (string)ulBIn, db ); } }else{ ulB = (Ultralink)ulBIn; }

            Connection theConnection = getConnection(ulA, ulB);
            if( theConnection != null )
            {
                theConnection.connection = connection;
                return theConnection;
            }

            Connection newConnection = Connection.C( ulA, ulB, connection );
            newConnection.dirty = true;

            string conHash = newConnection.hashString();

            if( ((Dictionary<string,Connection>)this["connectionsDead"])[ conHash ] != null )
            {
                Connection deadConnection = ((Dictionary<string, Connection>)this["connectionsDead"])[ conHash ];
                ((Dictionary<string, Connection>)this["connectionsDead"]).Remove( conHash );
                ((Dictionary<string, Connection>)this["connections"])[ conHash ] = deadConnection;

                return deadConnection;
            }

            ((Dictionary<string, Connection>)this["connections"])[ conHash ] = newConnection;

            if( otherDid == false )
            {
                     if( ulA.ID == ID ){ if( ulBIn is Ultralink ){ ulB.setConnection( this,  ulB, connection, true ); } }
                else if( ulB.ID == ID ){ if( ulAIn is Ultralink ){ ulA.setConnection(  ulA, this, connection, true ); } }
            }

            return newConnection;
        }

        /* GROUP(Connections) ulB(Connection or ultralink ID) connection(A connection type string) Adds a connection between this ultralink and <b>ulB</b>. */
        public Connection addConnection( object ulB, string connection = "" ){ return setConnection( this, ulB, connection ); }

        /* GROUP(Connections) c(Connection or ultralink ID) connection(A connection type string) Removes the connection <b>c</b>. */
        public void removeConnection( object c, string connection = "" )
        {
            if( c is Connection )
            {
                ((Dictionary<string, Connection>)this["connectionsDead"])[ ((Connection)c).hashString() ] = getConnection( ((Connection)c).ulA, ((Connection)c).ulB, true );
            }
            else
            {
                Ultralink ulB = Ultralink.U( (string)c, db );

                Connection dc = getConnection(this, ulB, true);
                if( dc != null ){ ((Dictionary<string, Connection>)this["connectionsDead"])[ dc.hashString() ] = dc; }
                else
                {
                    dc = getConnection(ulB, this, true);
                    if (dc != null) { ((Dictionary<string, Connection>)this["connectionsDead"])[dc.hashString()] = dc; }
                }
            }
        }

        /* GROUP(Connections) Removes all connections from this ultralink. */
        public void removeAllConnections()
        {
            ((Dictionary<string, Connection>)this["connectionsDead"]).Concat(((Dictionary<string, Connection>)this["connections"])); this["connections"] = new Dictionary<string, Connection>();
        }

    // Page Feedback

        /* GROUP(Page Feedback) page_ID(A page ID) word(A word string.) nuke(Boolean. If true then remove the PageFeedback object found from the Ultralink.) Returns the page feedback for this ultralink on a given page ID for a word string. The word string may be "". Can optionally remove it from the ultralink. */
        public PageFeedback getPageFeedback( string page_ID, string word, bool nuke = false ){ foreach( PageFeedback thePageFeedback in (List<PageFeedback>)this["pageFeedback"] ){ if( (thePageFeedback.page_ID == page_ID) && (thePageFeedback.word == word) ){ if( nuke ){ ((List<PageFeedback>)this["pageFeedback"]).Remove(thePageFeedback); } return thePageFeedback; } } return null; }

        /* GROUP(Page Feedback) o(A JSON object representing the PageFeedback.) Sets a page feedback on this ultralink based on the information in <b>o</b>. */
        public PageFeedback setPageFeedbackFromObject( JObject o )
        {
            if( o["page_ID"] != null )
            {
                string feedback = null;

                if( o["feedback"] != null ){ feedback = o["feedback"].ToString(); }

                return setPageFeedback( o["page_ID"].ToString(), o["word"].ToString(), feedback );
            }

            return null;
        }

        /* GROUP(Page Feedback) page_ID(A page ID) word(A word string.) feedback(A non-zero feedback number.) Sets the page feedback for <b>page_ID</b>, <b>word</b> and <b>feedback</b>. Adds it if it doesn't exist, modifies it if it does. */
        public PageFeedback setPageFeedback( string page_ID, string word, string feedback = null )
        {
            PageFeedback thePageFeedback = getPageFeedback(page_ID, word);
            if( thePageFeedback != null )
            {
                if( feedback != null )
                {
                    if( feedback == "0" ){ removePageFeedback( thePageFeedback ); return null; }
                    else{ thePageFeedback.feedback = feedback; }
                }

                return thePageFeedback;
            }

            if( feedback == null ){ feedback = "-1"; }

            if( feedback != "0" )
            {
                PageFeedback newPageFeedback = PageFeedback.PF( this, page_ID, word, feedback );
                newPageFeedback.dirty = true;

                foreach( PageFeedback deadPageFeedback in (List<PageFeedback>)this["pageFeedbackDead"] )
                {
                    if( newPageFeedback.isEqualTo( deadPageFeedback ) )
                    {
                        ((List<PageFeedback>)this["pageFeedbackDead"]).Remove(deadPageFeedback);
                        ((List<PageFeedback>)this["pageFeedback"]).Add(deadPageFeedback);
                        return deadPageFeedback;
                    }
                }

                ((List<PageFeedback>)this["pageFeedback"]).Add(newPageFeedback);
                return newPageFeedback;
            }

            return null;
        }

        /* GROUP(Page Feedback) pf(PageFeedback) Removes page feedback <b>pf</b> from this ultralink. */
        public void removePageFeedback( PageFeedback pf ){ ((List<PageFeedback>)this["pageFeedbackDead"]).Add( getPageFeedback( pf.page_ID, pf.word, true ) ); }

        /* GROUP(Page Feedback) Removes all page feedback from this ultralink. */
        public void removeAllPageFeedback(){ ((List<PageFeedback>)this["pageFeedbackDead"]).Concat(((List<PageFeedback>)this["pageFeedback"])); List<PageFeedback> pageFeedback = new List<PageFeedback>(); }

    //

        /* GROUP(Annotation) Returns the URL for this Ultralink's annotation link. */
        public string annotationLink( ){ return Master.cMaster.masterPath + "annotation/" + db.ID + "/" + ID; }

        /* GROUP(Annotation) language(A language code string.) country(A country code string.) Gets the annotation content for this ultralink for a given language and country bias if any. */
        public string annotation( string language = "", string country = "" ){ return APICallSub("/annotation", new JObject{ ["language"] = language, ["country"] = country }, "Could not retrieve annotation for " + description()).ToString(); }

        /* GROUP(Annotation) text(Annotation text.) type(The type of content being stored.) language(A language code string.) country(A country code string.) Sets the annotation data for this ultralink to <b>text</b> on <b>langauge</b> and <b>country</b>. */
        public bool setAnnotation( string text, string type = "text", string language = "", string country = "" ){ if (APICall(new JObject { ["set"] = text, ["type"] = type, ["language"] = language, ["country"] = country }, "Could not insert/update annotation for " + description()) != null) { return true; } return false; }

        /* GROUP(Holding Tank) word(A word string.) caseSensitive(Boolean. 1 indicates that this Word is case sensitive.) resolution(The decision string whether to <b>accept</b> or <b>reject</b> the submitted word.) contributor(<user identifier>) Removes the submission entry for the new word. If the resolution is 'accept' then it adds the word. */
        public bool resolveWord( string word, string caseSensitive, string resolution, string contributor ){ if( APICall( new JObject{ ["resolveWord"] = word, ["resolution"] = resolution, ["caseSensitive"] = caseSensitive, ["contributor"] = contributor }, "Couldn't resolve the submitted word (word: " + word + ", resolution: " + resolution + ")" ) != null ){ return true; } return false; }

        /* GROUP(Holding Tank) category(A category string.) resolution(The decision string whether to <b>accept</b> or <b>reject</b> the submitted word.) contributor(<user identifier>) Removes the submission entry for the new category. If the resolution is 'accept' then it adds the category. */
        public bool resolveCategory( string category, string resolution, string contributor ){ if (APICall(new JObject {["resolveCategory"] = category,["resolution"] = resolution,["contributor"] = contributor }, "Couldn't resolve the submitted category (category: " + category + ", resolution: " + resolution + ")") != null) { return true; } return false; }

        /* GROUP(Holding Tank) URL(A URL string.) type(A link type.) resolution(The decision string whether to <b>accept</b> or <b>reject</b> the submitted word.) contributor(<user identifier>) Removes the submission entry for the new link. If the resolution is 'accept' then it adds the link. */
        public bool resolveLink( string URL, string type, string resolution, string contributor ){ if (APICall(new JObject {["resolveLink"] = URL,["type"] = type,["resolution"] = resolution,["contributor"] = contributor }, "Couldn't resolve the submitted link (URL: " + URL + ", type: " + type + ", resolution: " + resolution + ")") != null) { return true; } return false; }

        /* GROUP(Holding Tank) page_ID(A page ID.) feedback(A non-zero feedback number.) resolution(The decision string whether to <b>accept</b> or <b>reject</b> the submitted word.) contributor(<user identifier>) Removes the submission entry for the new page feedback. If the resolution is 'accept' then it adds the page feedback. */
        public bool resolvePageFeedback( string page_ID, string feedback, string resolution, string contributor ){ if (APICall(new JObject {["resolvePageFeedback"] = page_ID,["feedback"] = feedback,["resolution"] = resolution,["contributor"] = contributor }, "Couldn't resolve the submitted page feedback (pageFeedback: " + feedback + ", page_ID" + page_ID + ", resolution: " + resolution + ")") != null) { return true; } return false; }

        /* GROUP(Holding Tank) urlID(A URL ID) type(A link type.) problem(The problem of the above URL.) contributor(<user identifier>) Removes a submitted link from the holding tank or a specific type, problem type and contributor. */
        public bool dismissReportedLink( string urlID, string type, string problem, string contributor ){ if (APICall(new JObject {["dismissReportedLink"] = urlID,["type"] = type,["problem"] = problem,["contributor"] = contributor }, "Couldn't remove the reported link (urlID: " + urlID + ", type: " + type + ", problem: " + problem + ")") != null) { return true; } return false; }

        /* GROUP(Holding Tank) con_ID(An ID for a connected ultralink.) problem(The problem with the connection.) Adds a connection complaint into the holding tank for this ultralink. */
        public bool reportConnection( string con_ID, string problem ){ if (APICall(new JObject {["reportConnection"] = con_ID,["problem"] = problem }, "Could not enter in connection report description_ID: " + description() + ", con_ID: " + con_ID + ", problem: " + problem) != null) { return true; } return false; }

        /* GROUP(Holding Tank) url_ID(A URL ID) type(A link type.) problem(The problem of the above URL.) Adds a link complaint into the holding tank for this ultralink. */
        public bool reportLink( string url_ID, string type, string problem ){ if (APICall(new JObject {["reportLink"] = url_ID,["type"] = type,["problem"] = problem }, "Could not enter in link report description_ID: " + description() + ", url_ID: " + url_ID + ", type: " + type + ", problem: " + problem) != null) { return true; } return false; }

        /* GROUP(Holding Tank) category(A category string.) Adds a category into the holding tank for this ultralink. */
        public bool submitCategory( string category ){ if (APICall(new JObject {["submitCategory"] = category }, "Could not enter in submitted category " + category + " ultralink: " + description()) != null) { return true; } return false; }

        /* GROUP(Holding Tank) connection(A connection type string.) connection_ID(The ID of another ultralink to connect to.) Adds a connection into the holding tank for this ultralink. */
        public bool submitConnection( string connection, string connection_ID ){ if (APICall(new JObject {["submitConnection"] = connection,["connection_ID"] = connection_ID }, "Could not enter in submitted connection " + connection + " ultralink: " + description() + " connection_ID: " + connection_ID) != null) { return true; } return false; }

        /* GROUP(Holding Tank) URL(A URL string.) type(A link type.) Adds a link into the holding tank for this ultralink. */
        public bool submitLink( string URL, string type ){ if (APICall(new JObject {["submitLink"] = URL,["type"] = type }, "Could not enter in submitted link ultralink: " + description() + ", URL: " + URL + ", type: " + type) != null) { return true; } return false; }

        /* GROUP(Holding Tank) word(A word string.) caseSensitive(Boolean. 1 indicates that this Word is case sensitive.) Adds a word into the holding tank for this ultralink. */
        public bool submitWord( string word, string caseSensitive ){ if (APICall(new JObject {["submitWord"] = word,["caseSensitive"] = caseSensitive }, "Could not enter in submitted words ultralink: " + description() + ", word: " + word + ", caseSensitive: " + caseSensitive) != null) { return true; } return false; }

        /* GROUP(Holding Tank) pageURL(A URL that this ultralink needs a bias on.) feedback(A non-zero feedback number.) Adds a page feedback value for the specified page into the holding tank for this ultralink. */
        public bool submitPageFeedback( string pageURL, string feedback ){ if (APICall(new JObject {["submitPageFeedback"] = pageURL,["feedback"] = feedback }, "Could not enter in submitted page feedback " + description() + " " + pageURL + " " + feedback) != null) { return true; } return false; }

        public object APICallSub( string sub, object fields, string error )
        {
            object result = Master.cMaster.APICall("0.9.1/db/" + db.ID + "/ul/" + ID + sub, fields);
            if (result != null) { return result; }else{ UltralinkAPI.commandResult( 500, error ); }

            return null;
        }
        public object APICall( object fields, string error ){ return APICallSub( "", fields, error ); }
    }
}
