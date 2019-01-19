# SS14 Status Protocol

An SS14 server can host a simple HTTP server for status fetching by external software (like websites, bots...). We use HTTP here because it's simple and easy to consume.

Enabling this HTTP server is done by setting the `status.enabled` config variable to `true`. You can control the address the server will be bound to with the `bind` variable. Because .NET's `HttpListener` is written by people who clearly have no idea how networking works, I recommend you simply leave this as `localhost` and change the port number if you want to run more than one server on a machine. This server is absolutely designed to be slapped behind an Nginx/Apache reverse proxy anyways so it doesn't matter. You're getting no modern features like SSL, gzip, better routing, etc... otherwise. Don't bother PRing it.

Anyways, the URI path you're looking for is.. `/status`.

This *should* send a JSON response with some information about the game. This data is handled by content though, so while I can't make any guarantees, you're definitely likely to get the following values back:

* `name`: name of the server, string.
* `players`: player count, integer.

There's a few more there. Check the code for your content repo for specifics. If you're wondering where this HTTP server is handled, check `SS14.Server/ServerStatus/StatusHost.cs`. Just hit find usages a bit you'll find the content side.
