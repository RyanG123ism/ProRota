
// Function to highlight table weekly rota cells based on their content
function highlightTableCellsforWeeklyRota() {
    const tableRows = document.querySelectorAll("#weeklyRotaTable tbody tr");
    
    tableRows.forEach(row => {
        const cells = row.querySelectorAll("td");

        cells.forEach((cell, index) => {
            // Skip the first column (index 0) which is the Employee column
            if (index !== 0) {
                if (!cell.textContent.trim().includes("OFF")) {
                    cell.style.backgroundColor = "lightgreen";
                }
                if (cell.textContent.trim().includes("HOLIDAY")) {
                    cell.style.backgroundColor = "lightyellow";
                }
            }
        });
    });
}

// Function to highlight pending shifts table cells based on their content
function highlightTableCellsforPendingShifts() {
    const tableRows = document.querySelectorAll("#pendingShiftsTable tbody tr");

    tableRows.forEach(row => {
        const cells = row.querySelectorAll("td");

        cells.forEach((cell, index) => {
            // Skip the first column (index 0) which is the Employee column
            if (index !== 0) {
                if (!cell.textContent.trim().includes("OFF")) {
                    cell.style.backgroundColor = "lightgreen";
                }
                if (cell.textContent.trim().includes("HOLIDAY")) {
                    cell.style.backgroundColor = "lightyellow";
                }
            }
        });
    });
}

//Group of functions repsonsible for managing the pending shifts table in the user dashboard
function initialiseUserRota(shiftsJson, timeOffRequestsJson) {

    const shifts = shiftsJson;//json data sent from the view
    const requests = timeOffRequestsJson;//json data sent from the view

    //maps the approved requests to an array 
    const filteredRequests = mapTimeOffRequests(requests);

    let currentWeekStart; // Tracks the current week's Monday
    const oneWeekMilliseconds = 7 * 24 * 60 * 60 * 1000; // Number of milliseconds in a week

    // Function to initialize the current week based on the earliest shift
    function initialiseWeek() {

        //maps all a users shifts to an array
        const sortedShifts = mapShifts(shifts);

        //sets the first shift date to the first index position
        const firstShiftDate = sortedShifts.length > 0 ? new Date(sortedShifts[0].StartDateTime) : new Date();

        //sets the current week start date by subtracting the day of the week from the day of the month which will always give you a sunday +1
        currentWeekStart = new Date(firstShiftDate);
        currentWeekStart.setDate(currentWeekStart.getDate() - currentWeekStart.getDay() + 1); // Adjust to Monday
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

    // Function to navigate weeks
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

    // Initialize the table
    initialiseWeek();
    populateTable();

    // Attach the navigateWeek function to buttons
    document.getElementById("prev-week-btn").addEventListener("click", () => navigateWeek(-1));
    document.getElementById("next-week-btn").addEventListener("click", () => navigateWeek(1));
    document.getElementById("current-week-btn").addEventListener("click", () => navigateToCurrentWeek());

}

function mapShifts(shifts) {
    //maps the json into individual shift objects, filters out any null values, and orders by ascending
    const sortedShifts = shifts.map(shift => ({
        ...shift,
        StartDate: shift.StartDateTime ? new Date(shift.StartDateTime).toISOString().split('T')[0] : null
    })).filter(shift => shift.StartDate !== null)
        .sort((a, b) => new Date(a.StartDateTime) - new Date(b.StartDateTime));

    console.log("Sorted Shifts: ", sortedShifts);

    return sortedShifts;

}

function mapTimeOffRequests(requests) {
    //filtering the reuqest data to only contain approved requests
    const filteredRequests = requests.map(req => ({
        ...req,
        Status: req.IsApproved === 0 ? "Approved" : req.IsApproved === 1 ? "Denied" : "Pending" // Adds a status label
    })).filter(req => req.Status === "Approved")

    console.log("Filtered Time-Off Requests: ", filteredRequests);

    return filteredRequests;
}

//pop up for user asttempting to delete their own time off requests in the userdashboard
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

function showEditSiteConfigurationPopUp(id, siteName) {
    let message;
    let pageTitle;

    if (id == 0) {
        pageTitle = `ERROR`;
        message = `Error: Could not find ${siteName}`;
        errorPopUp(message);//displays error popUp
    }
    else {
        pageTitle = `Edit ${siteName}'s Configuration`;
    }

    //sets the message
    document.getElementById('EditSiteConfigurationModalLabel').textContent = pageTitle;

    //show the popUp
    const popUp = new bootstrap.Modal(document.getElementById('EditSiteConfigurationPopUp'));
    popUp.show();

}

function showEditTradingTimesPopUp(dayOfWeek) {
    let pageTitle; 

    if (dayOfWeek !== null && dayOfWeek !== undefined) {
        const dayOfWeekEnum = dayOfWeek;//C# dayofWeek enum int value

        const days = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
        const dayOfWeekString = days[dayOfWeekEnum]//returns the string for the chosen day of week by the user

        pageTitle = `Edit ${dayOfWeekString}'s Trading Hours`;

        //send the dayOfWeek enum to BOTH form through a hidden input value
        const hiddenInput = document.querySelector('#EditTradingTimesPopUp input[name="dayOfWeekEnum"]');
        const hiddenInput2 = document.querySelector('#EditTradingTimesPopUp input[name="dayOfWeekEnum2"]'); 
        hiddenInput.value = dayOfWeekEnum;
        hiddenInput2.value = dayOfWeekEnum;
        
    }
    else {
        pageTitle = `Error selecting time of day`;
    }

    document.getElementById('EditTradingTimesModalLabel').textContent = pageTitle;

    //show the popUp
    const popUp = new bootstrap.Modal(document.getElementById('EditTradingTimesPopUp'));
    popUp.show(); 
}

function validateTradingHours() {
    const openingTime = document.getElementById("openingTime").value; //gets the opening time value
    const closingTime = document.getElementById("closingTime").value; //gets the closing time value

    const submitButton = document.getElementById("saveButton");
    const errorMessage = document.getElementById("errorMessageTradingHours");

    //validation
    if (openingTime > closingTime) {
        submitButton.disabled = true;//disable save button
        errorMessage.textContent = `opening time must be before the closing time of: ${closingTime}.`;
        errorMessage.style.display = "block";
    }
    else {        
        submitButton.disabled = false;//enable the save button
        errorMessage.textContent = "";
        errorMessage.style.display = "none";
    }
}

function validateSiteConfigurationForm() {
    const errorMessage = document.getElementById("errorMessage");
    const submitButton = document.getElementById("submitButton");

    const minManagement = parseInt(document.getElementById("minManagement").value, 10);
    const maxManagement = parseInt(document.getElementById("maxManagement").value, 10);

    if (minManagement > maxManagement) {
        submitButton.disabled = true
        errorMessage.textContent = "Minimum management cannot exceed maximum management.";
        errorMessage.style.display = "block";
        return;
    }
    else {
        submitButton.disabled = false;
        errorMessage.textContent = "";
        errorMessage.style.display = "none";
        return;
    }
}



function popUpMessage(message) {
    // Check if the error modal already exists
    let errorModal = document.getElementById('errorModal');

    if (!errorModal) {
        // Create the modal element dynamically
        const modalHtml = `
        <div class="modal fade" id="errorModal" tabindex="-1" aria-labelledby="errorModalLabel" aria-hidden="true">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header bg-danger text-white">
                        <h5 class="modal-title" id="errorModalLabel">Error</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <p id="errorModalMessage"></p>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                    </div>
                </div>
            </div>
        </div>
        `;

        // Append the modal to the body
        document.body.insertAdjacentHTML('beforeend', modalHtml);

        // Get the newly created modal
        errorModal = document.getElementById('errorModal');
    }

    // Set the error message in the modal
    const errorModalMessage = errorModal.querySelector('#errorModalMessage');
    errorModalMessage.textContent = message;

    // Show the modal using Bootstrap
    const bootstrapModal = new bootstrap.Modal(errorModal);
    bootstrapModal.show();
}









