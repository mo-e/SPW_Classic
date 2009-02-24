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
  public Vector2 position ;

  public int graphicalWidth ;
  public int graphicalHeight ;
  public Vector2 center ;

  // Using this value for all sprites's bounding spheres
  public static float BOUNDING_SPHERE_RADIUS = 6.0f ;

  public Texture2D tex ;

  public Sprite()
  {

  }

  public Sprite( Texture2D texture, float x, float y, int w, int h )
  {
    tex = texture ;
    position.X = x ;
    position.Y = y ;
    graphicalWidth = w;
    graphicalHeight = h;

    center = new Vector2( w / 2, h / 2 );
  }

  // Intersection method to tell if this sprite intersects another
  // default uses bounding spheres.
  public virtual bool Intersects( Sprite other )
  {
    // construct BoundingSphere
    BoundingSphere thisSphere = new BoundingSphere(
      new Vector3( this.position.X, this.position.Y, 0.0f ), BOUNDING_SPHERE_RADIUS );

    BoundingSphere thatSphere = new BoundingSphere(
      new Vector3( other.position.X, other.position.Y, 0.0f ), BOUNDING_SPHERE_RADIUS );

    if( thisSphere.Intersects( thatSphere ) )
      return true ;
    else
      return false ;
  }
}


public class Particle : Sprite
{
  Ship owner ;
  Vector2 spreadDir;

  public Particle()
  {
    // give each particle a random spread direction

    float spreadX = 1.0f - (float)SPW.rand.NextDouble(); 
    float spreadY = 1.0f - (float)SPW.rand.NextDouble();

    spreadDir = new Vector2( spreadX, spreadY );
    
  }

  public Particle( Ship shipOwner ) : this()
  {
    owner = shipOwner ;
  }


  public Vector2 getCurrentPosition( float spread  )
  {
    return owner.position + spreadDir * spread ;
  }
}





// A sprite that is intended to move over time.
public class MovingSprite : Sprite
{
  public Vector2 velocity;

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
  // maximum number of units this torpedo travels before
  // self destruct
  private static float MAX_TIME = 20.0f ;
  private static float DAMAGE = 4.0f ;
  public static float NORMAL_START_SPEED = 2.5f ;

  private float lifeTime ;

  // mark projectile as finished once it strikes a player or expires
  public bool dead;

  // keep a back reference to the player
  // that shot the projectile
  public Ship shooter ;

  public Projectile()
  {
    lifeTime = MAX_TIME ;

    dead = false;

    // because torpedos measure 7 by 12
    center = new Vector2( 3.0f, 6.0f ) ;
  }

  public override void Step( float stepTime )
  {
    if( this.lifeTime < 0.0f )
    {
      this.Strike( null ) ; // strike nothing, destroying itself
    }
    lifeTime -= stepTime ;

    base.Step( stepTime );
  }

  public void Strike( Ship which ) 
  {
    if( which != null )
    {
      which.Damage( DAMAGE );
      SPW.world.sfx[SFX.BlowUp].Play();
    }

    // now remove itself from the collection of torpedos
    //SPW.world.torpedos.Remove( this ) ; // not allowed since
    // we are iterating over the collection WHEN this gets called
    shooter.numTorpedosInAir-- ;
    this.dead = true ;
    
  }
}






public class Ship : MovingSprite
{
  public int playerNumber ;
  
  public float energy ;
  public float shield ;

  public bool dead ; // set when blow up sequence done

  // used when go into hyperspace or explode.
  public List<Particle> particles ;

  // Maximum value either energy or shield can increase to.
  private static float MAX_ENERGY = 120.0f ;
  
  
  // Penalties for various things
  private static float PENALTY_CLOAK_PER_SEC = 2.0f, PENALTY_ENGINES_PER_SEC = 2.0f ;
  private static float PENALTY_HYPERSPACE = 8.0f, PENALTY_PHASER_SHOT = 1.0f, PENALTY_TORPEDO_SHOT = 1.0f;


  // torpedos
  public int numTorpedosInAir;
  public static int MAX_TORPEDOS_IN_AIR = 7;

  // whether has foot on throttle or not
  private bool thrusting ;

  // bonus every 2 seconds
  private float timeSinceLastBonus ;

  // only allowed 1 phasor shot per second
  private float timeSinceLastPhasorShot ;

  // player energy++ every 2 seconds
  private static float TIME_BETWEEN_ENERGY_BONUS = 2.0f, TIME_BETWEEN_PHASOR_SHOTS = 0.5f ;
  
  // Length of time hyperspace goes on for, and death sequence when player blows up
  public static float HYPERSPACE_LENGTH = 1.0f, DEATH_SEQUENCE_LENGTH = 1.0f ;
  

  public ShipState state ;

  // Amount of time left in hyperspace.
  // also used to draw the "cloud" of the player
  // when he is in hyperspace
  public float hyperspaceTimeRem, deathSequenceTimeRem ;

  public Ship()
  {
    velocity = new Vector2();
    state = ShipState.Normal ;
    dead = false ;

    // total looks like about 160
    energy =  40.0f ;
    shield = MAX_ENERGY ;

    particles = new List<Particle>();

    for( int i = 0 ; i < 100; i++ )
    {
      particles.Add( new Particle( this ) ) ;
    }
  }

  public override void Step( float stepTime )
  {
    switch( state )
    {
      case ShipState.Cloaking:

        // take energy down by penalty amount
        energy -= PENALTY_CLOAK_PER_SEC * stepTime ;

        // there, we spent the energy for the cloak,
        // and we'll try to revert to normal state if was cloaking
        state = ShipState.Normal;

        //C# doesn't allow fall-thru
        //without goto, but the rest of the frame
        // computation is going to be the same as Normal state
        goto case ShipState.Normal ;

      case ShipState.Normal:

        if( this.thrusting )
        {
          this.energy -= PENALTY_ENGINES_PER_SEC * stepTime ;

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

          this.thrusting = false ;
        }

        // run the Step function in MovingSprite,
        // which actually physically moves the player
        base.Step( stepTime );
        break;

      case ShipState.Hyperspace:

        hyperspaceTimeRem -= stepTime;

        base.Step( stepTime );

        // get out of phasing mode if done hyperspacing
        if( hyperspaceTimeRem < 0 )
        {
          state = ShipState.Normal ;
          velocity = Vector2.Zero;
        }

        break;

      case ShipState.BlowingUp:

        deathSequenceTimeRem -= stepTime ;

        if( deathSequenceTimeRem < 0 )
        {
          this.dead = true ;
        }

        break;
    }

    // Player gets a chance to recharge a bit every turn
    // phasors back online and auto-shield ++
    Recharge( stepTime );
  }

  private void Recharge( float stepTime )
  {
    // phasors recharge
    timeSinceLastPhasorShot += stepTime;

    // shields recharge
    timeSinceLastBonus += stepTime;

    // (( award energy bonus if 2.0s has passed ))
    if( timeSinceLastBonus >= TIME_BETWEEN_ENERGY_BONUS && energy < MAX_ENERGY )
    {
      energy ++ ;
      timeSinceLastBonus = 0.0f;
    }
  }


  public void BeginHyperspace()
  {
    if( state != ShipState.Hyperspace )
    {
      if( energy > PENALTY_HYPERSPACE )
      {
        // phasing costs 8 units of energy
        energy -= PENALTY_HYPERSPACE;

        // set phase to max phase length
        hyperspaceTimeRem = HYPERSPACE_LENGTH ;

        state = ShipState.Hyperspace;

        velocity = new Vector2( (float)SPW.rand.NextDouble() * 5.0f, (float)SPW.rand.NextDouble() * 5.0f );

        // play the sound
        SPW.world.sfx[SFX.Hyperspace].Play();
      }
    }
  }

  public void BlowUp()
  {
    if( state != ShipState.BlowingUp )
    {
      state = ShipState.BlowingUp;
      deathSequenceTimeRem = DEATH_SEQUENCE_LENGTH ;
    }
  }

  public void Cloak()
  {
    if( state != ShipState.Hyperspace )
    {
      state = ShipState.Cloaking ;
    }
  }

  private Vector2 GetHeading()
  {
    Vector2 heading = new Vector2();

    // using rotation matrix - shortened
    heading.X = (float)( Math.Cos( rot ) );
    heading.Y = (float)( Math.Sin( rot ) );

    return heading ;
  }

  public void IncreaseThrust()
  {
    if( this.energy > PENALTY_ENGINES_PER_SEC )
    {
      thrusting = true ;
    }
  }

  public void RotateLeft()
  {
    rot -= 0.1f;
    // avoid going over 360 degrees (2 pi)
    rot %= (float)(2.0 * Math.PI);
  }

  public void RotateRight()
  {
    rot += 0.1f;
    // avoid going over 360 degrees (2 pi)
    rot %= (float)(2.0 * Math.PI);
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
    // to implement
  }

  public void ShootTorpedos()
  {
    // disallow extreme rapid fire
    if( this.numTorpedosInAir >= MAX_TORPEDOS_IN_AIR )
    {
      // no shooting for you!  already at max of 7 torpedos.
      return ;
    }

    // check if has enough energy left to shoot a torpedo
    if( this.energy > PENALTY_TORPEDO_SHOT )
    {
      // shooting the torpedo costs us some energy
      energy -= PENALTY_TORPEDO_SHOT;

      // this will automatically be taken back down
      // by the torpedo when it crashes into something
      // or expires due to time
      this.numTorpedosInAir ++ ;

      // introduce a new sprite into the world.. a phasor!
      Projectile torpedo = new Projectile();

      Vector2 heading = GetHeading();
      torpedo.position = this.position + 10.0f * heading;
      torpedo.rot = this.rot ;
      torpedo.velocity = this.velocity + ( heading * Projectile.NORMAL_START_SPEED );

      if( this.playerNumber == 1 )
      {
        // if its player 1 use the texture for player 1's torpedos
        torpedo.tex = SPW.world.torpedoTexP1 ;
        torpedo.shooter = this ;
      }
      else
      {
        // its player 2 so use the player 2 texture
        torpedo.tex = SPW.world.torpedoTexP2;
        torpedo.shooter = this ;
      }

      SPW.world.torpedos.Add( torpedo );

      // make the sound
      SPW.world.sfx[SFX.Torpedo].Play();
    }
  }

  

  public void Damage( float howMuch )
  {
    this.shield -= howMuch ;

    if( this.shield < 0 )
    {
      // play the sound
      SPW.world.sfx[SFX.BlowUp].Play();
    }
  }




  public void DrawHealth( SpriteBatch spriteBatch )
  {
    string txt = String.Format( "E: {0:f0}   S: {1:f0}", energy, shield );
    Vector2 strlen = SPW.sw.sf.MeasureString( txt );


    // just a rectangle along the bottom.
    if( this.playerNumber == 1 )
    {
      // player 1 health on the right side
      //spriteBatch.Draw( SPW.world.whitePX, new Rectangle( 


      int x = (int)(SPW.world.ScreenWidth - strlen.X - 40.0f );
      int y = (int)(SPW.world.ScreenHeight - 40.0f );
      SPW.sw[ "player1Stats" ] = new StringItem( txt, x, y );
    }
    else
    {
      // player 2 health on the left side

      int x = 40 ;
      int y = (int)( SPW.world.ScreenHeight - 40.0f );
      SPW.sw[ "player2Stats" ] = new StringItem( txt, x, y );
    }
  }


  public void DrawDeath( SpriteBatch spriteBatch )
  {
    float spread = 0.0f;

    // expanding
    spread = Ship.DEATH_SEQUENCE_LENGTH - this.deathSequenceTimeRem ;

    spread *= 200.0f;

    foreach( Particle part in this.particles )
    {
      spriteBatch.Draw( SPW.world.whitePX, part.getCurrentPosition( spread ), null, Color.White );
    }
  }

  public void DrawParticles( SpriteBatch spriteBatch )
  {
    float spread = 0.0f;
      
    float mid = Ship.HYPERSPACE_LENGTH / 2 ;
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
      String.Format( "rot={0:f2}", rot ) ;
  }
}



