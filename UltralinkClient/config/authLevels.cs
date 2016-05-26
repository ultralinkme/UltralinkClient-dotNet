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
    class Auth
    {
        public static Dictionary<string,int> authLevels = new Dictionary<string, int> { { "Open"                  , 0 },
                                                                                        { "Optional"              , 0 },
                                                                                        { "Anonymous Contributor" , 0 },
                                                                                        { "Contributor"           , 1 },
                                                                                        { "Editor"                , 2 },
                                                                                        { "Admin"                 , 3 },
                                                                                        { "Root"                  , 4 },
                                                                                        { "Node"                  , 1000 }
                                                                                      };

        string roleForAuthLevel( int authLevel )
        {
            foreach( var x in authLevels )
            {
                string role  = x.Key;
                int level    = Int32.Parse( x.Value.ToString() );

                if( authLevel == level ){ return role; }
            }
            
            return "Unknown";
        }
    }
}
