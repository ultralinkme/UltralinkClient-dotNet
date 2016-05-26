// Copyright © 2016 Ultralink Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Text.RegularExpressions;

using System.Diagnostics;

namespace UL
{
    public static class ExtensionMethods
    {
        public static String PregReplace(this String input, string[] pattern, string[] replacements)
        {
            if( replacements.Length != pattern.Length ){ throw new ArgumentException("Replacement and Pattern Arrays must be balanced"); }

            for (var i = 0; i < pattern.Length; i++)
            {
                input = Regex.Replace(input, pattern[i], replacements[i]);
            }

            return input;
        }
    }

    public class UltralinkAPI
    {
        public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1);

        public static int currentTime(){ return (int)((new DateTime() - UltralinkAPI.UnixEpoch).TotalSeconds); }

        public static int startTime = currentTime();
        public static bool shouldExitOnFail = true;

        public static void commandResult(int statusCode, string logString = "")
        {
            int executionTime = currentTime() - startTime;

            string statusString  = "";
            string messageString = "";

            messageString = executionTime + " " + statusString;

            if( User.cUser != null ){ if( User.cUser.ID != "0" ){ messageString += " [" + User.cUser.email + "] "; } }
            if( Database.cDB.ID != "0" ){ messageString += " {" + Database.cDB.name + "} "; }

            messageString += " - " + logString;

            switch( statusCode )
            {
                case 400:
                case 401:
                case 403:
                case 404:
                case 500:
                {
                    Debug.WriteLine( messageString + "\n" );
                    if( shouldExitOnFail ){ /*exit();*/ }
                }
                break;
            }
        }
    }
}
