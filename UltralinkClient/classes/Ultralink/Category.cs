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
    public class Category
    {
        public Ultralink ul;

        private string _categoryString;  public string  categoryString { get{ return _categoryString;  } set{ if( _categoryString == null ){ _categoryString = value; } }                  }

        private string _primaryCategory; public string primaryCategory { get{ return _primaryCategory; } set{ if( (_primaryCategory != null) && (_primaryCategory != value) ){ dirty = true; } _primaryCategory = value; } }

        public bool dirty = false;

        public static string defaultCategory = "(NEEDS CATEGORIZATION)";

        /* GROUP(Class Functions) ul(Ultralink) Returns an array of categories for the ultralink <b>ul</b>. */
        public static List<Category> getCategories( Ultralink ul )
        {
            List<Category> theCategories = new List<Category>();

            JArray categories = (JArray)Master.cMaster.APICall("0.9.1/db/" + ul.db.ID + "/ul/" + ul.ID, "categories");
            if( categories != null )
            {
                foreach( JObject category in categories ){ theCategories.Add( Category.categoryFromObject( ul, category ) ); }
            }
            else{ UltralinkAPI.commandResult( 500, "Could not retrieve categories for " + ul.ID + " - " + ul.db.name ); }

            return theCategories;
        }

        /* GROUP(Class Functions) theUL(Ultralink) theString(A category string.) thePrimaryCategory(Boolean. Indicates whether this category is primary.) Creates a category <b>theString</b> on the ultralink <b>theUL</b>. */
        public static Category C( Ultralink theUL, string theString, string thePrimaryCategory = null )
        {
            Category c = new Category();

            c.ul             = theUL;
            c.categoryString = theString;

            if( thePrimaryCategory != null )
            {
                c.primaryCategory = thePrimaryCategory;
            }
            else
            {
                JObject details = (JObject)Master.cMaster.APICall("0.9.1/db/" + theUL.db.ID + "/ul/" + theUL.ID, new JObject{ ["categorySpecific"] = theString } );
                if( details != null )
                {
                    c.primaryCategory = details["primaryCategory"].ToString();
                }
                else
                {
                    c.primaryCategory = "0";
                    
                    c.dirty = true;
                }
            }

            return c;
        }

        /* GROUP(Class Functions) ul(Ultralink) category(A JSON representation of the Category object.) Creates a category on based on the state in <b>category<b> object passed in. */
        public static Category categoryFromObject( Ultralink ul, JObject category ){ return Category.C( ul, category["category"].ToString(), category["primaryCategory"].ToString()); }

        /* GROUP(Information) Returns a string that can be used for hashing purposes. */
        public string hashString(){ return categoryString; }

        /* GROUP(Information) Returns a string describing this category. */
        public string description(){ return "Category " + categoryString + " / " + primaryCategory; }

        /* GROUP(Representations) Returns a JSON string representation of this category. */
        public string json(){ return JsonConvert.SerializeObject( objectify() ); }

        /* GROUP(Representations) Returns a serializable object representation of the category. */
        public JObject objectify(){ return new JObject{ ["category"] = categoryString, ["primaryCategory"] = primaryCategory }; }

        /* GROUP(Primary) Returns the primary category for the ultralink that this category is attached to. */
        public string getCurrentPrimary()
        {
            JValue currentPrimary = (JValue)Master.cMaster.APICall("0.9.1/db/" + ul.db.ID + "/ul/" + ul.ID, "primaryCategory" );
            if( currentPrimary != null )
            {
                return currentPrimary.ToObject<string>();
            }
            else{ UltralinkAPI.commandResult( 500, "Could not get primary category for " + ul.description() ); }

            return null;
        }

        public void __destruct(){ if( ul != null ){ ul = null; } }

        /* GROUP(Actions) other(Category) Performs a value-based equality check. */
        public bool isEqualTo( Category other )
        {
            if( ( categoryString  == other.categoryString  ) &&
                ( primaryCategory == other.primaryCategory ) &&
                ( ul.ID           == other.ul.ID           ) &&
                ( ul.db.ID        == other.ul.db.ID        ) )
            { return true; }
            return false;
        }

        /* GROUP(Actions) Syncs the status of this category to disk in an efficient way. */
        public bool sync()
        {
            if( dirty )
            {
                if( Master.cMaster.APICall("0.9.1/db/" + ul.db.ID + "/ul/" + ul.ID, new JObject {["setCategory"] = json() }) == null ){ UltralinkAPI.commandResult( 500, "Could not set category " + description() + " on to " + ul.description() ); }
                dirty = false;

                return true;
            }

            return false;
        }

        /* GROUP(Actions) Deletes this category. */
        public void nuke()
        {
            if( Master.cMaster.APICall("0.9.1/db/" + ul.db.ID + "/ul/" + ul.ID, new JObject {["removeCategory"] = json() }) == null ){ UltralinkAPI.commandResult( 500, "Could not remove category " + description() + " from " + ul.description() ); }
        }
    }
}
