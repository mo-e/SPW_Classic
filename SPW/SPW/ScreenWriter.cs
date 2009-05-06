using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;



/// <summary>
/// A StringItem represents some text on the screen
/// with its own position and lifetime to display for
/// </summary>
public class StringItem
{
  public enum Centering
  {
    Vertical = 1,    // binary 01
    Horizontal = 2,  // binary 10
    Both = 3         // binary 11, so is bitwise OR of Vertical | Horizontal
  }

  /// <summary>
  /// The color the String should start at
  /// </summary>
  private Color initColor;

  /// <summary>
  /// The color the String should fade out to
  /// as it comes to the end of its life
  /// </summary>
  private Color finalColor;

  /// <summary>
  /// The number of seconds to display the String item for
  /// </summary>
  public float life;

  /// <summary>
  /// Fades out or not
  /// </summary>
  public bool fades;

  /// <summary>
  /// The actual text of the string to display
  /// </summary>
  public string message;

  /// <summary>
  /// WHERE on the screen to display it
  /// </summary>
  public Vector2 pos;


  /// <summary>
  /// Whether or not to actually DRAW IT.  Once
  /// a StringItem's life is up, it gets deactivated
  /// (but not deleted, so it can be revived in case
  /// user missed seeing the message)
  /// </summary>
  public bool isActive;


  public static float DEFAULT_LIFETIME = 4.0f;
  public static Color DEFAULT_START_COLOR = Color.White;
  public static Color DEFAULT_END_COLOR = Color.TransparentWhite;

  public StringItem()
  {
    isActive = false;
  }

  #region positioned constructors
  public StringItem( string msg, int x, int y ) : this( msg, x, y, DEFAULT_LIFETIME, DEFAULT_START_COLOR, DEFAULT_END_COLOR ) { }

  /// <summary>
  /// Displays a message on the screen
  /// </summary>
  /// <param name="msg">Message to display</param>
  /// <param name="x">Where-x in screen coordinates</param>
  /// <param name="y">Where-y in screen coordinates</param>
  /// <param name="lifeTime">Amount of time to display for before fading away.  Pass __0__ for NO FADE.</param>
  public StringItem( string msg, int x, int y, float lifeTime ) : this( msg, x, y, lifeTime, DEFAULT_START_COLOR, DEFAULT_END_COLOR ) { }

  /// <summary>
  /// Displays a message on the screen
  /// </summary>
  /// <param name="msg">Message to display</param>
  /// <param name="x">Where to display it in x</param>
  /// <param name="y">Where to display it in y</param>
  /// <param name="lifeTime">Amount of time to display for before fading away.  Pass __0__ for NO FADE.</param>
  /// <param name="startColor">The color of the message</param>
  public StringItem( string msg, int x, int y, float lifeTime, Color startColor ) : this( msg, x, y, lifeTime, startColor, DEFAULT_END_COLOR ) { }

  /// <summary>
  /// Displays a message on the screen wherever you want it, in pixel coordinates.
  /// </summary>
  /// <param name="msg">The message to display</param>
  /// <param name="x">Where to display it in x</param>
  /// <param name="y">Where to display it in y</param>
  /// <param name="lifeTime">Number of seconds to display for.  If you pass 0, then the item will display for exactly 1 frame and will not fade out.</param>
  /// <param name="startColor">Starting color</param>
  /// <param name="fadeToColor">End color when fade out</param>
  public StringItem( string msg, int x, int y, float lifeTime, Color startColor, Color fadeToColor )
  {
    message = msg;
    pos = new Vector2( x, y );
    life = lifeTime;
    initColor = startColor;
    finalColor = fadeToColor;

    if( lifeTime == 0 )
      fades = false;
    else
      fades = true;

    isActive = true;
  }
  #endregion

  #region centered constructors
  public StringItem( string msg )
    : this( msg, Centering.Both, 0, DEFAULT_LIFETIME, DEFAULT_START_COLOR, DEFAULT_END_COLOR ) { }

  public StringItem( string msg, Color startColor )
    : this( msg, Centering.Both, 0, DEFAULT_LIFETIME, startColor, DEFAULT_END_COLOR ) { }

  public StringItem( string msg, Color startColor, float lifeTime )
    : this( msg, Centering.Both, 0, lifeTime, startColor, DEFAULT_END_COLOR ) { }

  public StringItem( string msg, Color startColor, Color endColor )
    : this( msg, Centering.Both, 0, DEFAULT_LIFETIME, startColor, endColor ) { }

  public StringItem( string msg, Centering how )
    : this( msg, how, 0, DEFAULT_LIFETIME, DEFAULT_START_COLOR, DEFAULT_END_COLOR ) { }

  /// <summary>
  /// Displays a message, centered either vertically or horizontally in the screen.
  /// </summary>
  /// <param name="msg">Message string to display</param>
  /// <param name="how">What type of centering do you want?  You can choose BOTH, you know</param>
  /// <param name="otherCoordValue">Value of for axis you are NOT trying to center</param>
  public StringItem( string msg, Centering how, int otherCoordValue )
    : this( msg, how, otherCoordValue, DEFAULT_LIFETIME, DEFAULT_START_COLOR, DEFAULT_END_COLOR ) { }

  public StringItem( string msg, Centering how, int otherCoordValue, float lifeTime )
    : this( msg, how, otherCoordValue, lifeTime, DEFAULT_START_COLOR, DEFAULT_END_COLOR ) { }

  public StringItem( string msg, Centering how, int otherCoordValue, float lifeTime, Color color )
    : this( msg, how, otherCoordValue, lifeTime, color, DEFAULT_END_COLOR ) { }

  /// <summary>
  /// Displays a message, centered either vertically or horizontally in the screen
  /// </summary>
  /// <param name="msg"></param>
  /// <param name="how"></param>
  /// <param name="otherCoordValue"></param>
  /// <param name="lifeTime"></param>
  /// <param name="startColor"></param>
  /// <param name="endColor"></param>
  public StringItem( string msg, Centering how, int otherCoordValue, float lifeTime, Color startColor, Color endColor )
  {
    // x and y to use
    int x, y;
    if( how == Centering.Horizontal )
    {
      // he only wants it horizontally centered.
      x = ScreenWriter.GetCenteredX( msg );

      // the coordValue he passed must be intended value for 'y'
      y = otherCoordValue;
    }
    else if( how == Centering.Vertical )
    {
      // center vertically only
      x = otherCoordValue;
      y = ScreenWriter.GetCenteredY( msg );
    }
    else
    {
      // center both, other coordinate value is ignored.
      x = ScreenWriter.GetCenteredX( msg );
      y = ScreenWriter.GetCenteredY( msg );
    }

    // now make the string item
    message = msg;
    pos = new Vector2( x, y );
    life = lifeTime;
    initColor = startColor;
    finalColor = endColor;
    if( lifeTime == 0 )
      fades = false;
    else
      fades = true;
    isActive = true;
  }
  #endregion


  public Color Color
  {
    get
    {
      if( fades && life < 1 )
        return new Color( Vector4.Lerp( finalColor.ToVector4(), initColor.ToVector4(), life ) );
      else
        return initColor;
    }
  }

  /// <summary>
  /// Provides debug spew of this StringItem
  /// </summary>
  /// <returns>String with data about this instance</returns>
  public override string ToString()
  {
    return base.ToString() +
      ": '" + this.message + "' " +
      ( this.isActive ? "is" : "is not" ) + " active " +
      ( this.life < 0 ? "is dead " : "has " + this.life + " left " ) +
      "positioned at " + this.pos.ToString();
  }
}



public class ScreenWriter : DrawableGameComponent
{
  private volatile Dictionary<string, StringItem> history;

  private SpriteBatch sb;

  public static SpriteFont font;
  public static Game game;

  #region toggle enabledness
  /// <summary>
  /// Set via Disable() and Enable() functions.
  /// If someone calls Disable() somewhere, then
  /// log WILL NOT APPEND messages.  It will still
  /// display the ones its already got, but
  /// it won't accumulate anymore.
  /// </summary>
  private bool enabled;
  public bool IsEnabled
  {
    get { return enabled; }
  }
  public void Disable()
  {
    enabled = false;
  }
  public void Enable()
  {
    enabled = true;
  }
  #endregion

  public ScreenWriter( Game g )
    : base( g )
  {
    this.enabled = true;

    game = g;

    // initialize the "history" object (which is just
    // a collection of all the strings being displayed
    // on the screen at the present time)

    history = new Dictionary<string, StringItem>();
  }

  protected override void LoadContent()
  {
    sb = new SpriteBatch( this.GraphicsDevice );
    try
    {
      // Use the Content object of the Game class (the SPW class)
      // that this GameComponent belongs to try and load a font
      font = this.Game.Content.Load<SpriteFont>( "screenwriterFont" );
    }
    catch( Exception e )
    {
      // You have to supply a font called screenWriterFont in your project
      // for the ScreenWriter object to draw its text with.

      // (like, you must right click the "Content" folder, Add New Item..
      // SpriteFont item and CALL IT screenwriterFont)
      throw new Exception( "You ninny!  You must provide a SpriteFont called \"screenwriterFont\" for the ScreenWriter engine to use!\n\n" + e.Message );
    }

    base.LoadContent();
  }

  public static int GetCenteredX( string msg )
  {
    Vector2 strDims = font.MeasureString( msg );
    return (int)( ( game.GraphicsDevice.PresentationParameters.BackBufferWidth - strDims.X ) / 2 );
  }

  public static int GetCenteredY( string msg )
  {
    Vector2 strDims = font.MeasureString( msg );
    return (int)( ( game.GraphicsDevice.PresentationParameters.BackBufferHeight - strDims.Y ) / 2 );
  }

  /// <summary>
  /// Adds a new StringItem to the collection
  /// of StringItems to display on the screen.
  /// </summary>
  /// <param name="id">The ID of the string to display
  /// (this is NOT what gets shown.. this is just how
  /// you can overwrite /erase a string that's 
  /// already on the screen)</param>
  /// <returns>The string item object referenced
  /// by the ID you are passing</returns>
  public StringItem this[ string id ]
  {
    get
    {
      lock( this.history )
      {
        if( history.ContainsKey( id ) )
          return history[ id ];
        else
          return null;
      }

    }
    set
    {
      lock( this.history )
      {
        // if a StringItem by this id is already there...
        if( history.ContainsKey( id ) )
          history[ id ] = value; //...then overwrite it
        else
          history.Add( id, value ); //...add for the first time
      }
    }
  }


  // "Steps" each StringItem in the history
  // collection forward in time.
  // "Stepping forward" for a StringItem just
  // decreases its lifetime by some incremental amount
  public override void Update( GameTime gameTime )
  {
    lock( this.history )
    {
      foreach( StringItem si in history.Values )
      {
        if( si.isActive )
        {
          if( si.life < 0 )
          {
            //deactivate it so it stops displaying
            si.isActive = false;

            // The reason the StringItems aren't removed is
            // so that they can be re-activated in case the
            // user missed the message and wants to see it again
          }

          // reduce life left
          si.life -= (float)gameTime.ElapsedGameTime.Ticks / TimeSpan.TicksPerSecond;
        }
      }
    }
    base.Update( gameTime );
  }


  // Revives the last message that was last deactivated
  public void ReactivateLastDeactivated()
  {
    lock( this.history )
    {
      float leastDead = 0.0f;
      string leastDeadIndex = string.Empty;
      foreach( KeyValuePair<string, StringItem> pair in history )
      {
        if( pair.Value.isActive == false &&  // looking for deactivated
            pair.Value.life < leastDead )    // AND most recently deactivated
        {
          // this one is the least dead so far
          leastDead = pair.Value.life;

          // so remember it
          leastDeadIndex = pair.Key;
        }
      }

      if( leastDeadIndex != string.Empty )
      {
        // reactivate least dead.
        history[ leastDeadIndex ].life = 5.0f;
        history[ leastDeadIndex ].isActive = true;
      }
      else
      {
        Console.WriteLine( " I couldn't find any strings" );
      }
    }
  }


  // Draws all the ACTIVE StringItems in the history
  public override void Draw( GameTime gameTime )
  {
    // ONLY DRAW MESSAGES IF THE LOG IS ENABLED
    if( this.enabled == true )
    {
      lock( this.history )
      {
        sb.Begin( SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState );

        foreach( StringItem si in history.Values )
        {
          if( si.isActive )
            sb.DrawString( font, si.message, si.pos, si.Color );
        }

        sb.End();
      }
    }

    base.Draw( gameTime );
  }
}