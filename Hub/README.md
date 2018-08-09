# Hub

The FHIRcast Hub project is an implementation of a FHIRcast Hub with some additional APIs to make it easy to inspect and test with. 

## Usage

Assuming your current directory is the same as this file, you can run the Hub using:

```sh
$ dotnet run
```

which will launch the Hub on http://localhost:5000 by default.

## API

There are some APIs outside of FHIRcast that can be used towards the hub to inspect its current state:

### `GET /api/hub`

Get the current subscriptions of the hub.
