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
    public class Word
    {
        public Ultralink ul;

        private string _wordString;           public string           wordString { get{ return _wordString;           } set{ if( _wordString == null ){ _wordString = value; } }                                    }

        private string _caseSensitive;        public string        caseSensitive { get{ return _caseSensitive;        } set{ if( (_caseSensitive        != null) && (_caseSensitive        != value) ){ dirty = true; } _caseSensitive        = value; } }
        private string _primaryWord;          public string          primaryWord { get{ return _primaryWord;          } set{ if( (_primaryWord          != null) && (_primaryWord          != value) ){ dirty = true; } _primaryWord          = value; } }
        private string _commonalityThreshold; public string commonalityThreshold { get{ return _commonalityThreshold; } set{ if( (_commonalityThreshold != null) && (_commonalityThreshold != value) ){ dirty = true; } _commonalityThreshold = value; } }

        public bool dirty = false;

        /* GROUP(Class Functions) ul(Ultralink) Returns an array of words for the ultralink <b>ul</b>. */
        public static List<Word> getWords( Ultralink ul )
        {
            List<Word> theWords = new List<Word>();

            JArray words = (JArray)Master.cMaster.APICall("0.9.1/db/" + ul.db.ID + "/ul/" + ul.ID, "words" );
            if( words != null )
            {
                foreach( JObject word in words ){ theWords.Add( Word.wordFromObject( ul, word ) ); }
            }
            else{ UltralinkAPI.commandResult( 500, "Could not retrieve words for " + ul.ID + " - " + ul.db.name ); }

            return theWords;
        }

        /* GROUP(Class Functions) theUL(Ultralink) theString(A word string.) theCaseSensitive(Boolean. Indicates whether the word is case-sensitive.) thePrimaryWord(Boolean. Indicates whether the word is primary.) theCommonalityThreshold(A number indicating the commonality threshold of this word.) Creates a word on <b>theUL</b> based on the passed in paramters. */
        public static Word W( Ultralink theUL, string theString, string theCaseSensitive = null, string thePrimaryWord = null, string theCommonalityThreshold = null )
        {
            Word w = new Word();

            w.ul         = theUL;
            w.wordString = theString;

            if( (theCaseSensitive != null) && (thePrimaryWord != null) && (theCommonalityThreshold != null) )
            {
                w.caseSensitive        = theCaseSensitive;
                w.primaryWord          = thePrimaryWord;
                w.commonalityThreshold = theCommonalityThreshold;
            }
            else
            {
                JObject details = (JObject)Master.cMaster.APICall("0.9.1/db/" + theUL.db.ID + "/ul/" + theUL.ID, new JObject { ["wordSpecific"] = theString } );
                if( details != null )
                {
                    w.caseSensitive        = details["caseSensitive"].ToString();
                    w.primaryWord          = details["primaryWord"].ToString();
                    w.commonalityThreshold = details["commonalityThreshold"].ToString();
                }
                else
                {
                    w.caseSensitive        = "0";
                    w.primaryWord          = "0";
                    w.commonalityThreshold = "0";

                    w.dirty = true;
                }
            }

            return w;
        }

        /* GROUP(Class Functions) ul(Ultralink) word(A JSON object representing the Word.) Creates a word on based on the state in <b>word<b> object passed in. */
        public static Word wordFromObject( Ultralink ul, JObject word ){ return Word.W( ul, word["word"].ToString(), word["caseSensitive"].ToString(), word["primaryWord"].ToString(), word["commonalityThreshold"].ToString()); }

        public void __destruct(){ if( ul != null ){ ul = null; } }

        /* GROUP(Information) Returns a string describing this word. */
        public string description(){ return "Word " + wordString + " / " + caseSensitive + " / " + primaryWord + " / " + commonalityThreshold; }

        /* GROUP(Information) Returns a string that can be used for hashing purposes. */
        public string hashString(){ return wordString; }

        /* GROUP(Representations) Returns a JSON string representation of this word. */
        public string json(){ return JsonConvert.SerializeObject( objectify() ); }

        /* GROUP(Representations) Returns a serializable object representation of the word. */
        public JObject objectify(){ return new JObject{ ["word"] = wordString, ["caseSensitive"] = caseSensitive, ["primaryWord"] = primaryWord, ["commonalityThreshold"] = commonalityThreshold }; }

        /* GROUP(Actions) other(Word) Performs a value-based equality check. */
        public bool isEqualTo( Word other )
        {
            if( ( wordString           == other.wordString           ) &&
                ( caseSensitive        == other.caseSensitive        ) &&
                ( primaryWord          == other.primaryWord          ) &&
                ( commonalityThreshold == other.commonalityThreshold ) &&
                ( ul.ID                == other.ul.ID                ) &&
                ( ul.db.ID             == other.ul.db.ID             ) )
            { return true; }
            return false;
        }

        /* GROUP(Actions) Syncs the status of this word to disk in an efficient way. */
        public bool sync()
        {
            if( dirty )
            {
                if( Master.cMaster.APICall("0.9.1/db/" + ul.db.ID + "/ul/" + ul.ID, new JObject { ["setWord"] = json() }) == null ){ UltralinkAPI.commandResult( 500, "Could not set word " + description() + " on to " + ul.ID + " - " + ul.db.name ); }
                dirty = false;

                return true;
            }

            return false;
        }

        /* GROUP(Actions) Deletes this word. */
        public void nuke()
        {
            if(Master.cMaster.APICall("0.9.1/db/" + ul.db.ID + "/ul/" + ul.ID, new JObject { ["removeWord"] = json() }) == null ){ UltralinkAPI.commandResult( 500, "Could not remove word " + description() + " from " + ul.ID + " - " + ul.db.name ); }
        }
    }
}
