public enum SFX
{
  Phasor,
  Torpedo,
  Hyperspace,
  Alarm,
  BlowUp
}


public enum GameState
{
  TitleScreen,
  LocalGame,
  NetGame
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
  Connected
  
}


public enum ShipState
{
  Normal,
  Cloaking,
  Hyperspace,
  BlowingUp
}
