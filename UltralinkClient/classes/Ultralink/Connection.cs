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
    public class Connection
    {
        private Ultralink _ulA; public Ultralink ulA { get{ return _ulA; } set{ if( _ulA == null ){ _ulA = value; } } }
        private Ultralink _ulB; public Ultralink ulB { get{ return _ulB; } set{ if( _ulB == null ){ _ulB = value; } } }

        private string _connection; public string connection { get{ return _connection; } set{ if( (_connection != null) && (_connection != value) ){ dirty = true; } _connection = value; } }

        public bool dirty = false;

        /* GROUP(Class Functions) ul(Ultralink) Returns an array of connections for the Ultralink <b>ul</b>. */
        public static List<Connection> getConnections( Ultralink ul )
        {
            List<Connection> theConnections = new List<Connection>();

            JArray connections = (JArray)Master.cMaster.APICall("0.9.1/db/" + ul.db.ID + "/ul/" + ul.ID, "connections" );
            if( connections != null )
            {
                foreach( JObject connection in connections ){ theConnections.Add( Connection.connectionFromObject( ul, connection ) ); }
            }
            else{ UltralinkAPI.commandResult( 500, "Could not retrieve connections for " + ul.ID + " - " + ul.db.name ); }

            return theConnections;
        }

        /* GROUP(Class Functions) theULA(Ultralink or <ultralink identifier>) theULB(Ultralink or <ultralink identifier>) theConnection(A connection string.) db(<Database or <database identifier>>) Creates a connection <b>theConnection</b> between Ultralinks <b>theULA</b> and <b>theULB</b>. */
        public static Connection C( object theULA, object theULB, string theConnection = null, object db = null )
        {
            Connection c = new Connection();

            if( db != null )
            {
                if( theULA is string ){ theULA = Ultralink.U( (string)theULA, db ); }
                if( theULB is string ){ theULB = Ultralink.U( (string)theULB, db ); }
            }
            else if( (theULA is Ultralink) || (theULB is Ultralink) )
            {
                if( theULA is string ){ theULA = Ultralink.U( (string)theULA, ((Ultralink)theULB).db ); }
                if( theULB is string ){ theULB = Ultralink.U( (string)theULB, ((Ultralink)theULA).db ); }
            }
            else{ UltralinkAPI.commandResult( 500, "Need some sort of database context to make this connection (A: " + theULA + ", B: " + theULB + ", theDB: " + db + ")" ); }

            c.ulA = (Ultralink)theULA;
            c.ulB = (Ultralink)theULB;

            if( theConnection != null )
            {
                c.connection = theConnection;
            }
            else
            {
                JObject details = (JObject)Master.cMaster.APICall("0.9.1/db/" + ((Ultralink)theULA).db.ID + "/ul/" + ((Ultralink)theULA).ID, new JObject{ ["connectionA"] = ((Ultralink)theULA).ID, ["connectionB"] = ((Ultralink)theULB).ID } );
                if( details != null )
                {
                    c.connection = details["connection"].ToString();
                }
                else
                {
                    c.connection = "";

                    c.dirty = true;
                }
            }

            return c;
        }

        /* GROUP(Class Functions) ul(Ultralink) connection(A JSON object representing the Connection.) Creates a connection on based on the state in <b>connection<b> object passed in. */
        public static Connection connectionFromObject( Ultralink ul, JObject connection ){ return Connection.C( connection["aID"].ToString(), connection["bID"].ToString(), connection["connection"].ToString(), ul.db ); }

        public void __destruct(){ if( ulA != null ){ ulA = null; } if( ulB != null ){ ulB = null; } }

        /* GROUP(Information) Returns a string describing this connection. */
        public string description(){ return "Connection " + ulA.ID + " / " + ulB.ID + " / " + connection; }

        /* GROUP(Information) Returns a string that can be used for hashing purposes. */
        public string hashString(){ return ulA.db.ID + "_" + ulA.ID + "_" + ulB.db.ID + "_" + ulB.ID; }

        /* GROUP(Representations) Returns a JSON string representation of this connection. */
        public string json(){ return JsonConvert.SerializeObject( objectify() ); }

        /* GROUP(Representations) Returns a serializable object representation of the connection. */
        public JObject objectify(){ return new JObject { ["aID"] = ulA.ID, ["bID"] = ulB.ID, ["connection"] = connection }; }

        /* GROUP(Connections) theUL(Ultralink) Returns the ultalink that <b>theUL</b> is connected to through this connection. */
        public Ultralink getOtherConnection( Ultralink theUL )
        {
                 if( ulA.ID == theUL.ID ){ return ulB; }
            else if( ulB.ID == theUL.ID ){ return ulA; }

            return null;
        }

        /* GROUP(Actions) other(Connection) Performs a value-based equality check. */
        public bool isEqualTo( Connection other )
        {
            if( ( connection == other.connection ) &&
                ( ulA.ID     == other.ulA.ID     ) &&
                ( ulB.ID     == other.ulB.ID     ) &&
                ( ulA.db.ID  == other.ulA.db.ID  ) &&
                ( ulB.db.ID  == other.ulB.db.ID  ) )
            { return true; }
            return false;
        }

        /* GROUP(Actions) Syncs the status of this connection to disk in an efficient way. */
        public bool sync()
        {
            if( dirty )
            {
                if( Master.cMaster.APICall("0.9.1/db/" + ulA.db.ID + "/ul/" + ulA.ID, new JObject {["setConnection"] = json() }) == null ){ UltralinkAPI.commandResult( 500, "Could not set connection " + description() ); }
                dirty = false;

                return true;
            }

            return false;
        }

        /* GROUP(Actions) Deletes this connection. */
        public void nuke()
        {
            if( Master.cMaster.APICall("0.9.1/db/" + ulA.db.ID + "/ul/" + ulA.ID, new JObject {["removeConnection"] = json() }) == null ){ UltralinkAPI.commandResult( 500, "Could remove connection " + description() ); }
        }
    }
}
