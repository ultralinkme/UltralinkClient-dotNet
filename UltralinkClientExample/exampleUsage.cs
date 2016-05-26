// Copyright © 2016 Ultralink Inc.

using System.Collections.Generic;
using System.Diagnostics;

using UL;

namespace UltralinkClientExample
{
    class Program
    {
        static void Main(string[] args)
        {
            string APIKey = "<Enter your API Key here>"; // You can get an API Key from your Profile pane in the Ultralink Dashboard.

            // There are a couple of variables that are useful and occassionally come into play.
            Debug.WriteLine( "\n" );
            Debug.WriteLine( "Current Master: \t"   + Master.cMaster.description() ); // The current Ultralink Master you are pointing to.
            Debug.WriteLine( "Current Database: \t" + Database.cDB.description()   ); // The current default Database within cMaster that you are working in.
            Debug.WriteLine( "Current User: \t\t"   + User.cUser.description()     ); // The current User that will perform API calls by default.

            // This is a global variable that indicates whether the process should exit or keep going if a failure occurs.
            // This is set to false only for the purposes of this example code.
            UltralinkAPI.shouldExitOnFail = false;

            // By default, things are set up to use an anonymous User with the Mainline Database at the https://ultralink.me/ Master.
            // So right off the bat you can do anything an unauthenticated User can. For instance, you can load an Ultralink:

            // This line below creates an object representing the Ultralink with ID number 58 in cDB residing at cMaster.
            // You can use Ultralink ID numbers or vanity names to specify which Ultralink you want.
            // You can also specify which specific database the Ultralink resides in.
            Ultralink ul = Ultralink.U(58);

            // Just creating an Ultralink object doesn't actually perform any API calls or load any data.
            // The relevant data is only loaded in on-demand when appropriate.
            // Simply referencing the different parts of an Ultralink is enough force it to page in the data behind the scenes.

            Debug.WriteLine( "\nWords:" );
            foreach( Word word in (List<Word>)ul["words"] ){ Debug.WriteLine( "  " + word.description() ); }

            Debug.WriteLine( "\nCategories:" );
            foreach( Category category in (List<Category>)ul["categories"]) { Debug.WriteLine( "  " + category.description() ); }

            Debug.WriteLine( "\nLinks:" );
            foreach( Link link in (List<Link>)ul["links"] ){ Debug.WriteLine( "  " + link.description() ); }

            Debug.WriteLine( "\nConnections:" );
            foreach( var x in (Dictionary<string,Connection>)ul["connections"] ){ Connection connection = x.Value; Debug.WriteLine( "  " + connection.description() ); }

            Debug.WriteLine( "\nPage Feedback:" );
            foreach( PageFeedback pf in (List<PageFeedback>)ul["pageFeedback"] ){ Debug.WriteLine( "  " + pf.description() ); }

            // You can iterate over these object arrays like above, but most often you will want to get and set various things on an Ultralink using get<class name> methods.
            Debug.WriteLine( "" );
            Word w = ul.getWord("Spencer Nielsen");
            if( w != null ){ Debug.WriteLine( "Lookup by word string: " + w.description() ); }
            Word w2 = ul.getWord("Some other name");
            if( w2 == null ){ Debug.WriteLine( "This word was not found on this Ultralink." ); }

            // Once you have an Ultralink component object, you can modify them.
            Debug.WriteLine( "" );
            Debug.WriteLine( "Before modification:\t" + w.description() );
            w.caseSensitive = "0";
            Debug.WriteLine( "After modification:\t" + w.description() );

            // There are also set<class name> methods which you can use to add new objects to an Ultralink or easily change the properties of existing ones.
            ul.setWord("Spencer For Hire");   // Adds a new Word object for the string "Spencer For Hire".
            ul.setWord("Spence", "0", "0", "3");    // Adds a new Word object for the string "Spence", not case-sensitive, not the primary Word and with a commonality threshold of 3.
            ul.setWord("Spencer Nielsen", "1"); // Changes the existing Word object on this Ultralink for the string "Spencer Nielsen" back to being case-sensitive again.

            // The in-memory Ultralink object here has changed to reflect the modifications we performed above.
            Debug.WriteLine( "\nWords:" );
            foreach( Word word in (List<Word>)ul["words"] ){ Debug.WriteLine("  " + word.description()); }

            // We can examine the exact changes that we have made on the Ultralink.
            Debug.WriteLine( "\nModifications:" );
            ul.printCurrentModifications();

            // To actually write all these pending modifications to the Database in the Master all we need to do is call the sync() method.
            Debug.WriteLine( "\nAttempting to sync" );
            ul.sync();

            // Oh, oops. Looks like an anonymous User is not allowed to actually make changes to the Ultralinks on the Master.
            // To actually write changes back to the Master, we need to authenticate.
            User me = Master.cMaster.login(APIKey);
            Debug.WriteLine( "Me: " + me.description() );

            // Logging in to a Master will automatically set your User to the current User if the current User is anonymous.
            Debug.WriteLine( "Current User: " + User.cUser.description() );

            // Now that you have actually authenticated, you can actually use sync() to write changes to the database.
            // Check out documentation/Documentation.html and browse the each of the classes to get a good idea of what kinds of things you can do.
        }
    }
}
