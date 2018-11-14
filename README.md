# FHIRcast Sandbox 

The FHIRcast Sandbox is a tool that allows users to simulate the workflow of the [FHIRcast](http://fhircast.org/) standard. It acts as a sort of "mock"-EHR or PACS that can be used as a demonstration and testing tool for showing how FHIRcast would work with subscribers.

## How it Works


## Local development

You can develop on and run this project locally by using the following steps below. The projects consists of two parts:

1. A [FHIRcast Hub](Hub) implementation with a non-standard API to show its current state.
2. A [WebSub client](WebSubClient) that can subscribe to a Hub using standard APIs and a web application that can notify other client connected to the hub as well as receive notifications from those client.

### Development without Docker (recommended)

#### Setup

First, install the [.NET Core SDK](http://dot.net).

#### Run it

In order to run the two webservers locally, run:

```sh
$ dotnet run --project Hub
```

to start the Hub, and

```sh
$ dotnet run --project WebSubClient
```

to start the WebSub client. On a Unix operating system you can also run both servers in the background using e.g.:

```sh
$ (dotnet run --project Hub &) && (dotnet run --project WebSubClient &)
```

and on Windows you can achieve the same thing like this in PowerShell:

```powershell
> "$pwd\Hub" | Start-Job { dotnet run --project $Input } -Name Hub; "$pwd\WebSubClient" | Start-Job { dotnet run --project $Input } -Name WebSubClient
```

You can then start issuing HTTP requests to the server. Here's an example using curl that will create a subscription and cause the Hub to attempt to validate your callback url (as defined in `my_url_encoded_callback`).

```sh
event='switch-patient-chart'
my_url_encoded_callback='http%3A%2F%2Flocalhost%3A1337'
topic='some_topic'

# Request a subscription on the hub.
curl -d "hub.callback={my_url_encoded_callback}&hub.mode=subscribe&hub.topic={topic}&hub.secret=secret&hub.events={events}&hub.lease_seconds=3600&hub.uid=untilIssueIsFixed" -X POST http://localhost:5000/api/hub
```

To stop the background servers, run:

For Unix:

```sh
$ pkill dotnet
```

For Windows:

```powershell
> Stop-Job Hub, WebSubClient; Remove-Job Hub, WebSubClient
```

#### Tutorial

See the [in progress Tutorial](https://github.com/fhircast/sandbox/wiki/Tutorial) for a more detailed steps towards a hello world app. [Feedback](https://chat.fhir.org/#narrow/stream/118-FHIRcast) welcome (and needed)!

### Development or runnign with Docker

In order to launch the hub and the client using docker, use docker-compose from the root of the repository like so:

```
$ docker-compose up
```

This will build the docker containers and run the application on ports 5000 (for Hub) and 5001 (for the Client).

## Build and Contribution

We welcome any contributions to help further enhance this tool for the FHIRcast community! To contribute to this project, please see instructions above for running the application locally and testing the app to make sure the tool works as expected with your incorporated changes. Then follow the steps below.

1. Issue a pull request on the `fhircast/sandbox` repository with your changes for review.
