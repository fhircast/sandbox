// Write your JavaScript code.
const connection = new signalR.HubConnection(
    "/clienthub", { logger: signalR.LogLevel.Information });

connection.on("ReceiveMessage", (message) => { 
    alert(message);
});

connection.start().catch(err => console.error);

<<<<<<< HEAD
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
=======
function deleteRow(rowId) {
    alert("deleting " + rowId);
>>>>>>> eb6a56c3119cc39dc48f9759eb57b6554107270d
}
