// Copyright © 2016 Ultralink Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using System.IO;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UL
{
    class LinkTypes
    {
        public static JObject linkTypes = LinkTypes.loadLinkTypes("linkTypes.json");

        public static JObject loadLinkTypes( string file )
        {
            JObject lt = null;

            Assembly a = typeof(Ultralink).GetTypeInfo().Assembly;
            String resourcePath = a.GetName().Name + "." + file.Replace("/", ".");

            if( a.GetManifestResourceInfo(resourcePath) != null )
            {
                Stream stream = a.GetManifestResourceStream(resourcePath);

                var serializer = new JsonSerializer();
                
                using( var sr = new StreamReader(stream) )
                using( var jsonTextReader = new JsonTextReader(sr) )
                {
                    lt = serializer.Deserialize<JObject>(jsonTextReader);
                }
            }

            return lt;
        }

        public static List<string> orderedCategories = LinkTypes.doOrderedCategories();

        private static int catOrder( string a, string b)
        {
            int aVal = 0; if( linkTypes[a]["order"] != null) { aVal = Int32.Parse(linkTypes[a]["order"].ToString()); }
            int bVal = 0; if( linkTypes[b]["order"] != null ){ bVal = Int32.Parse(linkTypes[b]["order"].ToString()); }
            return aVal - bVal;
        }

        public static List<string> doOrderedCategories()
        {
            orderedCategories = new List<string>();

            foreach( var x in linkTypes )
            {
                string cat = x.Key;
                orderedCategories.Add( cat );
            }

            orderedCategories.Sort(catOrder);

            return orderedCategories;
        }

        public static int categoryNumber( string tcat ){ int n = 0; while( n < orderedCategories.Count ){ if( orderedCategories[n] == tcat ){ break; } n++; } return n; }

        public static void mergeLinkTypes( JObject customLinkTypes, string resourceLocation )
        {
            foreach( var x in customLinkTypes )
            {
                string ccat           = x.Key;
                JObject customLinkCat = (JObject)x.Value;

                if( linkTypes[ccat] != null )
                {
                    JObject existingLinkCat = (JObject)linkTypes[ccat];

                    foreach( var y in (JObject)customLinkCat["links"] )
                    {
                        string itype           = y.Key;
                        JObject customLinkType = (JObject)y.Value;

                        if( existingLinkCat["links"][itype] != null )
                        {
                            JObject existingLinkType = (JObject)existingLinkCat["links"][itype];
                            foreach( var z in customLinkType )
                            {
                                string setting = z.Key;
                                string val     = z.Value.ToString();

                                updateLinkType( itype, setting, val );
                            }
                        }
                        else
                        {
                            linkTypes[ccat]["links"][itype] = customLinkType;
                            if( resourceLocation != null ){ updateLinkType( itype, "resourceLocation", resourceLocation ); }
                        }
                    }
                }
                else
                {
                    linkTypes[ccat] = customLinkCat;

                    if( resourceLocation != null )
                    {
                        foreach( var z in (JObject)customLinkCat["links"] )
                        {
                            string itype = z.Key;
                            updateLinkType( itype, "resourceLocation", resourceLocation );
                        }
                    }
                }
            }

            doOrderedCategories();
        }

        public delegate object linkTypeConditionFunc(string cat, string linkType, JObject link, string extra );

        public static string linkTypeCondition(linkTypeConditionFunc cond, string extra = "" )
        {
            foreach( var x in linkTypes )
            {
                string cat       = x.Key;
                JObject category = (JObject)x.Value;

                foreach( var y in (JObject)(category["links"]) )
                {
                    string linkType = y.Key;
                    JObject link    = (JObject)y.Value;

                    string result = (string)cond( cat, linkType, link, extra );
                    if( result != null ){ return result; }
                }
            }

            return null;
        }

        public static object typeCompare( string cat, string type, JObject link, string ltype ){ if( ltype == type ){ return link; } return null; }

        public static JObject getLinkType( string ltype )
        {
            return (JObject)linkTypeCondition( typeCompare, ltype );
        }

        public static void updateLinkType( string ltype, string key, string value)
        {
            JObject linkType = getLinkType( ltype );

            bool gotIt = false;

            foreach( var x in linkTypes )
            {
                string cat       = x.Key;
                JObject category = (JObject)x.Value;

                foreach( var y in (JObject)category["links"] )
                {
                    string lt    = y.Key;
                    JObject link = (JObject)y.Value;

                    if( lt == ltype )
                    {
                        linkTypes[cat]["links"][lt][key] = value;
                        gotIt = true;
                        break;
                    }
                }

                if( gotIt ){ break; }
            }
        }

        public static string linkDetect( string cat, string type, JObject link, string theURL)
        {
            if( link["detectors"] != null )
            {
                foreach( string detector in link["detectors"] )
                {
                    Regex re = new Regex("#" + detector + "#i");
                    if( re.Match(theURL) != null ){ return type; }
                }
            }

            return null;
        }

        public static string detectLinkType( string URL )
        {
            string result = linkTypeCondition( linkDetect, URL );

            if( result == null ){ result = "href"; }

            return result;
        }
    }
}
