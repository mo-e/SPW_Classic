public enum SFX
{
  Phasor,
  Torpedo,
  Hyperspace,
  Alarm,
  BlowUp,
  Death
}


public enum GameState
{
  TitleScreen,
  LocalGame,
  NetGame,

  Testing
}


public enum NetState
{

  /// <summary>
  /// Completely disconnected from the server/network
  /// </summary>
  Disconnected,

  /// <summary>
  /// Waiting for another player to join @ server
  /// </summary>
  Waiting,

  /// <summary>
  /// Connected to network and another player is present as well
  /// </summary>
  Connected,
  
  /// <summary>
  /// A state that says the OTHER GUY is lagging,
  /// and our game engine should NOT PROCESS ANYTHING
  /// (basically freeze the game for a bit)
  /// until we get a message from the other guy
  /// saying he has caught up
  /// </summary>
  TooFarAhead, //WaitingForOtherPlayer

  /// <summary>
  /// A state that says we are lagging
  /// and our game engine shouldn't accept inputs
  /// from us until we catch up because we could
  /// corrupt the game state by sending "expired"
  /// input
  /// </summary>
  TooFarBehind // Lagggggggging

}


public enum ShipState
{
  Normal,
  Cloaking,
  Hyperspace,
  BlowingUp
}






// Underneath it all, an ENUM is just a buncha ints.
// Default underlying type of an enum
// is INT.

// The underlying type of an enum can be 
// checked by Enum.GetUnderlyingType()

/// <summary>
/// Enumerates the actions the player can take
/// </summary>
public enum NetMessageCommand : int // specify underlying type should be INT
{                                   // ensures this enum
  // actually has int values underneath it all
  None = 0,

  // 9 actions the player can take
  /// <summary>
  /// Player wants to trade some of his shields for
  /// energy.  What a bargain!
  /// </summary>
  TradeShieldForEnergy = 1,

  /// <summary>
  /// Player wants to hyperspace!!  SHOOM!
  /// </summary>
  BeginHyperspace,  // will be int valued 2

  /// <summary>
  /// Player wants to swap some energy for shields
  /// </summary>
  TradeEnergyForShield, // 3

  /// <summary>
  /// Player wants to rotate left
  /// </summary>
  RotateLeft, // 4

  /// <summary>
  /// Player wants to move forward
  /// </summary>
  IncreaseThrust, // 5

  /// <summary>
  /// Player wants to rotate right
  /// </summary>
  RotateRight, // 6

  /// <summary>
  /// Player wants to shoot his phasor gun
  /// </summary>
  ShootPhasors, // 7

  /// <summary>
  /// Player wants to cloak
  /// </summary>
  Cloak,  // 8

  /// <summary>
  /// Player wants to shoot a torpedo.
  /// </summary>
  ShootTorpedos, // 9 



  // program control

  /// <summary>
  /// A message sent from the server, indicating
  /// that it wants to start the game
  /// </summary>
  GameStart, // 10

  /// <summary>
  /// The Sync message, used to tell the other player
  /// what frame I am CURRENTLY on.  Sent every 10 frames
  /// from this program, like a heartbeat.  If the other player
  /// is too far ahead, (approx 10-20 frames) then he
  /// knows that he needs to wait for me to catch up.
  /// </summary>
  Sync  // 11
}

