
let editedShifts = {}; // Dictionary to store new and updated shifts

function setupShiftClickListeners() {
    document.querySelectorAll(".shift-cell").forEach(cell => {
        cell.addEventListener("click", function () {
            let userRow = this.closest("tr");
            let userId = userRow.getAttribute("data-user-id");
            let firstName = userRow.getAttribute("data-firstname");
            let lastName = userRow.getAttribute("data-lastname");
            let role = userRow.getAttribute("data-role");
            let shiftDate = this.getAttribute("data-date");
            let dayName = new Date(shiftDate).toLocaleDateString('en-US', { weekday: 'long' });

            let shiftTimeText = this.querySelector(".shift-time")?.textContent; 

            if (shiftTimeText === "HOLIDAY") return; // Prevent clicking on holiday shifts

            shiftTimeText = shiftTimeText || "OFF"; // Now set default if empty
            let [startTime, endTime] = shiftTimeText.includes("-") ? shiftTimeText.split(" - ") : ["", ""];


            // Populate modal with user details
            document.getElementById("modalUserId").value = userId;
            document.getElementById("modalShiftDate").value = shiftDate;
            document.getElementById("modalEmployeeName").textContent = `${firstName} ${lastName}`;
            document.getElementById("modalRole").textContent = role;
            document.getElementById("modalDay").textContent = dayName;
            document.getElementById("modalFullDate").textContent = shiftDate;
            document.getElementById("shiftStartTime").value = startTime;
            document.getElementById("shiftEndTime").value = endTime;

            document.getElementById("addShiftButton").style.display = shiftTimeText === "OFF" ? "block" : "none";
            document.getElementById("editShiftButton").style.display = shiftTimeText !== "OFF" ? "block" : "none";
            document.getElementById("editShiftButton").disabled = true;
            document.getElementById("deleteShiftButton").style.display = shiftTimeText !== "OFF" ? "block" : "none"; // ✅ Show delete button if shift exists

            $("#shiftModal").modal("show");
        });
    });
}

// ✅ Disable "Edit/Add Shift" button if start time is equal to or later than end time
document.getElementById("shiftStartTime").addEventListener("input", validateShiftTime);
document.getElementById("shiftEndTime").addEventListener("input", validateShiftTime);

function validateShiftTime() {
    let startTime = document.getElementById("shiftStartTime").value;
    let endTime = document.getElementById("shiftEndTime").value;

    let isValidTime = startTime < endTime;
    document.getElementById("addShiftButton").disabled = !isValidTime;
    document.getElementById("editShiftButton").disabled = !isValidTime;
}

// ✅ Add/Edit Shift and Update View
document.getElementById("addShiftButton").addEventListener("click", saveShift);
document.getElementById("editShiftButton").addEventListener("click", saveShift);

function saveShift() {
    let userId = document.getElementById("modalUserId").value;
    let shiftDate = document.getElementById("modalShiftDate").value;
    let startTime = document.getElementById("shiftStartTime").value;
    let endTime = document.getElementById("shiftEndTime").value;

    if (!editedShifts[userId]) editedShifts[userId] = {};
    editedShifts[userId][shiftDate] = { startTime, endTime };

    updateTableCell(userId, shiftDate, startTime, endTime); // ✅ Update view dynamically

    $("#shiftModal").modal("hide");
}

// ✅ Delete Shift and Update View
document.getElementById("deleteShiftButton").addEventListener("click", function () {
    let userId = document.getElementById("modalUserId").value;
    let shiftDate = document.getElementById("modalShiftDate").value;

    if (!editedShifts[userId]) {
        editedShifts[userId] = {};
    }

    editedShifts[userId][shiftDate] = { startTime: null, endTime: null };

    updateTableCell(userId, shiftDate, null, null); // ✅ Update view to show "OFF"

    $("#shiftModal").modal("hide");
});

// ✅ Update the View (Without Refreshing)
function updateTableCell(userId, shiftDate, startTime, endTime) {
    let cell = document.querySelector(`[data-user-id="${userId}"] [data-date="${shiftDate}"]`);
    if (!cell) return;

    if (!startTime || !endTime) {
        cell.innerHTML = `<span class="text-muted">OFF</span>`;
        cell.style.backgroundColor = ""; // ✅ Remove background color when deleting a shift
    } else {
        let formattedShift = `${startTime} - ${endTime}`;
        cell.innerHTML = `<span class="shift-time">${formattedShift}</span>`;
        cell.style.backgroundColor = "#d4edda"; // ✅ Light green for new shifts
    }
}


// Function to highlight table weekly rota cells based on their content
function highlightTableCellsforWeeklyRota() {
    const tableRows = document.querySelectorAll("#weeklyRotaTable tbody tr");

    tableRows.forEach(row => {
        const cells = row.querySelectorAll("td");

        cells.forEach((cell, index) => {
            // Skip the first three columns which is the employee, role and rolecategory 
            if (index > 2) {
                if (!cell.textContent.trim().includes("OFF")) {
                    cell.style.backgroundColor = "lightgreen";
                }
                if (cell.textContent.trim().includes("HOLIDAY")) {
                    cell.style.backgroundColor = "lightyellow";
                }
            }
            if (index === 10) {
                cell.style.backgroundColor = "#807C96"
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

function calculateTotalHours() {
    document.querySelectorAll("#weeklyRotaTable tbody tr").forEach(row => {
        let totalMinutes = 0;

        row.querySelectorAll(".shift-cell").forEach(cell => {
            let startTime = cell.getAttribute("data-start-time");
            let endTime = cell.getAttribute("data-end-time");

            console.log(`Shift Data: Start - ${startTime}, End - ${endTime}`); // Debugging

            if (startTime && endTime) {
                let start = parseTime(startTime);
                let end = parseTime(endTime);

                if (start !== null && end !== null) {
                    let shiftDuration = end - start;
                    if (shiftDuration < 0) shiftDuration += 24 * 60; // Handle overnight shifts
                    totalMinutes += shiftDuration;
                }
            }
        });

        // Convert minutes to hours (rounded to 2 decimal places)
        let totalHours = (totalMinutes / 60).toFixed(2);
        row.querySelector(".total-hours").textContent = totalHours;
    });
}

// Helper function to convert "HH:mm" string to total minutes
function parseTime(timeStr) {
    if (!timeStr || timeStr.trim() === "") return null;
    let parts = timeStr.split(":");
    if (parts.length !== 2) return null;
    let hours = parseInt(parts[0], 10);
    let minutes = parseInt(parts[1], 10);
    return hours * 60 + minutes;
}
