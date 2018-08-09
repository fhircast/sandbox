# WebSubClient

This project serves to exemplify how a WebSub client could be built towards a FHIRcast Hub. The client consists of a server component and a web UI, but only the server component communicates directly with the Hub. The client and the server component communicates using proprietary means outside the FHIRcast standard. In order to have a user-facing client communicate with a FHIRcast Hub directly, see the [WebSocket addition to the standard][fhircast-websocket] (TODO: Add correct link here).

## Usage

Assuming your current directly is the same as this file, you can run the WebSub client using:

```sh
$ dotnet run
```

You can then go to http://localhost:5001 and use the web application to notify and subscribe to FHIRcast hubs.

fhircast-websocket: http://fhircast.org
