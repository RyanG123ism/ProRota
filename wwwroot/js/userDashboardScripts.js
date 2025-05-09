﻿//Group of functions repsonsible for managing the pending shifts table in the user dashboard
function initialiseUserRota(shiftsJson, timeOffRequestsJson) {

    document.getElementById("prev-week-btn").addEventListener("click", () => navigateWeek(-1));
    document.getElementById("next-week-btn").addEventListener("click", () => navigateWeek(1));
    document.getElementById("current-week-btn").addEventListener("click", () => navigateToCurrentWeek());

    const shifts = shiftsJson;//json data sent from the view
    const requests = timeOffRequestsJson;//json data sent from the view

    //maps the approved requests to an array 
    const filteredRequests = mapTimeOffRequests(requests);

    let currentWeekStart; // Tracks the current week's Monday
    const oneWeekMilliseconds = 7 * 24 * 60 * 60 * 1000; // Number of milliseconds in a week

    function initialiseWeek() {
        const today = new Date();
        currentWeekStart = new Date(today);
        currentWeekStart.setDate(today.getDate() - today.getDay() + 1); // Adjust to Monday
    }


    // Function to populate the table for the current week
    function populateTable() {
        //get the table elements
        const tableHeader = document.getElementById("pendingShifts-table-header");
        const tableBody = document.getElementById("pendingShifts-table-body");

        // Clear existing content
        tableHeader.innerHTML = "";
        tableBody.innerHTML = "";

        // Build the date range for the current week (Monday to Sunday)
        const dateRange = [];
        for (let i = 0; i < 7; i++) {
            const date = new Date(currentWeekStart);
            date.setDate(currentWeekStart.getDate() + i);
            dateRange.push(date);
        }

        // Generate table headers (Dates)
        const headerRow = document.createElement("tr");
        const dateHeader = document.createElement("th");
        dateHeader.textContent = "Shift Date";
        headerRow.appendChild(dateHeader);

        //aplying the date range to table headers
        dateRange.forEach(date => {
            const th = document.createElement("th");
            th.textContent = date.toLocaleDateString();
            headerRow.appendChild(th);
        });
        tableHeader.appendChild(headerRow);

        // Generate table rows (Shift times or "OFF")
        const row = document.createElement("tr");
        const detailsHeader = document.createElement("td");
        detailsHeader.textContent = "Shift Times";
        row.appendChild(detailsHeader);

        dateRange.forEach(date => {
            const shift = shifts.find(s => new Date(s.StartDateTime).toDateString() === date.toDateString());
            const request = filteredRequests.find(r => new Date(r.Date).toDateString() === date.toDateString());
            const td = document.createElement("td");
            if (shift) {
                td.textContent =
                    new Date(shift.StartDateTime).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }) +
                    " - " +
                    new Date(shift.EndDateTime).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
            } else {
                if (request) {
                    td.textContent = "HOLIDAY";
                } else {
                    td.textContent = "OFF";
                }
            }
            row.appendChild(td);
        });
        tableBody.appendChild(row);
    }

    //navigate weeks of a users rota
    function navigateWeek(direction) {
        currentWeekStart = new Date(currentWeekStart.getTime() + direction * oneWeekMilliseconds); // Adjust week
        populateTable(); // Re-populate the table with the new week
        highlightTableCellsforPendingShifts(); // highlight cells after table change
    }

    function navigateToCurrentWeek() {
        const today = new Date();
        currentWeekStart = new Date(today);
        currentWeekStart.setDate(today.getDate() - today.getDay() + 1); // Adjust to the Monday of the current week

        populateTable(); // Re-populate the table for the current week
        highlightTableCellsforPendingShifts(); // Highlight cells if needed
    }

    //maps JSON data into individual shift objects, filters out any null values, and orders by ascending
    function mapShifts(shifts) {
        const sortedShifts = shifts.map(shift => ({
            ...shift,
            StartDate: shift.StartDateTime ? new Date(shift.StartDateTime).toISOString().split('T')[0] : null
        })).filter(shift => shift.StartDate !== null)
            .sort((a, b) => new Date(a.StartDateTime) - new Date(b.StartDateTime));

        console.log("Sorted Shifts: ", sortedShifts);

        return sortedShifts;

    }

    //maps JSON data into timeoffrequests array, filters for only approved requests
    function mapTimeOffRequests(requests) {
        //filtering the reuqest data to only contain approved requests
        const filteredRequests = requests.map(req => ({
            ...req,
            Status: req.IsApproved === 0 ? "Approved" : req.IsApproved === 1 ? "Denied" : "Pending" // Adds a status label
        })).filter(req => req.Status === "Approved")

        console.log("Filtered Time-Off Requests: ", filteredRequests);

        return filteredRequests;
    }

    // Initialize the table
    initialiseWeek();
    populateTable();

    // Attach the navigateWeek function to buttons
    document.getElementById("prev-week-btn").addEventListener("click", () => navigateWeek(-1));
    document.getElementById("next-week-btn").addEventListener("click", () => navigateWeek(1));
    document.getElementById("current-week-btn").addEventListener("click", () => navigateToCurrentWeek());

}

//Pop Up to confirm time off request deletion (users own request)
function showDeleteConfirmationPopUp(id, date) {
    let message;

    if (id == 0) {
        message = "Error: Time-off Request was not found"
    }
    else {
        message = `Are you sure you want to delete the time-off request dated: ${date}?`;

        //send the req ID to the form through a hidden input value
        const hiddenInput = document.querySelector('#deleteConfirmationPopUp input[name="requestId"]');
        hiddenInput.value = id;
    }

    //sets the message
    document.getElementById('deleteConfirmationMessage').textContent = message;

    //show the popUp
    const popUp = new bootstrap.Modal(document.getElementById('deleteConfirmationPopUp'));
    popUp.show();
}

//displays the form for a user to create a timeoff request
function showCreateTimeOffRequestPopUp() {
    //show's the time of request popUp
    const popUp = new bootstrap.Modal(document.getElementById('CreateTimeOffRequestPopUp'));
    popUp.show();
}

//handles invalid dates in the create time off request form in the user dashboard
function validateRequestDate() {
    const today = new Date().toISOString().split("T")[0]; // Get today's date in YYYY-MM-DD format
    const requestDate = document.getElementById("requestDate").value; // Get the value of the date input

    const submitButton = document.getElementById("submitButton");
    const errorMessage = document.getElementById("errorMessage");

    // Check if the request date is valid
    if (requestDate <= today) {
        // Disable the Submit button and show an error message
        submitButton.disabled = true;
        errorMessage.textContent = `Request date must be a date after ${today}.`;
        errorMessage.style.display = "block";
    } else {
        // Enable the Submit button and hide the error message
        submitButton.disabled = false;
        errorMessage.textContent = "";
        errorMessage.style.display = "none";
    }
}