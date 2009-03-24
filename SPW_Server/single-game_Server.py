#Python 2.5
from socket import *
from select import select
from struct import pack
import sys


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


print 'MAINSOCKET is ready to receive new connections.'


# wait for player 1 to connect
( player1sock, ip1 ) = MAINSOCKET.accept()
print 'player 1 is here'

# wait for player 2 to connect
( player2sock, ip2 ) = MAINSOCKET.accept()
print 'player 2 is here'

print 'putting player1sock and player2sock into an array..'
ALL = [ player1sock, player2sock ]

print 'creating GameStart message..'
# send the start messages to both players,
# at the same time, telling each which player
# he is.
player1msg = pack(
  'iii', #sending 3 ints, packed together
  1,     # the first one is which player you are
  0,     # the second one is the frame stamp, which is just 0
  10 )   # the third one is type of message.  corresponds to "GameStart"
         # message in NetworkListener.cs.
         # because this is the ONLY message the server actually sends down
         # we're not bothering to put that entire enum up in this file,
         # though we could if we were ocd about it.
player2msg = pack( 'iii', 2, 0, 10 ) # game start message to player 2

print 'SENDING the GameStart message..'
player1sock.send( player1msg )
player2sock.send( player2msg )

# now player1 and player2 both know which
# player they're supposed to play as.
# they also know the game has started.

# all that's left for the server to do now
# is simply listen for incoming messages
# from either player and just shoot that message
# down again to all connected players.

print "I told them we're starting"

running = True
while running:

  # block until one of the player sockets sends something
  ( ready, dontcare, dontcare ) = select( ALL, [], [] )

  # get data from every ready socket and send it down
  # to both player1sock and player2sock
  for sock in ready :
    
    data = None
    try:
      data = sock.recv( 1008 )  # = 12*84. Want multiple of 12, since sizeof(Message) struct = 12 bytes
    except:
      # if an exception happens when trying to receive,
      # it means that player has disconnected abruptly,
      # so basically game over
      print 'someone has gone away, abandoning session'
      running=False
      break

    if data is None or len( data ) == 0 :
      # if either player sends a 0 length packet,
      # then they have "gracefully"/"politely" disconnected
      # from the server, so again, game over.
      print 'a player has disconnected, abandoning session'
      running=False
      break

    
    # some data is passing through, send off to both players
    player1sock.send( data )
    player2sock.send( data )
  #</for>
#</while running>

# When game is over, terminate both sockets
print 'Shutting down player sockets..'
player1sock.close()
player2sock.close()

# terminate the main socket
print 'Shutting down main socket'
MAINSOCKET.close()



