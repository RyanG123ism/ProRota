function confirmEmailHub() {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/emailConfirmationHub") // ✅ This must match Program.cs
        .configureLogging(signalR.LogLevel.Information) // ✅ Add logging
        .build();

    connection.start()
        .then(() => console.log("Connected to SignalR"))
        .catch(err => console.error("SignalR connection failed:", err));

    connection.on("ReceiveConfirmation", function () {
        console.log("Received confirmation event!");

        document.getElementById("confirmationMessage").innerHTML =
            "<div class='alert alert-success'>Your account has been confirmed! Redirecting to login...</div>";

        document.getElementById("confirmationMessage").style.display = "block"; // ✅ Ensure it's visible


        setTimeout(() => {window.location.href = "/Identity/Account/Login"; // ✅ Correct path
        }, 7000);
    });
}