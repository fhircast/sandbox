// Write your JavaScript code.
const connection = new signalR.HubConnection(
    "/clienthub", { logger: signalR.LogLevel.Information });

connection.on("ReceiveMessage", (message) => { 
    alert(message);
});

connection.start().catch(err => console.error);