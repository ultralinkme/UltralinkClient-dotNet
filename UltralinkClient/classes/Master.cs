// Copyright © 2016 Ultralink Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UL
{
    public class Master
    {
        public static bool printCallstring = false;

        public string masterPath;
        public string masterDomain;

        public int numberOfCalls = 0;

        /* GROUP(Interfacing) mp(A URL to an Ultralink Master) Creates an Ultralink Master instance located at <b>mp</b>. */
        public static Master M( string mp = "https://ultralink.me/" )
        {
            Master m = new Master();

            if( !mp.EndsWith("/") ){ mp += "/"; }

            m.masterPath = mp;
        
            m.masterDomain =   m.masterPath.Replace("https://", "");
            m.masterDomain = m.masterDomain.Replace( "http://", "");
            m.masterDomain = m.masterDomain.Replace(       "/", "");

            return m;
        }

        /* GROUP(Interfacing) at(An Ultralink API Key) mp(A URL to an Ultralink Master) Creates an Ultralink Master instance located at <b>mp</b> and authenticates it against the given API Key <b>at</b>. */
        public static Master authenticate( string at, string mp = "https://ultralink.me/" )
        {
            Master m = Master.M( mp );
            m.login(at);

            return m;
        }

        /* GROUP(Interfacing) at(An Ultralink API Key) Authenticates this Master object with the given API Key. Returns the corresponding User object. */
        public User login( string at )
        {
            JObject details = (JObject)APICall("0.9.1/user/me", "", at);
            if( details != null )
            {
                User u = new User();

                u.ID           = (string)details["ID"];
                u.email        = (string)details["email"];
                u.access_token = at;
            
                u.iterateOverDetails( details );

                if( User.cUser.ID == "0" ){ u.setCurrent(); }
            }
            else{ UltralinkAPI.commandResult(500, "Could not lookup user with token " + at); }

            return User.cUser;
        }

        /* GROUP(Interfacing) identifier(<database identifier>) Sets the current database to the one identified by <b>identifier</b>. */
        public static Master currentMaster( string mp = "https://ultralink.me/" ){ Master theM = M( mp ); if( theM != null ){ theM.setCurrent(); return theM; } return null; }

        /* GROUP(Interfacing) Sets this master to be the current master. */
        public void setCurrent(){ cMaster = this; }

        public object APICall( string command, object fieldsIncoming = null, string at = "" )
        {
            HttpClient ch = new HttpClient();

            ch.DefaultRequestHeaders.UserAgent.ParseAdd("Ultralink API Client .NET/0.9.1");

            Dictionary<string, string> content = new Dictionary<string, string>();

            string callLineString = "";

            string token_string = "";

            if( User.cUser.access_token != "" ){ token_string += "access_token=" + User.cUser.access_token; content["access_token"] = User.cUser.access_token; }
                           else if ( at != "" ){ token_string += "access_token=" + at;                      content["access_token"] = at;                      }

            string fields_string = token_string;

            if ( fieldsIncoming != null )
            {
                if( fieldsIncoming is string )
                {
                    string fields = (string)fieldsIncoming;

                    if (fields_string != "") { fields_string += "&"; }
                    fields_string += fields;
                    callLineString += fields;

                    content[fields] = "";
                }
                else if( fieldsIncoming is JObject )
                {
                    JObject fields = (JObject)fieldsIncoming;
                    foreach (var x in fields)
                    {
                        string key = x.Key;
                        JToken theValue = x.Value;

                        string valueType = theValue.Type.ToString();

                        if (valueType == "JObject")
                        {
                            JObject value = (JObject)theValue;

                            if (fields_string != "") { fields_string += "&"; }
                            fields_string += Uri.EscapeUriString(key) + "=" + Uri.EscapeUriString(JsonConvert.SerializeObject(value));
                            callLineString += key + " = " + value + " ";

                            content[key] = JsonConvert.SerializeObject(value);
                        }
                        else if (theValue == null)
                        {
                            if (fields_string != "") { fields_string += "&"; }
                            fields_string += Uri.EscapeUriString(key);
                            callLineString += key + " ";

                            content[key] = "";
                        }
                        else
                        {
                            string value = theValue.ToString();

                            if (fields_string != "") { fields_string += "&"; }
                            fields_string += Uri.EscapeUriString(key) + "=" + Uri.EscapeUriString(value);
                            callLineString += key + " = " + value + " ";

                            content[key] = value;
                        }
                    }
                }
            }

            //StringContent postContent = new StringContent(fields_string);
            FormUrlEncodedContent postContent = new FormUrlEncodedContent(content);
            var response = ch.PostAsync(masterPath + "API/" + command, postContent).Result;

            if( Master.printCallstring ){ Debug.WriteLine(command + " - " + callLineString); }

            this.numberOfCalls++;

            //string result = response.Content.ToString();
            //string result = response.Content.ReadAsStringAsync();
            //Stream receiveStream = response.ste
            //StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
            //string result = readStream.ReadToEnd();
            string result = response.Content.ReadAsStringAsync().Result;

            if ( response.StatusCode == System.Net.HttpStatusCode.OK )
            {
                if( result == "" ){ return true; }
                //return "true";
                var resObject = JsonConvert.DeserializeObject(result);
                return resObject;
            }
            else{ Debug.WriteLine(this.masterPath + " returned " + response.StatusCode.ToString() + " - " + result); }

            return null;
        }

        /* GROUP(Misc.) Returns all the unique user assocation types. */
        public List<string> associationTypes(){ JArray associationTypes = (JArray)APICall("0.9.1/", "associationTypes"); if(associationTypes != null ){ List<string> types = new List<string>(); foreach( var type in associationTypes) { types.Add( type.ToString() ); } return types; }else{ UltralinkAPI.commandResult( 500, "Could not retrieve the association types" ); } return null; }

        /* GROUP(Misc.) ultralinks(A JSON array of Ultralink ID numbers.) Returns descriptions for the specified ultralinks. */
        public JObject specifiedDescriptions( JArray ultralinks )
        {
            JObject result = new JObject();
            JArray changedUltralinks = new JArray();
            
            int i = 0;
            
            while( i < ultralinks.Count )
            {
                JObject d = Ultralink.U((string)ultralinks[i]).objectify(true);
                if( d != null ){ changedUltralinks.Add( d );                                      }
                           else{ changedUltralinks.Add( new JObject { ["ID"] = ultralinks[i] } ); }
                i++;
            }

            result["changedUltralinks"] = changedUltralinks;

            return result;
        }

        /* GROUP(Misc.) Gets the currrent routing table. */
        public JObject getRoutingTable()
        {
            JArray routingTable = (JArray)APICall("0.9.1/", "getRoutingTable" );
            if(routingTable != null )
            {
                foreach(JObject rt in routingTable)
                {
                    rt["interface"] = rt["interface"] + "API/0.9/";
                    routingTable.Add( rt );
                }

                return new JObject { ["masterDomain"] = routingTable };
            }
            else{ UltralinkAPI.commandResult( 500, "Could not get the routing table" ); }

            return null;
        }

        /* GROUP(Misc.) Returns if this Master exists or not. */
        public bool exists(){ if( APICall("0.9.1/", "exists") != null ){ return true; }else{ UltralinkAPI.commandResult( 500, "Could test for Master existance" ); } return false; }

        /* GROUP(Misc.) Returns a human-readable description string about this Master. */
        public string description(){ return "Master at " + masterPath; }

        /* GROUP(Syncing Progress) name(The name of the sync) type(The type of syncing count. Can be 'time' or 'count') kind(Each syncing type has a 'Lower' and 'Upper' varient. You can use either or both.) This returns a number for either a syncing count or UNIX timestamp representing progress, ceilings or however you want to use these stored values. */
        public long syncingProgress( string name = "", string type = "time", string kind = "Lower" ){ JValue syncingProgress = (JValue)APICall("0.9.1/", new JObject { ["syncingProgress"] = name, ["type"] = type, ["kind"] = kind }); if( syncingProgress != null ){ return syncingProgress.ToObject<long>(); }else{ UltralinkAPI.commandResult( 500, "Could not get the syncing progress for " + name ); } return 0; }

        /* GROUP(Syncing Progress) value(The numeric count or time value of the syncing progress) name(The name of the sync) type(The type of syncing count. Can be 'time' or 'count') kind(Each syncing type has a 'Lower' and 'Upper' varient. You can use either or both.) This enters in the syncing progress specifed by 'value'. */
        public void syncingProgressSet( string value, string name = "", string type = "time", string kind = "Lower" ){ if( APICall("0.9.1/", new JObject { ["syncingProgressSet"] = name, ["type"] = type, ["kind"] = kind, ["value"] = value }) != null ){ UltralinkAPI.commandResult( 500, "Could not set the syncing progress for " + name ); } }

        /* GROUP(Syncing Progress) name(The name of the sync) Returns the currentlySyncing numeric value. */
        public int syncingCurrently( string name = "" ){ JValue syncingCurrently = (JValue)APICall("0.9.1/", new JObject { ["syncingCurrently"] = name }); if(syncingCurrently != null ){ return syncingCurrently.ToObject<int>(); } else{ UltralinkAPI.commandResult( 500, "Could not get the currently syncing for " + name ); } return 0; }

        /* GROUP(Syncing Progress) name(The name of the sync) Attempts to acquire the currentlySyncing lock. */
        public bool syncingLockAquire( string name = "" ){ if( APICall("0.9.1/", new JObject { ["syncingLockAquire"] = name }) != null ){ return true; } return false; }

        /* GROUP(Syncing Progress) name(The name of the sync) force(Indicates whether this should forcefully release the currently syncing lock even if this process doesn't own it.) Attempts to release the currentlySyncing lock. */
        public bool syncingLockRelease( string name = "", bool force = false ){ if (APICall("0.9.1/", new JObject { ["syncingLockRelease"] = name, ["force"] = force }) != null) { return true; }else { UltralinkAPI.commandResult( 500, "Could not release the sync lock for " + name ); return false; } }

        /* GROUP(Syncing Progress) value(The numeric count or time value of the syncing progress) name(The name of the sync) force(Indicates whether this should forcefully release the currently syncing lock even if this process doesn't own it.) Writes the current syncing progress and attempts to release the currentlySyncing lock. */
        public bool syncingComplete( string value, string name = "", bool force = false ){ if (APICall("0.9.1/", new JObject { ["syncingComplete"] = name, ["name"] = name, ["force"] = force }) != null) { return true; }else { UltralinkAPI.commandResult( 500, "Could not set syncing for " + name + " to complete." ); return false; } }

        /* GROUP(Syncing Progress) time(A UNIX timestamp) Converts a UNIX timestamp into an equivolent LDAP timestamp. */
        public string LDAPTime(string time = null ){ if( time != null ){ return String.Format("YmdHis", time) + ".0Z"; }else{ return LDAPTime(new DateTime().ToString() ); } }

        /* GROUP(Invite Codes) Returns a list of all the unredeemed invite codes and the information related to them. */
        public JArray inviteCodes(){ JArray inviteCodes = (JArray)APICall("0.9.1/", "inviteCodes" ); if(inviteCodes != null ){ return inviteCodes; }else{ UltralinkAPI.commandResult( 500, "Could not retrieve the invite codes" ); } return null; }

        /* GROUP(Invite Codes) name(The person's name.) email(Email address for the person being invited.) Creates a new invite code for a user. */
        public bool inviteUser( string name, string email) { if(APICall("0.9.1/", new JObject { [ "inviteUser" ] = email, [ "name" ] = name }) != null ){ return true; }else{ UltralinkAPI.commandResult( 500, "Could not invite " + name + "/" + email ); } return false; }
        
        public static Master cMaster = M();
    }
}
