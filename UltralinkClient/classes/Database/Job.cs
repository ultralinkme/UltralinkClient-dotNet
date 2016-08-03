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
    public class Job
    {
        public Database db;
        public string ID;

        /* GROUP(Class Functions) */
        public static Job J( string ID = "-1", object db = null )
        {
                 if( db == null   ){ db = Database.cDB;              }
            else if( db is string ){ db = Database.DB( (string)db ); }

            Job j = new Job();

            j.db = (Database)db;
            j.ID = ID;

            return j;
        }

        /* GROUP(Class Functions) ID(<job identifier>) db(<database identifier>) Returns the existing Ultralink object specified by <b>ID</b>. Errors out if it does not exist. */
        public static Job existingJ( string ID = "-1", object db = null ){ Job j = Job.J( ID, db ); if( !j.doesExist() ){ UltralinkAPI.commandResult( 404, j.description() + " does not exist." ); } return j; }

        /* GROUP(Information) Returns a string describing this Ultralink. */
        public string description(){ return "Job " + db.name + "/" + ID; }

        /* GROUP(Information) Returns whether this job exists on disk. */
        public bool doesExist(){ if (APICall("", "Could not test for existence for job " + description()) != null) { return true; } return false; }

        /* GROUP(Work) Gets a list of the assignment details for all users who have even completed any work for the specified job or are currently assigned work. */
        public JArray jobAssignmentDetails(){ return (JArray)APICall( "assignments", "Couldn't lookup the job details for " + ID ); }

        /* GROUP(Work) Gets a list of the Ultralinks currently assigned to the current user for the given job. */
        public JArray getJobAssigned(){ return (JArray)APICallSub( "/ul", "", "Couldn't lookup the assigned for " + ID ); }

        /* GROUP(Work) description_ID(An Ultralink ID.) Gets the work state of an Ultralink for the given job if it has been assigned to the current user. */
        public JObject getWorkState( string description_ID ){ return (JObject)APICallSub( "/ul/" + description_ID, "", "Couldn't lookup the work state for  " + description_ID + " on " + ID ); }

        /* GROUP(Work) description_ID(An Ultralink ID.) operation(A valid operation ID for the job.) input(An input string if the operation requires it.) Commits an operation for an Ultralink in a job with an optional input. */
        public bool commitWork( string description_ID, string operation, string input ){ if (APICallSub("/ul/" + description_ID, new JObject {["operation"] = operation,["input"] = input }, "Couldn't commit work for  " + description_ID + " on " + ID) != null) { return true; } return false; }

        /* GROUP(Work) theUser(<user identifier>) amount(An optional amount of work to assign.) Attempts to assign an optionally given amount of work to the specified user for the specified job. If no amount is specified then if tries to assign the default amount for the job. */
        public int assignWork( string theUser, int amount = 0 ){ return ((JValue)APICall( new JObject { ["assign"] = theUser, ["amount"] = amount }, "Couldn't assign work to " + theUser + " on " + description() )).ToObject<int>(); }

        /* GROUP(Work) theUser(<user identifier>) Removes all the assigned work from <b>theUser</b> for <b>jobID</b>. */
        public bool deassignWork( string theUser ){ if (APICall(new JObject {["deassign"] = theUser }, "Couldn't deassign work from " + theUser + " on " + description()) != null) { return true; } return false; }

        /* GROUP(Work) work_LIMIT(A limit of how much assigned work should be returned.) Attempts to get assigned work data associated with the specified job for the current user up to an optionally given limit. */
        public JArray getWork( int work_LIMIT = -1 ){ return (JArray)APICall( new JObject { "get", work_LIMIT }, "Couldn't get work for " + User.cUser.description() + " from " + description() ); }

        public object APICall( object fields, string error ){ return APICallSub( "", fields, error ); }
        public object APICallSub( string sub, object fields, string error )
        {
            object result = Master.cMaster.APICall("0.9.1/db/" + db.ID + "/jobs/" + ID + sub, fields );
            if( result != null ){ return result; }else{ UltralinkAPI.commandResult( 500, error ); }

            return null;
        }
    }
}
