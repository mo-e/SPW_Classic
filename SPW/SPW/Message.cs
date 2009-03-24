#region using...
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
#endregion

// Message structure definition
// 
// What's [StructLayout( LayoutKind.Sequential )]:
// I'm saying that the .NET framework shouldn't
// take any liberties and re-arrange the order
// of the variables in this struct in its attempt
// to "optimize".

// However, this is redundant, because a struct will 
// be sequential automatically.  This is here out of
// just sheer paranoia.
[StructLayout( LayoutKind.Sequential )]
public struct Message : IComparable // This message struct implements the IComparable interface.
// for more detail, scroll to CompareTo method below.
{
  #region fields
  /// <summary>
  /// The identity of the player sending the message
  /// is attached to the Message structure itself.
  /// </summary>
  public int playerNumber;

  /// <summary>
  /// The frame at which the message was created and
  /// sent by the remote player.
  /// </summary>
  public int frame;

  /// <summary>
  /// The COMMAND that is coming from across the network
  /// to this program.
  /// </summary>
  public NetMessageCommand cmd;
  #endregion

  /// <summary>
  /// Gives you the size of this Message struct
  /// </summary>
  public static int Size
  {
    get
    {
      return System.Runtime.InteropServices.Marshal.SizeOf( typeof( Message ) );
    }
  }

  public byte[] GetBytes()
  {
    #region GetBytes explained
    byte[] resultBytes = new byte[ Message.Size ];

    // resultBytes will be kinda like this:

    // |  |  |  |  |  |  |  |  |  |  |  |  |
    // |__|__|__|__|__|__|__|__|__|__|__|__|
    //  0  1  2  3  4  5  6  7  8  9  10 11
    // resultBytes

    // Now the deal is here, we're going to convert
    // THIS struct object as an ARRAY of bytes and
    // cram it into resultBytes.  Then resultBytes
    // will be shoved across the internet into
    // the remote player's game, and he will use
    // FromBytes() to turn it back into a Message struct.



    // I'm doing this in broken down steps,
    // so you can see exactly what is happening.

    // Get the byte representation for the first
    // variable in this struct.  It will be 4 bytes
    // because I happen to know that an int is 4 bytes.
    // We could have used sizeof() operations throughout
    // here, but we decided to use hard-coded 4's
    // just to make things appear simpler.

    byte[] firstFourBytes = BitConverter.GetBytes( this.playerNumber );

    // firstFourBytes now has the internal bytes of this.playerNumber,
    // whatever that looks like:

    // |01|00|00|00|
    // |__|__|__|__|
    //
    // firstFourBytes






    // Now that I have the byte representation for
    // the first variable of this array, I'm going
    // to smush it into the beginning of my resultBytes array.
    Buffer.BlockCopy(

      firstFourBytes, // smush in values from this array
      0,  // start READING the firstFourBytes array
      resultBytes, // this is the array to smush into
      0,  // start putting in at the beginning of resultBytes
      4   // finally, make sure to read up 4 bytes
      // (because I know an int is 4 bytes).  Again,
      // coulda used sizeof( int ) here, but we
      // wanted to be more clear 'bout it.
    );

    // |01|00|00|00|
    // |__|__|__|__|
    //
    // firstFourBytes

    //   vv  BLOCK COPY  vv

    // |01|00|00|00|  |  |  |  |  |  |  |  |
    // |__|__|__|__|__|__|__|__|__|__|__|__|
    //  0  1  2  3  4  5  6  7  8  9  10 11
    // resultBytes





    // For the next copy, I'm going to do it in one hop.

    Buffer.BlockCopy(

      BitConverter.GetBytes( this.frame ),
      0,  // start at the beginning of the bytes we get for "frame"

      resultBytes, // again save in resultBytes array

      4,  // !! start at BYTE 4 (because the previous write
      // will have taken up the first 4 bytes as shown
      // in the diagram

      4   // and frame is an int, so it takes 4 bytes.

    );


    // Now we have:

    // |55|99|77|22|
    // |__|__|__|__|
    //
    // frame's bytes

    //   vv  BLOCK COPY (starting at BYTE 4)  vv

    //              <- starts here @ byte 4
    // |01|00|00|00|55|99|77|22|  |  |  |  |
    // |__|__|__|__|__|__|__|__|__|__|__|__|
    //  0  1  2  3  4  5  6  7  8  9  10 11
    // resultBytes




    // Finally, for the last variable, this.cmd.
    Buffer.BlockCopy(

      BitConverter.GetBytes( (int)this.cmd ), // ah, tricky.

      // Look at my enum definition at 
      // the top of this file.  It says:

//    public enum NetMessageCommand : int

      // The : int there specifies that the
      // underlying type of this enum must be the int type.

      // So really, it says that any NetMessageCommand is really
      // just going to be an INT.  We cast to (int) to
      // formally do the conversion from "NetMessageCommand" type
      // to the int type.  You can always check out the
      // underlying type of any enum using Enum.GetUnderlyingType()

      0,  // start at the beginning of the bytes we get for "cmd"

      resultBytes, // again save in resultBytes array

      8,  // !! start at BYTE 8 (because the previous write
      // will have taken up the first 4 bytes as shown
      // in the diagram

      4   // and because this.action is an enum with underlying type INT,
      // it will be 4 bytes in size.

    );



    // Now we have:

    // |10|10|19|18|
    // |__|__|__|__|
    //
    // cmd's bytes

    //   vv  BLOCK COPY (starting at BYTE 8)  vv

    //                          <- starts here @ byte 8
    // |01|00|00|00|55|99|77|22|10|10|19|18|
    // |__|__|__|__|__|__|__|__|__|__|__|__|
    //  0  1  2  3  4  5  6  7  8  9  10 11
    // resultBytes



    // Now we're ready to send resultBytes across the network!!
    return resultBytes;
    #endregion
  }

  /// <summary>
  /// Takes an array of bytes and converts its
  /// first (Size) bytes into a Message struct.
  /// </summary>
  /// <param name="theBytes">Byte array to create the Message out of</param>
  /// <returns>A Message structure instance</returns>
  public static Message FromBytes( byte[] theBytes )
  {
    #region FromBytes explained
    // The purpose of this function is to RECREATE a
    // Message struct FROM an array of bytes
    // that would have originated at the other player's PC.

    // The other player woulda called GetBytes( onSomeMessageStruct ) ;
    // and so this function receives something like:

    //
    // |01|00|00|00|55|99|77|22|10|10|19|18|
    // |__|__|__|__|__|__|__|__|__|__|__|__|
    //  0  1  2  3  4  5  6  7  8  9  10 11
    // theBytes  ( this is copied from the GetBytes method )


    // So, now we need to turn theBytes into a Message object!
    // We're basically doing the opposite of what
    // we did in GetBytes().
    Message theMessage = new Message();

    // c is a counter variable.  As I read in values from
    // theBytes, I advance by count pointer forward 4 bytes each
    // time (all of these are int32's, which are 4 bytes in size each.)
    int c = 0;
    theMessage.playerNumber = BitConverter.ToInt32( theBytes, c );

    // Here's what we just did:

    // interpret 4 bytes starting from byte 0 as an int value
    // and cram it into playerNumber

    // <-        ->
    // |01|00|00|00|55|99|77|22|10|10|19|18|
    // |__|__|__|__|__|__|__|__|__|__|__|__|
    //  0  1  2  3  4  5  6  7  8  9  10 11
    // theBytes  ( this is copied from the GetBytes method )

    //    vv   BIT CONVERTER   vv

    // |   1    |
    // |________|
    //
    // playerNumber



    // advance counter pointer
    c += 4;



    theMessage.frame = BitConverter.ToInt32( theBytes, c );

    //              <-        ->
    // |01|00|00|00|55|99|77|22|10|10|19|18|
    // |__|__|__|__|__|__|__|__|__|__|__|__|
    //  0  1  2  3  4  5  6  7  8  9  10 11
    // theBytes  ( this is copied from the GetBytes method )

    //    vv   BIT CONVERTER   vv

    // | 50000  |
    // |________|
    //
    // frameNumber

    // (or something like this.  Note that the 50000 number isn't
    //  the binary representation of 55997722, this is
    //  just an example with numbers selected for
    //  visual appeal :)


    // advance counter
    c += 4;

    // although underlying type is (int), we immediately
    // cast to (NetMessageCommand) enum type to INTERPRET
    // that int value as one of the values in the enum.
    theMessage.cmd = (NetMessageCommand)BitConverter.ToInt32( theBytes, c );
    //                          <-        ->
    // |01|00|00|00|55|99|77|22|10|10|19|18|
    // |__|__|__|__|__|__|__|__|__|__|__|__|
    //  0  1  2  3  4  5  6  7  8  9  10 11
    // theBytes  ( this is copied from the GetBytes method )

    //    vv   BIT CONVERTER   vv

    // | 422222  |
    // |_________|
    // cmd

    // (again, 422222 isn't binary interp of 10101918, just
    // an example)

    return theMessage;
    #endregion
  }

  // for debug.
  public override string ToString()
  {
    return "Player=" + playerNumber + " Frame=" + frame + " cmd=" + cmd;
  }

  // for debug, outputting message queue as a graphical linked list, kind of
  public string DebugLine( int line )
  {
    // want to see message queue, output like:
    // |          1 ||          1 |
    // |         99 ||        100 |
    // | RotateLeft || RotateLeft |
    int len = cmd.ToString().Length; // cmd will be the longest string, unless you've reached insane frame count

    if( line == 1 )
      return "| " + playerNumber.ToString().PadRight( len ) + " |";
    else if( line == 2 )
      return "| " + frame.ToString().PadRight( len ) + " |";
    else if( line == 3 )
      return "| " + cmd.ToString().PadRight( len ) + " |";
    else
      return "error man, only pass me 1 2 or 3";
  }

  // so can be sorted by .Sort()
  public int CompareTo( object obj )
  {
    #region sortable
    // I said above this struct IMPLEMENTS the IComparable interface,
    // what the heck does that mean?

    // Well, when you IMPLEMENT an interface, you basically are
    // making a PROMISE to provide an "implementation" of the
    // function set that interface requires you to provide.

    // So what functions DO the IComparable interface
    // require you to provide?  Just one.  This one.  CompareTo.



    // What does CompareTo do?  It specifies the rules for
    // comparing one Message object to another.  It just tells,
    // if you have two Message struct objects, which one
    // should go in front of the other when they are ordered.

    // The most important thing about CompareTo, is
    // notice how it RETURNS AN INT.

    // Returning a negative value means 'this' object
    // belongs in front of 'other' when sorted.

    // The WHOLE POINT of this function is to
    // return a negative number if 'this' belongs
    // before 'other', and a positive number if
    // 'this' belongs AFTER 'other'.

    // So to recap, your MAIN JOB in this function is to
    // returning an int:

    //   A --negative value-- when 'this' object belongs BEFORE 'other'.
    //   A ++ positive value ++ when 'this' object belongs AFTER 'other'
    //   Exactly 0 when 'this' is considered to be equal to 'other'

    Message other = (Message)obj;

    // Remember, comparing 2 message objects here.
    // 'this' refers to one of them, and 'other' is
    // the other message you're comparing it to.
    if( this.frame == other.frame )
    {
      // frame numbers equal.  3 possibilities:
      /*
       
       A)  all other info different, player and cmd:
       
       | player   1  |     | player   2  |
       | frame   99  |     | frame   99  |
       | cmd      7  |     | cmd      5  |
       |_____________|     |_____________|
            this               other
        
       
       B)  or, same player, different cmd
        
       | player   1  |     | player   1  |
       | frame   99  |     | frame   99  |
       | cmd      7  |     | cmd      5  |
       |_____________|     |_____________|
            this               other
       
       
       C)  or, same cmd, different player
        
       | player   1  |     | player   2  |
       | frame   99  |     | frame   99  |
       | cmd      5  |     | cmd      5  |
       |_____________|     |_____________|
            this               other
       

       D)  [[ or, both (this last one is actually not possible, because of how messages get sent, and
              anyway, these are completely equal, so we'll just return 0 for this case) ]]
       | player   1  |     | player   1  |
       | frame   99  |     | frame   99  |
       | cmd      5  |     | cmd      5  |
       |_____________|     |_____________|
            this               other
      
      
      */


      // A)  all other info different, player and cmd:
      if( this.playerNumber != other.playerNumber && this.cmd != other.cmd )
      {
        // we'll sort based on player number.  Let player 1 go first,
        // always.
        return this.playerNumber - other.playerNumber;
        // if this is player 1's message, and other is player 2's, we'd get:
        // 1 - 2 = -1         ( player 1's messages go before player 2 )

        // if this is player 2's message, and other is player 1's, we'd get:
        // 2 - 1 = +1         ( player 2's messages go AFTER player 1 )
      }
      // B)  or, same player, different cmd
      else if( this.playerNumber == other.playerNumber && this.cmd != other.cmd )
      {
        // trying to order two messages from 
        // the same player, with the same frame stamp

        // so order based on value of cmd,
        // lower values of cmd executed first.
        // remember, cmd really is just an INT
        // in its underlying implementation.
        return this.cmd - other.cmd;

        // 2 - 5 == -3     - cmd with value 2 goes first
        // 5 - 2 == +3     - cmd with value 5 goes AFTER
      }
      // C)  or, same cmd, different player
      else if( this.cmd == other.cmd && this.playerNumber != other.playerNumber )
      {
        // Here, same frame stamp, and same cmd.

        // So elect to let player 1's messages
        // be executed first.

        return this.playerNumber - other.playerNumber;
        // if this is player 1's message, and other is player 2's, we'd get:
        // 1 - 2 = -1         ( player 1's messages go before player 2 )

        // if this is player 2's message, and other is player 1's, we'd get:
        // 2 - 1 = +1         ( player 2's messages go AFTER player 1 )
      }
      // D)  [[ or, both (this last one is actually not possible, because of how messages get sent, and
      //        anyway, these are completely equal, so we'll just return 0 for this case) ]]
      else
      {
        // The message structs are completely equal, so just return 0,
        // which basically means "they are equal"
        return 0;
      }
    }
    else
    {
      // different frame numbers
      /*
       
       possibly different player numbers and cmds, but none of
       that matters, because the item with the lower frame number
       will ALWAYS go first.
       
       | player   1  |     | player   2  |
       | frame   98  |     | frame   99  |
       | cmd      7  |     | cmd      5  |
       |_____________|     |_____________|
            this               other
       */

      return this.frame - other.frame;

      // if 'this' is a frame stamped 98 message, and 'other' is frame 99, we get:
      //
      // this.frame - other.frame
      // = 98 - 99
      // = -1         ( frame 98 (this) goes before frame 99 ('other') )

      // if 'this' is a frame stamped 99 message, and 'other' is frame 98, we get:
      //
      // this.frame - other.frame
      // = 99 - 98
      // = +1         ( frame 99 (this) goes AFTER frame 98 ('other') )
    }

    //////
    // BUT WHY???

    // By providing this function, and implementing this IComparable interface,
    // .NET will be able to order an arrayful of Message struct
    // objects, AUTOMATICALLY, and according to the rules we just
    // defined above, simply by us calling .Sort() on a List full of Messages.
    // This is what we do in the process message routine, actually, to ensure
    // that the messages are processed in order.
    #endregion
  }

}

