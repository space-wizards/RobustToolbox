This file serves as documentation for network stuff

# Auth

Public servers on the hub generally expect you to be authenticated with a proper account. There is one central authentication server, hosted by us.

Since you're only supposed to connect to these public servers with the launcher, the client cannot authenticate an account (from username+password) on its own. This also provides security by keeping the user's password or sensitive login tokens outside the client process, were it to get compromised.

First of all, the game server generates an RSA key pair on startup of which the public key is available via the status API. When the launcher wants to connect to a game server, it sends this public key to the auth server (along with login token to prove identity). The auth server then returns a JWT encrypted with the aforementioned public key. The JWT contains the userID, username, and an SHA-256 hash of the public key (in base64). The JWT is also signed asymmetrically which can be verified with a public key provided by the auth server (which is fetched by the game server on startup).

The launcher hands this RSA-encrypted JWT to the client, which will then hand it to the server. The server will decrypt it with its private key, and then assert that, indeed, the hash matches and the token signature is valid for the auth server's public key. It then has all the info it needs to accept the client, including user ID and username.

Using crypto for this means only trip to the auth server is necessary for the authentication process, and we don't need to keep a store of temporary login sessions on the auth server. It also means that login tokens can ONLY be used for the server they're intended for.

# Handshake

The client and server connect via Lidgren.Network.
This will be immediately obvious to you if you spent any time reading the code.

The game server can either require authentication, optionally allow authentication, or disable authentication entirely.

After the initial Lidgren handshake is done, the client will send an `MsgLogin` to the server with the following data:

* Username
* (if trying to authenticate) auth token gotten from launcher
* (if trying to authenticated) 32-byte random encryption key generated client side, encrypted with the server's public RSA key.

Note that this `MsgLogin` isn't sent like a regular string-table-indexed net message. It's just as a dummy to keep the write/read logic in one place.




