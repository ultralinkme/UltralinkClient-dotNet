// Copyright © 2016 Ultralink Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace UL
{
    public class ULBase
    {
        protected Dictionary<string, object> objectStorage = new Dictionary<string, object>();
        protected List<string> schema = null;

        public virtual void populateDetail(string key, object value){}

        public void iterateOverDetails( JObject details )
        {
            schema = new List<string>();

            foreach( var x in details )
            {
                string key   = x.Key;
                JToken value = x.Value;

                populateDetail( key, value );

                schema.Add(key);
            }
        }

        protected virtual void populateDetails( string call = "" )
        {
            if( call != "" )
            {
                if( schema == null )
                {
                    JObject details = (JObject)Master.cMaster.APICall( call, "" );
                    if( details != null )
                    {
                        iterateOverDetails(details);
                    }
                    else{ UltralinkAPI.commandResult(500, "Could not lookup info using call " + call); }
                }
            }
            else{ Debug.WriteLine( "Need to pass in a call to populateDetails\n" ); }
        }

    }
}
