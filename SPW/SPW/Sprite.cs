#region using...
using System;
using System.Collections.Generic;
using System.Linq;
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
/// A Sprite represents a visible game entity.
/// </summary>
public class Sprite
{
  public Vector2 position;

  // how wide this sprite should appear
  // when drawn on screen in pixels
  public int graphicalWidth;

  // how wide this sprite should appear
  // when drawn on the screen in pixels
  public int graphicalHeight;

  // the center of this sprite
  public Vector2 center;

  // The texture that should be used to draw
  // this Sprite
  public Texture2D tex;

  public Sprite()
  {

  }

  // Gives you an approximate radius by
  // using the average of the width and height
  public float GetApproxRadius()
  {
    // get the average of the width and height
    float avgWH = ( this.graphicalHeight + this.graphicalWidth ) / 2;

    // now just say "approximate radius" is just half that
    return avgWH / 2;
  }

  public Sprite( Texture2D texture, float x, float y, int w, int h )
  {
    tex = texture;
    position.X = x;
    position.Y = y;
    graphicalWidth = w;
    graphicalHeight = h;

    center = new Vector2( w / 2, h / 2 );
  }

  // Intersection method to tell if this sprite intersects another
  // default uses bounding spheres.
  public virtual bool Intersects( Sprite other )
  {
    // construct BoundingSpheres and see if the
    // two sprites should collide
    BoundingSphere thisSphere = new BoundingSphere(
      new Vector3( this.position.X, this.position.Y, 0.0f ), this.GetApproxRadius() );

    BoundingSphere thatSphere = new BoundingSphere(
      new Vector3( other.position.X, other.position.Y, 0.0f ), this.GetApproxRadius() );

    if( thisSphere.Intersects( thatSphere ) )
      return true;
    else
      return false;
  }
}


public class Particle : Sprite
{
  Ship owner;
  Vector2 spreadDir;

  public Particle()
  {
    // give each particle a random spread direction
    float spreadX = 0.5f - (float)SPW.rand.NextDouble();
    float spreadY = 0.5f - (float)SPW.rand.NextDouble();

    spreadDir = new Vector2( spreadX, spreadY );
  }

  public Particle( Ship shipOwner )
    : this()
  {
    owner = shipOwner;
  }

  public Vector2 getCurrentPosition( float spread )
  {
    return owner.position + spreadDir * spread;
  }
}





// A sprite that is intended to move over time.
public class MovingSprite : Sprite
{
  public Vector2 velocity;

  // rotation
  public float rot;

  // If it moves, it should have a step calculation function!
  public virtual void Step( float stepTime )
  {
    // Move the ship
    this.position += velocity;

    // wrap
    if( position.X < 0 )
      position.X = SPW.world.ScreenWidth;

    if( position.Y < 0 )
      position.Y = SPW.world.ScreenHeight;

    if( position.X > SPW.world.ScreenWidth )
      position.X = 0;

    if( position.Y > SPW.world.ScreenHeight )
      position.Y = 0;
  }
}




/// <summary>
/// Represents a torpedo
/// </summary>
public class Projectile : MovingSprite
{
  // maximum number of seconds this torpedo travels before
  // self destruct
  private static float MAX_TIME = 20.0f;

  // the damage a torpedo does to a ship
  private static float DAMAGE = 4.0f;

  // when a torpedo is first launched, it goes at this speed
  public static float NORMAL_START_SPEED = 2.5f;

  // amount of time this torpedo has left before self destruct
  private float lifeTime;

  // mark projectile as finished once it strikes a player or expires
  public bool dead;

  // keep a back reference to the player
  // that shot the projectile
  public Ship shooter;


  public Projectile()
  {
    // start lifeTime out @ max
    lifeTime = MAX_TIME;

    dead = false;

    // torpedos measure 7 by 12 - see the
    // world.CreateContent() method
    this.graphicalWidth = 12;
    this.graphicalHeight = 7;

    // because torpedos measure 7 by 12
    center = new Vector2( 3.5f, 6.0f );
  }

  public override void Step( float stepTime )
  {
    // a couple of things to be done before 
    // we call the BASE (MovingSprite)'s .Step()
    // function

    // If the torpedo has expired..
    if( this.lifeTime < 0.0f )
    {
      this.Strike( null ); // strike nothing, destroying itself
    }

    // take away from lifeTime remaining
    lifeTime -= stepTime;


    // call the base class function (MovingSprite.Step())
    // which is what actually moves the sprite forward a bit
    // in space.
    base.Step( stepTime );
  }

  public void Strike( Sprite which )
  {
    // which ship (or other torpedo!) are you hitting?
    if( which is Ship )
    {
      // damage the ship this torpedo hit
      ( which as Ship ).Damage( DAMAGE );
      SPW.world.sfx[ SFX.BlowUp ].Play();
    }
    else if( which is Projectile )
    {
      // destroy both this and the other one
      ( which as Projectile ).Destroy();
    }

    // kill this torpedo after striking another (whatever)
    this.Destroy();
  }

  // kills this torpedo and cleans up
  // by reducing numTorpedos in Air on shooter ship
  public void Destroy()
  {
    shooter.numTorpedosInAir--;

    // now remove itself from the collection of torpedos
    //SPW.world.torpedos.Remove( this ) ; // not allowed since
    // we are iterating over the collection WHEN this gets called
    this.dead = true;
  }
}




public class Ship : MovingSprite
{
  // player 1 or player 2
  public int playerNumber;

  // amount of power left (for firing weapons)
  public float energy;

  // shields remaining (basically hp)
  public float shield;

  // is this player still alive?
  public bool dead; // set when blow up sequence done,
  // signalling game over

  // used when go into hyperspace or explode.
  public List<Particle> particles;

  // Maximum value either energy or shield can increase to.
  private static float MAX_ENERGY = 120.0f;


  // Penalties for various things
  private static float PENALTY_CLOAK_PER_SEC = 2.0f, PENALTY_ENGINES_PER_SEC = 2.0f;
  private static float PENALTY_HYPERSPACE = 8.0f, PENALTY_PHASOR_SHOT = 1.0f, PENALTY_TORPEDO_SHOT = 1.0f;


  // torpedos
  public int numTorpedosInAir;
  public static int MAX_TORPEDOS_IN_AIR = 7;

  // whether has foot on throttle or not
  private bool thrusting;

  // bonus every 2 seconds
  private float timeSinceLastBonus;

  // player energy++ every 2 seconds
  private static float TIME_BETWEEN_ENERGY_BONUS = 2.0f;

  // Length of time hyperspace goes on for, and death sequence when player blows up
  public static float HYPERSPACE_LENGTH = 1.0f, DEATH_SEQUENCE_LENGTH = 1.0f;

  // what state is ship in?  Normal, Cloaking, Hyperspace, or BlowingUp
  public ShipState state;

  // Represents your phasor gun.  Tied to the ship,
  // so it doesn't have its own position or velocity.
  public Phasor phasor;

  // Amount of time left in hyperspace.
  // also used to draw the "cloud" of the player
  // when he is in hyperspace
  public float hyperspaceTimeRem, deathSequenceTimeRem;

  public Ship()
  {
    velocity = new Vector2();
    state = ShipState.Normal;
    dead = false;

    // total looks like about 160
    energy = MAX_ENERGY;
    shield = 40.0f;

    // create the particles that will be used
    // when the ship does hyperspace or explodes
    particles = new List<Particle>();
    for( int i = 0; i < 100; i++ )
    {
      particles.Add( new Particle( this ) );
    }

    // initialize the phasor
    phasor = new Phasor( this );
  }

  public override void Step( float stepTime )
  {
    switch( state )
    {
      case ShipState.Cloaking:

        // take energy down by penalty amount
        energy -= PENALTY_CLOAK_PER_SEC * stepTime;

        // there, we spent the energy for the cloak,
        // and we'll try to revert to normal state if was cloaking
        state = ShipState.Normal;

        //C# doesn't allow fall-thru
        //without goto, but the rest of the frame
        // computation is going to be the same as Normal state
        goto case ShipState.Normal;

      case ShipState.Normal:

        if( this.thrusting )
        {
          this.energy -= PENALTY_ENGINES_PER_SEC * stepTime;

          float thrusterPower = 0.5f;

          // Get a vector pointing in
          // the direction of the ship.
          Vector2 heading = GetHeading();

          // reduce effect a bit
          heading *= thrusterPower;

          // now influence the velocity vector with
          // this heading vector.  There, we spent the thrust,
          // so, now we should turn it off for the next frame.
          velocity += heading;

          // we can impose a maximum velocity here.
          if( velocity.Length() > 5.0f )
          {
            velocity.Normalize();

            // max velocity imposed.
            velocity *= 5.0f;
          }

          // we computed the thruster contribution
          // to velocity so now we can just turn
          // the thruster off.  It will appear
          // to be continuously on though if
          // the player keeps holding down the thruster key
          this.thrusting = false;
        }

        // run the Step function in MovingSprite,
        // which actually physically moves the player
        base.Step( stepTime );
        break;

      case ShipState.Hyperspace:

        // reduce the amount of time the player
        // has left in hyperspace
        hyperspaceTimeRem -= stepTime;

        // now, actually move the player using
        // the MovingSprite class's Step() function
        base.Step( stepTime );

        // get out of phasing mode if done hyperspacing
        if( hyperspaceTimeRem < 0 )
        {
          state = ShipState.Normal;

          // ship STOPS when done hyperspace
          velocity = Vector2.Zero;
        }

        break;

      case ShipState.BlowingUp:

        // reduce the amount of time the player
        // has left in his death sequence (just
        // particles flying)
        deathSequenceTimeRem -= stepTime;

        // once he has finished blowing up,
        // we set this.dead to true, signalling
        // the end fo the game (see in the Update function
        // there is a check if EITHER player is DEAD.

        // If either player is dead it goes back to the menu screen,
        // meaning game over.
        if( deathSequenceTimeRem < 0 )
        {
          this.dead = true;
        }

        break;
    }



    // step the phasor gun
    phasor.Step( stepTime );

    // Player gets a chance to recharge a bit every turn
    // auto-shield ++
    Recharge( stepTime );
  }

  private void Recharge( float stepTime )
  {
    // shields recharge
    timeSinceLastBonus += stepTime;

    // (( award energy bonus if 2.0s has passed ))
    if( timeSinceLastBonus >= TIME_BETWEEN_ENERGY_BONUS && energy < MAX_ENERGY )
    {
      energy++;
      timeSinceLastBonus = 0.0f;
    }
  }

  public void BeginHyperspace()
  {
    if( energy > PENALTY_HYPERSPACE )
    {
      // hyperspace costs 8 units of energy
      energy -= PENALTY_HYPERSPACE;

      // set hyperspace time to max hyperspace length
      // If you change HYPERSPACE_LENGTH to something
      // bigger or smaller, then hyperspace will last
      // longer or shorter.
      hyperspaceTimeRem = HYPERSPACE_LENGTH;

      state = ShipState.Hyperspace;

      velocity = new Vector2( (float)SPW.rand.NextDouble() * 5.0f, (float)SPW.rand.NextDouble() * 5.0f );

      // play the sound
      SPW.world.sfx[ SFX.Hyperspace ].Play();
    }
  }

  // Call this function to kill the player
  // in a !!DRAMATIC!! explosion over a few frames
  public void BlowUp()
  {
    if( state != ShipState.BlowingUp )
    {
      state = ShipState.BlowingUp;

      deathSequenceTimeRem = DEATH_SEQUENCE_LENGTH;

      SPW.world.sfx[ SFX.Death ].Play();
    }
  }

  public void Cloak()
  {
    if( this.energy > PENALTY_CLOAK_PER_SEC )
    {
      state = ShipState.Cloaking;
    }
  }

  public Vector2 GetHeading()
  {
    Vector2 heading = new Vector2();

    // using rotation matrix - shortened
    heading.X = (float)( Math.Cos( rot ) );
    heading.Y = (float)( Math.Sin( rot ) );

    return heading;
  }

  public void IncreaseThrust()
  {
    if( this.energy > PENALTY_ENGINES_PER_SEC )
    {
      thrusting = true;
    }
  }

  public void RotateLeft()
  {
    rot -= 0.1f;
    // avoid going over 360 degrees (2 pi)
    rot %= (float)( 2.0 * Math.PI );
  }

  public void RotateRight()
  {
    rot += 0.1f;
    // avoid going over 360 degrees (2 pi)
    rot %= (float)( 2.0 * Math.PI );
  }

  public void TradeEnergyForShield()
  {
    if( energy > 2 && shield < MAX_ENERGY )
    {
      energy -= 1.0f;
      shield += 1.0f;
    }
  }

  public void TradeShieldForEnergy()
  {
    if( shield > 2 && energy < MAX_ENERGY )
    {
      energy += 1.0f;
      shield -= 1.0f;
    }
  }

  public void ShootPhasors()
  {
    if( this.energy > PENALTY_PHASOR_SHOT )
    {
      if( phasor.IsReady )
      {
        this.energy -= PENALTY_PHASOR_SHOT;
        phasor.Shoot();
      }
    }
  }

  public void ShootTorpedos()
  {
    // disallow extreme rapid fire
    if( this.numTorpedosInAir >= MAX_TORPEDOS_IN_AIR )
    {
      // no shooting for you!  already at max of 7 torpedos.
      return;
    }

    // check if has enough energy left to shoot a torpedo
    if( this.energy > PENALTY_TORPEDO_SHOT )
    {
      // shooting the torpedo costs us some energy
      energy -= PENALTY_TORPEDO_SHOT;

      // this will automatically be taken back down
      // by the torpedo when it crashes into something
      // or expires due to time
      this.numTorpedosInAir++;

      // introduce a new sprite into the world.. a phasor!
      Projectile torpedo = new Projectile();

      Vector2 heading = GetHeading();
      torpedo.position = this.position + 10.0f * heading;
      torpedo.rot = this.rot;
      torpedo.velocity = this.velocity + ( heading * Projectile.NORMAL_START_SPEED );

      if( this.playerNumber == 1 )
      {
        // if its player 1 use the texture for player 1's torpedos
        torpedo.tex = SPW.world.torpedoTexP1;
        torpedo.shooter = this;
      }
      else
      {
        // its player 2 so use the player 2 texture
        torpedo.tex = SPW.world.torpedoTexP2;
        torpedo.shooter = this;
      }

      SPW.world.torpedos.Add( torpedo );

      // make the sound
      SPW.world.sfx[ SFX.Torpedo ].Play();
    }
  }

  // Damages the player for howMuch points
  // Reduces his shield.
  public void Damage( float howMuch )
  {
    this.shield -= howMuch;

    if( this.shield <= 0 )
    {
      // the player has been killed,
      // so blow him up
      this.BlowUp();
    }
  }

  /// <summary>
  /// Draws health as text
  /// </summary>
  /// <param name="spriteBatch">Spritebatch to draw with</param>
  public void DrawHealth( SpriteBatch spriteBatch )
  {
    string txt = String.Format( "E: {0:f0}   S: {1:f0}", energy, shield );
    Vector2 strlen = SPW.sw.sf.MeasureString( txt );

    if( this.playerNumber == 1 )
    {
      // player 1 health on the right side

      // draw as text
      int x = (int)( SPW.world.ScreenWidth - strlen.X - 40.0f );
      int y = (int)( SPW.world.ScreenHeight - 40.0f );
      SPW.sw[ "player1Stats" ] = new StringItem( txt, x, y );
    }
    else
    {
      // player 2 health on the left side

      int x = 40;
      int y = (int)( SPW.world.ScreenHeight - 40.0f );
      SPW.sw[ "player2Stats" ] = new StringItem( txt, x, y );
    }
  }

  /// <summary>
  /// Draws health as bars
  /// </summary>
  public void DrawHealthAsBars()
  {
    // Get approximate height of text, using M as
    // representative of the font
    Vector2 DimsOfM = SPW.sw.sf.MeasureString( "M" );
    float textHeight = DimsOfM.Y;
    float textWidth = DimsOfM.X;

    if( this.playerNumber == 1 )
    {
      // player 1 health on the right side

      // draw as bars
      int x = (int)( SPW.world.ScreenWidth - 20.0f );
      int y = (int)( SPW.world.ScreenHeight - 45.0f );
      SPW.sw[ "player1Stats" ] = new StringItem( "S\nE", x, y );

      // draw shield bar
      float startX = x - textWidth / 2;
      float startY = y + textHeight / 2;
      FlatShapes.Line( startX, startY, startX - ( this.shield ), startY );

      // draw energy bar
      startY = y + textHeight + textHeight / 2;
      FlatShapes.Line( startX, startY, startX - ( this.energy ), startY );
    }
    else
    {
      // player 2 health on the left side

      int x = (int)( 5.0f );
      int y = (int)( SPW.world.ScreenHeight - 45.0f );
      SPW.sw[ "player2Stats" ] = new StringItem( "S\nE", x, y );

      // shield
      float startX = x + textWidth + 3;
      float startY = y + textHeight / 2;
      FlatShapes.Line( startX, startY, startX + ( this.shield ), startY );

      // energy
      startY = y + textHeight + textHeight / 2;
      FlatShapes.Line( startX, startY, startX + ( this.energy ), startY );
    }
  }

  public void DrawDeath( SpriteBatch spriteBatch )
  {
    float spread = 0.0f;

    // expanding
    spread = Ship.DEATH_SEQUENCE_LENGTH - this.deathSequenceTimeRem;

    spread *= 200.0f;

    foreach( Particle part in this.particles )
    {
      spriteBatch.Draw( SPW.world.whitePX, part.getCurrentPosition( spread ), null, Color.White );
    }
  }

  public void DrawParticles( SpriteBatch spriteBatch )
  {
    float spread = 0.0f;

    float mid = Ship.HYPERSPACE_LENGTH / 2;
    if( this.hyperspaceTimeRem > mid )
    {
      // expanding
      spread = Ship.HYPERSPACE_LENGTH - this.hyperspaceTimeRem;
    }
    else
    {
      // contracting
      spread = ( this.hyperspaceTimeRem );
    }

    spread *= 400.0f;

    foreach( Particle part in this.particles )
    {
      spriteBatch.Draw( SPW.world.whitePX, part.getCurrentPosition( spread ), null, Color.White );
    }
  }

  private string vecString( Vector2 v )
  {
    return String.Format( "{0:f2}, {1:f2}", v.X, v.Y );
  }

  public override string ToString()
  {
    return base.ToString() + " " +
      state + " " +
      vecString( position ) + " " +
      vecString( velocity ) +
      String.Format( "rot={0:f2}", rot );
  }
}




// The phasor gun is different than
// the other Sprite types.
// Because a Phasor shot has to be tied to
// the ship that shoots it, it makes more sense
// to plant the Phasor object inside the Ship object
// rather than have the Phasors be seperate entities
// all unto themselves

// So the Phasor class doesn't inherit anything.
// A Phasor doesn't have it own position.. its position
// is tied to that of the ship that is shooting it

// It has its own property set however, which include
// amount of time its alive for, its current length
// (because when it hits something, it gets shortened)
public class Phasor
{
  // the ship that owns this phasor gun
  public Ship shooter;

  // damage the phasor gun does to a ship it strikes
  public static float DAMAGE = 2.0f;

  // when was last phasor shot?  determines when
  // allowed to shoot this phasor again
  private float timeSinceLastPhasorShot;

  // how far out does the phasor shot reach?
  // used for both hit detection and drawing
  public float reach;

  //the maximum reach in world units a phasor can go
  private static float MAX_REACH = 120.0f;

  // cooldown time between distinct firings
  // of the phasor gun
  private static float TIME_TO_RECHARGE_BETWEEN_PHASOR_SHOTS = 0.3f;

  // max time phasor stays alive for
  private static float MAX_TIME = 0.1f;

  // set the phasor to inactive after it hits something
  // so you don't get a phasor doing like 1,000,000 damage
  public bool isActive;

  // true when the phasor is ready to fire.
  public bool IsReady
  {
    get
    {
      // basically if the phasor has had enough time to recharge, then its ready
      // to shoot again
      if( this.timeSinceLastPhasorShot > TIME_TO_RECHARGE_BETWEEN_PHASOR_SHOTS )
        return true;
      else
        return false;
    }
  }

  public Phasor( Ship theOwner )
  {
    shooter = theOwner;
  }

  public void Shoot()
  {
    // can only fire the phasor gun if enough time
    // has passed since last shot for it to recharge
    if( this.timeSinceLastPhasorShot > TIME_TO_RECHARGE_BETWEEN_PHASOR_SHOTS )
    {
      this.timeSinceLastPhasorShot = 0;

      // means we'll actually check if phasor hits
      // something in collision detect routine
      this.isActive = true;

      // set its reach to the maximum
      this.reach = MAX_REACH;

      SPW.world.sfx[ SFX.Phasor ].Play();
    }
  }

  public void Step( float stepTime )
  {
    this.timeSinceLastPhasorShot += stepTime;

    if( this.timeSinceLastPhasorShot > MAX_TIME )
    {
      // deactivate
      this.isActive = false;
    }
  }

  public Vector2 getEndOfReachPoint()
  {
    return this.shooter.position + this.shooter.GetHeading() * this.reach;
  }



  /// <summary>
  /// Tells you how far along the line of this phasor
  /// it would be to strike the <paramref name="other"/> Sprite.
  /// 
  /// Gives you NULL if it simply wouldn't hit the other Sprite.
  /// </summary>
  /// <param name="other">The sprite to check if it hits</param>
  /// <returns>Distance along line if hits, NULL if does not</returns>
  public float? GetStrikeDistanceTo( Sprite other )
  {
    // phasors can't hit their owner.
    if( other == this.shooter as Sprite )
    {
      // null meaning no intersection
      return null;
    }

    Vector2 shipHeading = shooter.GetHeading();

    // create the ray
    Ray thisRay = new Ray(

      // starts at the center of
      // wherever the owning ship currently is.
      new Vector3( shooter.position.X, shooter.position.Y, 0.0f ),

      // it goes in the direction the ship is currently facing
      new Vector3( shipHeading.X, shipHeading.Y, 0.0f ) );



    // treat the other thing as a sphere
    BoundingSphere thatSphere = new BoundingSphere(
      new Vector3( other.position.X, other.position.Y, 0.0f ), other.GetApproxRadius() );


    // Determine if this ray intersects the other
    // sprite's bounding sphere.
    float? howFarOut = null;
    thisRay.Intersects( ref thatSphere, out howFarOut );

    if( howFarOut == null )
    {
      // This was a total miss.  The ray totally 
      // didn't intersect the sphere
      /*
       
       ray
       ------------->
       
       sphere
        __
       |  |
       |__|
       
       total miss
       */

      return howFarOut;  // totally missed.
    }
    else
    {
      // this means the INFINITE line the ray draws
      // does hit the sphere, and the float value indicates
      // hwo FAR ALONG the ray the intersection occurs

      /*
                        __
       ----------------|  |--------------->
                       |__|
       
       yeah it hits, but there's a float value to consider
        
       */


      // CONSIDERING THAT FLOAT VALUE:
      // don't let the hit occur if howFarOut is
      // greater than the reach of this phasor.
      if( howFarOut > reach )
      {
        // it WOULD have hit, but its out of range.
        /*
                       __
         --------->   |  |
                      |__|
                       
         not quite.  the ray falls short
        */

        // so say this was a miss.
        return null;
      }
      else
      {
        // this is a hit
        // tell calling function that yes it was a hit,
        // and give it that numeric value of howFarOut
        // (so it can actually determine the NEAREST body
        // to actually perform a strike)
        return howFarOut;
      }
    }
  }



  public void Strike( Sprite it )
  {
    if( it is Projectile )
    {
      // just destroy it
      ( it as Projectile ).Destroy();
    }
    else if( it is Ship )
    {
      Ship ship = it as Ship;

      ship.Damage( DAMAGE );

      SPW.world.sfx[ SFX.BlowUp ].Play();
    }

    this.reach = ( ( this.shooter.position - it.position ).Length() );

    // now deactivate the phasor, so it can't
    // strike multiple times per firing.

    // commenting this next line out
    // turns the phasor into
    // a really great (but unfair!) weapon though.
    this.isActive = false;


  }
}