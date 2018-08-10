// Write your JavaScript code.
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/websubclienthub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.on("ReceiveMessage", (message) => {
    alert(message);
});

connection
    .start()
    .catch(err => console.error(err.toString()));

function unsubscribe(subscriptionId) {
    $.ajax({
        type: "POST",
        url: "/client/unsubscribe/" + subscriptionId,
        contentType: "application/json; charset=utf-8",
        success: function (response) {
            alert("Success");
        },
        failure: function (response) {
            console.error(`Failed to unsubscribe: ${JSON.stringify(response)}`);
        },
        error: function (response) {
            console.error(`Error when unsubscribing: ${JSON.stringify(response)}`);
        }
    });
}
