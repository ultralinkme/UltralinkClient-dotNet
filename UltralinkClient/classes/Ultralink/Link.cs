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
    public class Link
    {
        public Ultralink ul;

        private string _url_ID; public string url_ID { get{ return _url_ID; } set{ if( _url_ID == null ){ _url_ID = value; } } }
        private string _url;    public string    url { get{ return _url;    } set{ if( _url    == null ){ _url    = value; } } }
        private string _type;   public string   type { get{ return _type;   } set{ if( _type   == null ){ _type   = value; } } }

        private string _language;    public string    language { get{ return _language;    } set{ if( (_language    != null) && (_language    != value) ){ dirty = true; } _language    = value; } }
        private string _country;     public string     country { get{ return _country;     } set{ if( (_country     != null) && (_country     != value) ){ dirty = true; } _country     = value; } }
        private string _primaryLink; public string primaryLink { get{ return _primaryLink; } set{ if( (_primaryLink != null) && (_primaryLink != value) ){ dirty = true; } _primaryLink = value; } }
        private string _metaInfo;    public JObject   metaInfo { get{ JObject v = JObject.Parse( _metaInfo ); return v; }
        set
        {
            string v = "";
                 if( value is JObject ){ v = JsonConvert.SerializeObject(value); }
            else if( value is string  ){ if ((string)value == "\"\""){ v = ""; }else{ v = (string)value; } }

            if( (_metaInfo != null) && (_metaInfo != v) ){ dirty = true; } _metaInfo = v; }
        }

        public bool dirty = false;

        /* GROUP(Class Functions) ul(Ultralink) Returns an array of links for the ultralink <b>ul</b>. */
        public static List<Link> getLinks( Ultralink ul )
        {
            List<Link> theLinks = new List<Link>();

            JArray links = (JArray)Master.cMaster.APICall("0.9.1/db/" + ul.db.ID + "/ul/" + ul.ID, "links" );
            if( links != null )
            {
                foreach( JObject link in links )
                {
                    ul.db.urlIDs[link["URL"].ToString()] = link["ID"].ToString();
                    theLinks.Add( Link.linkFromObject( ul, link ) );
                }
            }
            else{ UltralinkAPI.commandResult( 500, "Could not retrieve links for " + ul.ID + " - " + ul.db.name ); }

            return theLinks;
        }

        /* GROUP(Class Functions) theUL(Ultralink) theURL(A URL string or URL ID.) type(A link type string.) language(A language code string.) country(A country code string.) primaryLink(Boolean. Indiciates whether the link is primary.) metaInfo(An object or JSON string representing the metaInfo.) theURL2(A URL string or URL ID.) Creates a link on the ultralink <b>theUL</b>. */
        public static Link L( Ultralink theUL, string theURL, string type, string language, string country, string primaryLink, object metaInfo, string theURL2 = "" )
        {
            Link l = new Link();

            l.ul = theUL;
            int URLID;
            if( Int32.TryParse(theURL, out URLID) )
            {
                l.url_ID = theURL;
                if( theURL2 != "" ){ l.url = theURL2; }else{ l.url = theUL.db.getURL( l.url_ID ); }
            }
            else
            {
                if( theURL2 != "" ){ l.url_ID = theURL2; }else{ l.url_ID = theUL.db.getURLID( theURL ); }
                l.url = theURL;
            }

            if( (type == null) || (type == "") ){ type = LinkTypes.detectLinkType( l.url ); }
            l.type = type;

            if( (language != null) && (country != null) && (primaryLink != null) && (metaInfo != null) )
            {
    //            l.type        = type;
                l.language    = language;
                l.country     = country;
                l.primaryLink = primaryLink;
                if( metaInfo is JObject ){ l.metaInfo = (JObject)metaInfo; }else{ if ((string)metaInfo == "") { l.metaInfo = new JObject(); } else { l.metaInfo = JObject.Parse((string)metaInfo); } }
            }
            else
            {
                JObject details = (JObject)Master.cMaster.APICall("0.9.1/db/" + theUL.db.ID + "/ul/" + theUL.ID, new JObject{ "linkSpecific", l.url_ID } );
                if( details != null )
                {
                    l.type        = details["type"].ToString();
                    l.language    = details["language"].ToString();
                    l.country     = details["country"].ToString();
                    l.primaryLink = details["primaryLink"].ToString();
                    l.metaInfo    = (JObject)details["metaInfo"];
                }
                else
                {
                    l.language    =  "";
                    l.country     =  "";
                    l.primaryLink = "0";
                    l.metaInfo    =  new JObject();

                    l.dirty = true;
                }
            }

            return l;
        }

        /* GROUP(Class Functions) ul(Ultralink) link(A JSON object representing the link.) Creates a link on based on the state in <b>link<b> object passed in. */
        public static Link linkFromObject( Ultralink ul, JObject link ){ return Link.L( ul, link["URL"].ToString(), link["type"].ToString(), link["language"].ToString(), link["country"].ToString(), link["primaryLink"].ToString(), link["metaInfo"].ToString(), link["ID"].ToString()); }

        public void __destruct(){ if( ul != null ){ ul = null; } }

        /* GROUP(Information) Returns a string describing this link. */
        public string description(){ return "Link " + url_ID + " / " + url + " / " + type + " / " + language + " / " + country + " / " + primaryLink + " / " + metaInfo; }

        /* GROUP(Information) Returns a string that can be used for hashing purposes. */
        public string hashString(){ return url_ID + "_" + type; }

        /* GROUP(Representations) Returns a JSON string representation of this link. */
        public string json(){ return JsonConvert.SerializeObject( objectify() ); }

        /* GROUP(Representations) Returns a serializable object representation of the link. */
        public JObject objectify(){ return new JObject { ["ID"] = url_ID, ["URL"] = url, ["type"] = type, ["language"] = language, ["country"] = country, ["primaryLink"] = primaryLink, ["metaInfo"] = metaInfo }; }

        /* GROUP(Actions) string(A URL fragment string.) Returns whether the given string is contained in this Link's URL. */
        public bool urlContains( string urlFragment ){ if( url.IndexOf( urlFragment ) != -1 ){ return true; } return false; }

        public static string defaultType = "href";

        /* GROUP(Actions) other(Link) Performs a value-based equality check. */
        public bool isEqualTo( Link other )
        {
            string theMetaInfo = JsonConvert.SerializeObject( other.metaInfo ); if( theMetaInfo == "\"\"" ){ theMetaInfo = ""; }

            if( ( url_ID      == other.url_ID      ) &&
                ( type        == other.type        ) &&
                ( language    == other.language    ) &&
                ( country     == other.country     ) &&
                ( primaryLink == other.primaryLink ) &&
                (JsonConvert.SerializeObject(metaInfo) == theMetaInfo ) &&
                ( ul.ID       == other.ul.ID       ) &&
                ( ul.db.ID    == other.ul.db.ID    ) )
            { return true; }
            return false;
        }

        /* GROUP(Actions) Syncs the status of this link to disk in an efficient way. */
        public bool sync()
        {
            if( dirty )
            {
                if( Master.cMaster.APICall("0.9.1/db/" + ul.db.ID + "/ul/" + ul.ID, new JObject { ["setLink"] = json() }) == null ){ UltralinkAPI.commandResult( 500, "Could not set link " + description() + " on to " + ul.description() ); }

                dirty = false;

                return true;
            }

            return false;
        }

        /* GROUP(Actions) Deletes this link. */
        public void nuke()
        {
            if( Master.cMaster.APICall("0.9.1/db/" + ul.db.ID + "/ul/" + ul.ID, new JObject {["removeLink"] = json() }) == null ){ UltralinkAPI.commandResult( 500, "Could not remove link " + description() + " from " + ul.ID + " - " + ul.db.name ); }
        }
    }
}
