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
    public class PageFeedback
    {
        public Ultralink ul;

        private string _page_ID;  public string  page_ID { get{ return _page_ID;  } set{ if( _page_ID == null ){ _page_ID = value; } }                  }
        private string _word;     public string     word { get{ return _word;     } set{ if(    _word == null ){    _word = value; } }                  }

        private string _feedback; public string feedback { get{ return _feedback; } set{ if( (_feedback != null) && (_feedback != value) ){ dirty = true; } _feedback = value; } }

        public bool dirty = false;

        /* GROUP(Class Functions) ul(Ultralink) Returns an array of links for the Ultralink <b>ul</b>. */
        public static List<PageFeedback> getPageFeedback( Ultralink ul )
        {
            List<PageFeedback> thePageFeedback = new List<PageFeedback>();

            JArray JArray = (JArray)Master.cMaster.APICall("0.9.1/db/" + ul.db.ID + "/ul/" + ul.ID, "pageFeedbacks" );
            if(JArray != null )
            {
                foreach( JObject pf in JArray) { thePageFeedback.Add( PageFeedback.pageFeedbackFromObject( ul, pf ) ); }
            }
            else{ UltralinkAPI.commandResult( 500, "Could not retrieve page feedbacks for " + ul.ID + " - " + ul.db.name ); }

            return thePageFeedback;
        }

        /* GROUP(Class Functions) theUL(Ultralink) thePageID(A page ID.) theWord(A string of the word that the feedback is on.) theFeedback(A feedback number.) Creates a page feedback for Ultralink <b>theUL</b>. */
        public static PageFeedback PF( Ultralink theUL, string thePageID, string theWord, string theFeedback = null )
        {
            PageFeedback pf = new PageFeedback();

            pf.ul      = theUL;
            pf.page_ID = thePageID;
            pf.word    = theWord;

            if( theFeedback != null )
            {
                pf.feedback = theFeedback;
            }
            else
            {
                JObject details = (JObject)Master.cMaster.APICall("0.9.1/db/" + theUL.db.ID + "/ul/" + theUL.ID, new JObject{ ["pageFeedbackSpecific"] = pf.page_ID, ["word"] = pf.word } );
                if( details != null )
                {
                    pf.feedback = details["feedback"].ToString();
                }
                else
                {
                    pf.feedback = "-1";
                    pf.dirty = true;
                }
            }

            return pf;
        }

        /* GROUP(Class Functions) ul(Ultralink) pf(A JSON object representing a PageFeedback object.) Creates a page feedback on based on the state in <b>pf<b> object passed in. */
        public static PageFeedback pageFeedbackFromObject( Ultralink ul, JObject pf ){ return PageFeedback.PF( ul, pf["page_ID"].ToString(), pf["word"].ToString(), pf["feedback"].ToString() ); }

        public void __destruct(){ if( ul != null ){ ul = null; } }

        /* GROUP(Information) Returns a string describing this page feedback. */
        public string description(){ return "Page Feedback " + page_ID + " / " + word + " / " + feedback; }

        /* GROUP(Representations) Returns a JSON string representation of this page feedback. */
        public string json(){ return JsonConvert.SerializeObject( objectify() ); }

        /* GROUP(Representations) Returns a serializable object representation of the page feedback. */
        public JObject objectify(){ return new JObject{ ["page_ID"] = page_ID, ["word"] = word, ["feedback"] = feedback }; }

        /* GROUP(Actions) other(PageFeedback) Performs a value-based equality check. */
        public bool isEqualTo( PageFeedback other )
        {
            if( feedback == other.feedback )
            { return true; }
            return false;
        }

        /* GROUP(Actions) Syncs the status of this page feedback to disk in an efficient way. */
        public bool sync()
        {
            if( dirty )
            {
                if( Master.cMaster.APICall("0.9.1/db/" + ul.db.ID + "/ul/" + ul.ID, new JObject { ["setPageFeedback"] = json() }) == null ){ UltralinkAPI.commandResult( 500, "Could not set page feedback " + description() + " on to " + ul.description() ); }
                dirty = false;

                return true;
            }

            return false;
        }

        /* GROUP(Actions) Deletes this page feedback. */
        public void nuke()
        {
            if( Master.cMaster.APICall("0.9.1/db/" + ul.db.ID + "/ul/" + ul.ID, new JObject { ["removePageFeedback"] = json() }) == null ){ UltralinkAPI.commandResult( 500, "Could not remove page feedback " + description() + " from " + ul.ID + " - " + ul.db.name ); }
        }
    }
}
