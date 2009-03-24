#Python 2.5
from socket import *
from select import select
from struct import pack
import time
import thread
import sys

# Hosts multiple SPW games at once.

### <globals>
#
ALL_SOCKETS = []  # every socket ever created.. used in shutdown.

MAINSOCKET = None # the main socket

MAINRUNNING = True

TOTAL_RUNNING_GAMES = 0   # total number of games currently running
#
### </globals>


### <function-defs>
#

def main():
  # declare that we intend to use
  # the GLOBAL versions of these variables
  # and when I use them in an assignment
  # (e.g when I create MAINSOCKET = socket(),)
  # I DO NOT want a new local copy (local to this function)
  # defined.
  
  global MAINSOCKET
  global MAINRUNNING
  global TOTAL_RUNNING_GAMES
  
  ####
  # Server Startup.
  try :
    #1.  create server socket
    MAINSOCKET = socket( AF_INET, SOCK_STREAM )
    print 'socket created'
    
    #2.  bind
    MAINSOCKET.bind( ( '', 7070 ) )
    print 'bound'
    
    #3.  listen
    MAINSOCKET.listen( 5 )
    print 'listening'
      
  except :
    #only get here if something bad happened in try block above
    print "Server couldn't startup! Reason:",sys.exc_info()
    MAINSOCKET.close()
    sys.exit()  # bail if failed


  # start up sentry thread which is cmd-line means to quit
  thread.start_new_thread( QuitSentry, () )


  # redirect stdout to file for later review
  ##sys.stdout = open( "py-server-log.txt", "a" )
  Log( 'startup' )

  # now the main thread will enter this loop.. where
  # it continually attempts to accept PAIRS of players.

  # those pairs are scurried off to their own thread
  # where they play together
  MAINRUNNING = True

  while MAINRUNNING:
    player1sock = None
    player2sock = None
    try:
      # wait for player 1 to connect
      ( player1sock, ip1 ) = MAINSOCKET.accept()
      Log( 'a player 1 is here' )

      # wait for player 2 to connect
      ( player2sock, ip2 ) = MAINSOCKET.accept()
      Log( 'a player 2 is here' )
    except:
      Log( "I couldn't accept a player, reason: %s" % sys.exc_info() )
      ##sys.exit()  # no need to exit, just disconnect both sockets and loop back to recover
      if player1sock is not None:
        player1sock.close()
      if player2sock is not None:
        player2sock.close()
      # go back to while MAINRUNNING statement
      continue
      

    ALL_SOCKETS.append( player1sock )
    ALL_SOCKETS.append( player2sock )

    TOTAL_RUNNING_GAMES += 1

    Log( "Starting new game.  Total games running: %d, total players connected: %d" % (TOTAL_RUNNING_GAMES, len(ALL_SOCKETS) ) )

    thread.start_new_thread( RunGame, ( player1sock, player2sock ) )
  #</while>

  MAINSOCKET.close()


#</def main>




def RunGame( player1sock, player2sock ) :

  # we assign to TOTAL_RUNNING_GAMES in this function,
  # so we have to specify that when we assign to TOTAL_RUNNING_GAMES
  # we DO NOT want a new local copy (local to this function) created
  global TOTAL_RUNNING_GAMES

  print 'Running a game with player1sock=',player1sock,' player2sock=',player2sock
  
  # send the start messages to both players,
  # at the same time, telling each which player
  # he is.
  player1msg = pack( 'iii', 1, 0, 10 )
  player2msg = pack( 'iii', 2, 0, 10 )

  Log( 'sending the start signals' )
  player1sock.send( player1msg )
  player2sock.send( player2msg )
  Log( 'I sent player 1 and player 2 the start signals' )

  # now player1 and player2 both know which
  # player they're supposed to play as.
  # they also know the game has started.

  # all that's left for the server to do now
  # is simply listen for incoming messages
  # from either player and just shoot that message
  # down again to all connected players.

  ALL = [ player1sock, player2sock ]
  running = True
  
  while running and MAINRUNNING:
    
    ( ready, dontcare, dontcare ) = select( ALL, [], [] )
    
    for sock in ready :
      data = None
      try:
        data = sock.recv( 1008 )  # = 12*84. Want multiple of 12, since sizeof(Message) struct = 12 bytes
      except:
        Log( 'someone has gone away, abandoning session' )
        running=False
        break

      if data is None or len( data ) == 0 :
        Log( 'a player has disconnected, abandoning session' )
        running=False
        break
   
      #some data is passing through
      player1sock.send( data )
      player2sock.send( data )
    #</for>
  
  #</while running>
  
  TOTAL_RUNNING_GAMES -= 1

  print 'before closing player1sock=',player1sock,' player2sock=',player2sock
  
  player1sock.close()
  player2sock.close()

  print 'closed now player1sock=',player1sock,' player2sock=',player2sock

  for sock in ALL_SOCKETS :
    print sock
  print '\n'
  
  ALL_SOCKETS.remove( player1sock )
  ALL_SOCKETS.remove( player2sock )

  Log( "Game ended.  Total games running: %d, total players connected: %d" % (TOTAL_RUNNING_GAMES, len(ALL_SOCKETS) ) )

#</def RunGame>

""" Allows you to stop the server by typing 'quit' at the command line """
def QuitSentry():
  
  global MAINRUNNING
  
  # blocks until you type something
  """
  cmd = raw_input( "type anything to stop the server and disconnect all people" )
  print cmd
  
  # signal to all threads basically that we're shutting down
  
  MAINRUNNING = False  
  Log( 'shutting down the server...' )
  for sock in ALL_SOCKETS:
    print 'closing',sock,'\n'
    sock.close()
  
  print 'closing main=',MAINSOCKET,'\n'
  print 'ok, now TRY and connect to the server to complete the shutdown'
  MAINSOCKET.close()
  """
#</def sentry>

def Log( val ):
  print time.strftime( '%c' ),': ',val,'\n'
  sys.stdout.flush()
#</def Log>

#
### </function-defs>



main()







