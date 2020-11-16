This file serves as documentation for network stuff

# Authentication Handshake

The client and server connect via Lidgren.Network.
This will be immediately obvious to you if you spent any time reading the code.

The game server can either require authentication, optionally allow authentication, or disable authentication entirely.

The packet exchange looks like this:

1. C->S `MsgLoginStart`
    1. If client requests auth and server allows/requires auth:
    2. S->C `MsgEncryptionRequest`
    3. (client auth)
    4. C->S `MsgEncryptionResponse`
    5. (server auth, both enable encryption)
2. S->C `MsgLoginSuccess`

<small><small>Yes this is literally taken from [Minecraft's authentication protocol](https://wiki.vg/Protocol_Encryption) </small></small>

Note that the S->C packet AFTER `MsgLoginStart` is preceded with a bool (+pad) to indicate whether auth is being done or not. None of the net messages mentioned here are sent as "regular" net messages. They are used as containers for the write/read logic only. Barring the exception mentioned just now, they are read/written directory from the Lidgren data message instead of with a preceding string table ID.

A more detailed overview is here:

First the client sends `MsgLoginStart`. This contains the client's username, whether it wants to authenticate, and whether it needs the server's public RSA key sent (when authenticating && it doesn't have it yet from the launcher).

The server can then choose to do block the client, let the client authenticate, or let the client in as guest. If it lets the client in as guest it skips straight to sending `MsgLoginSuccess` (see below). Otherwise it will send an `MsgEncryptionRequest` to the client to initiate authentication.

`MsgEncryptionRequest` contains a random verify token sent by the server, as well as the server's public RSA key (if requested).

When the client receives `MsgEncryptionRequest`, it will generate a 32-byte random secret. It will then generate an SHA-256 hash of this secret and the server's public key. This hash is POSTed to `api/session/join` (along with login token in `Authorization` header) on the auth server. The shared secret and verify token are separately encrypted with the server's RSA key, then sent along with the client's account GUID to the server in `MsgEncryptionResponse`.

The server will then decrypt the verify token and shared secret with its private RSA key. If the verify token does not match then drop the client (to check if the client is using the correct key). Then the server will generate the same hash as mentioned earlier and GET it to `api/session/hasJoined?hash=<hash>&userId=<userId>` to check if the user did indeed authenticate correctly. And also gets the user's username and GUID again because why not.

From this point on, if authenticating, all messages sent between client and server will be AES encrypted with the shared secret generated earlier.

Then the server shall reply with `MsgLoginSuccess` with the assigned username/userID if login is successful.

I think that was everything.

Oh yeah, the server generates a new 2048-bit RSA key every startup and exposes it via its status API on `/info`.

This is a rough outline. If you want complete gritty details just check the damn code.
