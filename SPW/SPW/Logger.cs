#region using...
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
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

/// <summary>
/// Logs messages.  To change output device,
/// use LogTo member.
/// </summary>
public class Logger : DrawableGameComponent
{
  public OutputDevice LogTo; // Where to push output to.
  // Can be more than one place, e.g. ( OutputDevice.Screen | OutputDevice.Console )
  // pushes to both.

  /// <summary>
  /// If true, new messages pour in at the TOP of the screen,
  /// instead of being tacked onto the bottom.
  /// 
  /// Set to true if you have A LOT of messages pouring out.
  /// </summary>
  public bool newMessagesAtTop;

  #region enabledness
  /// <summary>
  /// Set via Disable() and Enable() functions.
  /// If someone calls Disable() somewhere, then
  /// log WILL NOT APPEND messages.  It will still
  /// display the ones its already got, but
  /// it won't accumulate anymore.
  /// </summary>
  private bool enabled;
  public void Disable()
  {
    enabled = false;
  }
  public void Enable()
  {
    enabled = true;
  }
  // The only reason I'm using public functions and 
  // not exposing the "bool enabled" variable
  // as a public variable is because I like the look of
  //    logger.Disable()
  
  // vs
  //    logger.enabled = false ;

  // The .Disable() syntax is much clearer what the 
  // code is doing.
  #endregion

  private List<LogMessage> history; // back log of
  // log messages.  Kept so can be displayed on screen
  // for a few seconds before being destroyed.

  private SpriteBatch sb;
  private SpriteFont sf;

  #region file out related
  // file handle stuff.
  private TextWriter logfileHandle;
  public string Filename ;
  //

  
  public Logger( Game g, bool doAppend )
    : base( g )
  {
    history = new List<LogMessage>();

    newMessagesAtTop = false ;
    enabled = true ;

    // By default, output to console, screen and file.
    // This can be changed at any time.
    LogTo = OutputDevice.Console | OutputDevice.Screen | OutputDevice.File;

    // Always open the output file
    openLogFile( doAppend );
  }


  private void openLogFile( bool doAppend )
  {
    // print start header
    DateTime now = DateTime.Now;

    Filename = SPW.path + "GameLog_" + CurrentTimestamp + ".txt";
    
    // open.
    logfileHandle = new StreamWriter( Filename, doAppend );  // yes, DO append to end of file

    // make thread-safe by using a synchronized wrapper
    // around our logfileHandle object instead of
    // logfileHandle object directly
    logfileHandle = StreamWriter.Synchronized( logfileHandle ) ;

    logfileHandle.WriteLine();
    logfileHandle.WriteLine( "[" + now.ToLongDateString() + " " + now.ToLongTimeString() + "] [" + LogMessageType.Info + "] Startup" );
    
  }

  protected override void LoadContent()
  {
    sb = new SpriteBatch( this.GraphicsDevice );

    try
    {
      sf = this.Game.Content.Load<SpriteFont>( "loggerFont" );
    }
    catch( Exception e )
    {
      throw new Exception( "You ninny!  You must provide a SpriteFont called \"loggerFont\" for the Logger engine to use!\n\n" + e.Message );
    }

    base.LoadContent();
  }

  /// <summary>
  /// Logs a message to the selected OutputDevices
  /// (Screen, Console, File)
  /// </summary>
  /// <param name="message">The message to log, ninny</param>
  /// <param name="type">The type of message this is</param>
  /// <param name="where">Where to dump it</param>
  /// <param name="doTimeStamp">Whether or not to include a timestamp</param>
  public void Log( string message, LogMessageType type, OutputDevice where, bool doTimestamp )
  {
    StringBuilder msg = new StringBuilder();

    if( doTimestamp )
    {
      DateTime now = DateTime.Now;
      msg.Append( "[" + now.ToString( "HH:mm:ss" ) + "] " + "["+SPW.currentFrame+"] "/*Spw: add the frame count stamp*/ );
    }

    msg.Append( "[" + type + "] " );
    msg.Append( message );

    string annotatedMsg = msg.ToString();
    #region log dumping
    if( EnumHelper<OutputDevice>.ContainsFlag( where, OutputDevice.Console ) )  // if the Console flag is set
      Console.WriteLine( annotatedMsg );
    if( EnumHelper<OutputDevice>.ContainsFlag( where, OutputDevice.Screen ) )
    {
      // Create a LogMessage object, which is just the struct type
      // that keeps all info about the screen-displayable message
      LogMessage logMessage = new LogMessage( annotatedMsg ) ;

      // Now alter the color depending on the message type
      if( type == LogMessageType.Error )
        logMessage.color = Color.Red ;
      else if( type == LogMessageType.Warning )
        logMessage.color = Color.Yellow;
      else
        logMessage.color = Color.Gray ;
      
      // make translucent, so log messages aren't overly intrusive
      //logMessage.color.A = 100 ;

      // lock-down the history array, in case
      // this function is called from multiple threads
      // we can't have multiple threads accessing the
      // history List at the same time else it will
      // get corrupted and throw an exception
      lock( this.history )
      {
        // This is protection for the logging system from being overflooded
        if( history.Count == 15 )
        {
          LogMessage overMsg = new LogMessage( "There were more... but they're not being displayed (too many messages).  See log file." ) ;
          history.Add( overMsg );
        }
        else if( history.Count < 15 )
        {
          // Construct a LogMessage object and add it to the history.
          history.Add( logMessage );
          // history is how the drawing routine knows what to draw.
          // if not in history, never drawn.
        }
        else
        {
          // you're over, so don't display anything
        }
      }
    }
    if( EnumHelper<OutputDevice>.ContainsFlag( where, OutputDevice.File ) )
    {
      logfileHandle.WriteLine( annotatedMsg );
      logfileHandle.Flush();  // in case of crash, we should flush often
      // so no data is lost.
    }
    if( EnumHelper<OutputDevice>.ContainsFlag( where, OutputDevice.Diagnostics ) )
      System.Diagnostics.Debug.WriteLine( annotatedMsg );
    #endregion
  }

  /// <summary>
  /// Gives current timestamp as a string with format
  /// Mar_10_09__10_59_59_050600
  /// </summary>
  public static string CurrentTimestamp
  {
    get
    {
      DateTime now = DateTime.Now;
      return now.ToString( "MMM_dd_yy__HH_mm_ss_ffffff" );
    }
  }


  #region convenient overloads for Log()
  /// <summary>
  /// Logs a message to the ALREADY selected OutputDevices
  /// in the "LogTo" (Screen, Console, File etc)
  /// </summary>
  /// <param name="message">The message to log, ninny</param>
  /// <param name="type">The type of message this is</param>
  /// <param name="where">Where to dump it.  E.g.
  /// can combine File and Screen, by using (OutputDevice.File | OutputDevice.Screen) </param>
  public void Log( string message, LogMessageType type, OutputDevice where )
  {
    Log( message, type, where, true );
  }

  public void Log( string message, LogMessageType type )
  {
    Log( message, type, LogTo, true );
  }

  public void Error( string message )
  {
    Log( message, LogMessageType.Error, LogTo, true );
  }

  public void Warn( string message )
  {
    Log( message, LogMessageType.Warning, LogTo, true );
  }

  public void Info( string message )
  {
    Log( message, LogMessageType.Info, LogTo, true );
  }
  #endregion
  #endregion

  #region on-screen message-display related stuff

  /// <summary>
  /// Resets life of each message in history to full
  /// </summary>
  public void Refresh()
  {
    lock( this.history )
    {
      foreach( LogMessage lm in history )
      {
        lm.life = 10.0f ;
      }
    }
  }

  public void DeleteOldestMessage()
  {
    lock( this.history )
    {
      if( history.Count > 0 )
        history.RemoveAt( 0 ) ;
    }
  }

  public void DeleteNewestMessage()
  {
    lock( this.history )
    {
      if( history.Count > 0 )
        history.RemoveAt( history.Count - 1 ) ;
    }
  }

  public override void Update( GameTime gameTime )
  {
    lock( this.history )
    {
      for( int i = history.Count - 1; i >= 0; i-- )
      {
        LogMessage lm = history[ i ];

        lm.ReduceLife( (float)gameTime.ElapsedGameTime.Ticks / TimeSpan.TicksPerSecond );

        if( lm.life < 0 ) // then kill this message
          history.RemoveAt( i );
      }
      base.Update( gameTime );
    }
  }

  public override void Draw( GameTime gameTime )
  {
    // DRAW ONLY IF LOG IS ENABLED
    if( this.enabled )
    {
      lock( this.history )
      {
        sb.Begin( SpriteBlendMode.AlphaBlend );
        GraphicsDevice.RenderState.SourceBlend = Blend.InverseDestinationColor;

        float xIndent = 20.0f;

        for( int i = 0; i < history.Count; i++ )
        {
          LogMessage lm = history[ i ];

          float y ;

          
          if( newMessagesAtTop )
            y = 20 + ( 20 * ( history.Count - i - 1 ) );  //message add-in at top of list ( don't like )
          else
            y = 20 + ( 20 * i ); //message add-in at bottom of list (looks better, but shifts when they disappear).
          

          sb.DrawString( sf,
                         lm.message,
                         new Vector2( xIndent, y ),
                         lm.color );
        }

        sb.End();
      }
    }
    base.Draw( gameTime );
  }


  #endregion

  /// <summary>
  /// Its important that this get called 
  /// when the application is exiting.
  /// </summary>
  public void Shutdown()
  {
    Log( "Shutdown", LogMessageType.Info );

    logfileHandle.Flush();
    logfileHandle.Close();
  }
}




/// <summary>
/// Specifically logs directly to a file.  You should use this class
/// when you only want to see output to a file and not to the screen
/// or console at all.
/// 
/// You can create multiple instances of this class in case you'd like
/// to log several things at once, but you don't want the output
/// all combobled together in one file (e.g. give each thread its own
/// log)
/// </summary>
public class FileLogger
{
  // file handle stuff.
  private TextWriter logfileHandle;
  //
  public string Filename ;

  #region enabling / disabling
  private bool enabled;
  public void Disable()
  {
    enabled = false;
  }
  public void Enable()
  {
    enabled = true;
  }
  //    fileLog.Disable() ;
  // is more clear/readable than
  //    fileLog.enabled = false ;
  // otherwise I would have made the
  // enabled property public, I assure you
  #endregion

  // Calls ol ctor below
  public FileLogger() : this( null, false ) { }

  public FileLogger( string filename, bool doAppend )
  {
    // Always open the output file right away
    openLogFile( filename, doAppend );

    // set to enabled
    enabled = true;
  }

  /// <summary>
  /// Gives current timestamp as a string with format
  /// Mar_10_09__10_59_59_050600
  /// </summary>
  public static string CurrentTimestamp
  {
    get
    {
      DateTime now = DateTime.Now;
      return now.ToString( "MMM_dd_yy__HH_mm_ss_ffffff" ) ;
    }
  }

  private void openLogFile( string i_filename, bool doAppend )
  {
    DateTime now = DateTime.Now;

    // If the person didn't pass in a file name to the ctor,
    // use the default generic log_timestamp.txt filename.
    if( i_filename == null )
      Filename = SPW.path + "log_" + CurrentTimestamp + ".txt";
    else
      Filename = i_filename;

    // open the file, and make it thread-safe while you're at it.  http://msdn.microsoft.com/en-us/library/system.io.textwriter.synchronized.aspx
    logfileHandle = StreamWriter.Synchronized( new StreamWriter( Filename, doAppend ) );

    // Write out a blank line, then the header
    logfileHandle.WriteLine();
    logfileHandle.WriteLine( "[" + now.ToLongDateString() + " " + now.ToLongTimeString() + "] [" + LogMessageType.Info + "] Startup" );

  }

  /// <summary>
  /// Logs a message
  /// </summary>
  /// <param name="message">The message to log</param>
  /// <param name="type">The type of message it is</param>
  /// <param name="doTimestamp">Whether or not to include a timestamp</param>
  public void Log( string message, LogMessageType type, bool doTimestamp )
  {
    // DO NOT APPEND MESSAGES IF THE LOG IS DISABLED
    if( this.enabled == false )
      return;

    StringBuilder msg = new StringBuilder();

    if( doTimestamp )
    {
      DateTime now = DateTime.Now;
      msg.Append( "[" + now.ToString( "HH:mm:ss" ) + "] " + "[" + SPW.currentFrame + "] " );
    }

    msg.Append( "[" + type + "] " );
    msg.Append( message );

    string annotatedMsg = msg.ToString();
    logfileHandle.WriteLine( annotatedMsg );
    logfileHandle.Flush();  // in case of crash, we should flush often
    // so no data is lost.
  }

  #region convenient overloads for Log() that just end up calling the single Log() method provided above
  public void Log( string message, LogMessageType type )
  {
    Log( message, type, true );
  }

  public void Info( string message )
  {
    Log( message, LogMessageType.Info, true );
  }

  public void Warn( string message )
  {
    Log( message, LogMessageType.Warning, true );
  }

  public void Error( string message )
  {
    Log( message, LogMessageType.Error, true );
  }
  #endregion

  public void Shutdown()
  {
    Info( "Shutdown" );

    logfileHandle.Flush();
    logfileHandle.Close();
  }

}




#region LogMessage data types
public enum LogMessageType
{

  Info,
  Warning,
  Error

}

// Combinable enum, like C enums:
// OutputDevice.Screen | OutputDevice.File means
// we want to print to both a file and the screen.
[Flags]
public enum OutputDevice
{

  Screen = 1,           // binary 1
  Console = 2,          // 10
  ScreenAndConsole = 3, // 11
  Diagnostics = 4,      // 100
  File = 8,             // 1000

  /// <summary>
  /// Output to both the screen and the log file
  /// </summary>
  ScreenAndFile = 9,    // 1001
  
  ALL = 15              // 1111

};


public class LogMessage
{
  public string message;

  // Drawable ones have color
  public Color color;
  public float life;
  public float fadeTime ;

  public static float DEFAULT_FADETIME = 5.0f; // how many seconds to fade out over
  public static float DEFAULT_LIFETIME = 20.0f; // default number of seconds for new messages to live for

  // public Vector2 pos; // Removed.  Actually doesn't know where it is.
  // Let logger's draw procedure figure that out.

  /// <param name="i_message">The message to log</param>
  public LogMessage( string i_message )
  {
    message = i_message;
    color = Color.White;
    life = DEFAULT_LIFETIME;
    fadeTime = DEFAULT_FADETIME ;
  }

  public void ReduceLife( float by )
  {
    life -= by;

    // set alpha component of color equal to 
    // life left over.  this makes the text fade out
    // over its last second of display.

    if( life < fadeTime )
      color.A = (byte)( ( life / fadeTime ) * 255.0f );
  }
}

#endregion


#region enumhelper
/// <summary>
/// Extension class to Enum
/// </summary>
/// <typeparam name="T">The type of Enum you want to parse</typeparam>
public class EnumHelper<T>
{
  /// <summary>
  /// Gives you the a strongly-typed instance
  /// of <typeparamref name="T"/> corresponding to the string.
  /// </summary>
  /// <param name="val">String you want to parse</param>
  /// <returns>Instance of type <typeparamref name="T"/></returns>
  public static T Parse( string val )
  {
    return (T)System.Enum.Parse( typeof( T ), val );
  }

  /// <summary>
  /// Tells you if an enum contains a certain flag or not.
  /// Uses bitwise, so THIS ONLY WORKS if the enum is intended to be used this way,
  /// e.g. if the values are powers of 2 like 1, 2, 4, 8, 16, 32, etc.
  /// </summary>
  /// <param name="valueToCheck">The value you want to see if it contains a certain flag or not</param>
  /// <param name="flagToCheckFor">That "certain flag" you want to check for</param>
  /// <returns>True or false, ninny.</returns>
  public static bool ContainsFlag( T valueToCheck, T flagToCheckFor )
  {
    object val = valueToCheck;
    object flag = flagToCheckFor;

    int v = (int)val;
    int f = (int)flag;

    return ( ( v & f ) == f );
  }
}
#endregion




