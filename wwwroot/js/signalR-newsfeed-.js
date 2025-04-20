const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${window.location.origin}/newsFeedHub`)
    .withAutomaticReconnect()
    .build();

//Start the SignalR connection
async function startConnection() {
    try {
        await connection.start();
        console.log("Connected to SignalR News Feed Hub");
    } catch (err) {
        console.error("SignalR Connection Error: ", err);
        setTimeout(startConnection, 5000); // Retry connection every 5 seconds
    }
}
startConnection();

//Function to show a simple alert notification
function showNewsFeedAlert() {
    let alertBox = document.createElement("div");
    alertBox.classList.add("news-alert");
    alertBox.innerHTML = `
        <p>📢 There's been an update to your feed! <a href="/Home/Index">Click here to check it out</a></p>
    `;

    document.body.appendChild(alertBox);

    //Auto-hide after 8 seconds
    setTimeout(() => alertBox.remove(), 8000);
}

//Listen for new news feed updates
connection.on("ReceiveNewsUpdate", function () {
    showNewsFeedAlert();
});

