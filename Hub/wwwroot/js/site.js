// Write your JavaScript code.
const connection = new signalR.HubConnection(
    "/clienthub", { logger: signalR.LogLevel.Information });

connection.on("ReceiveMessage", (message) => { 
    alert(message);
});

connection.start().catch(err => console.error);

function unsubscribe(subscriptionId) {
    $.ajax({
        type: "POST",
        url: "/client/Unsubscribe/" + subscriptionId,
        contentType: "application/json; charset=utf-8",
        success: function (response) {
            alert("Success");
        },
        failure: function (response) {
            alert("Failure");
        },
        error: function (response) {
            alert("Error");
        }
    });
}
