let editedShifts = {}; // Dictionary to store new and updated shifts

function setupShiftClickListeners() {
    document.querySelectorAll(".shift-cell:not(.non-clickable)").forEach(cell => {
        cell.addEventListener("click", function () {
            let userRow = this.closest("tr");
            let userId = userRow?.getAttribute("data-user-id");
            let firstName = userRow?.getAttribute("data-firstname");
            let lastName = userRow?.getAttribute("data-lastname");
            let role = userRow?.getAttribute("data-role");
            let shiftDate = this.getAttribute("data-date");
            let dayName = new Date(shiftDate).toLocaleDateString('en-US', { weekday: 'long' });

            let shiftTimeText = this.querySelector(".shift-time")?.textContent || "OFF";
            let [startTime, endTime] = shiftTimeText.includes("-") ? shiftTimeText.split(" - ") : ["", ""];

            if (
                document.getElementById("modalUserId") &&
                document.getElementById("modalShiftDate") &&
                document.getElementById("modalEmployeeName") &&
                document.getElementById("modalRole") &&
                document.getElementById("modalDay") &&
                document.getElementById("modalFullDate") &&
                document.getElementById("shiftStartTime") &&
                document.getElementById("shiftEndTime")
            ) {
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
                document.getElementById("deleteShiftButton").style.display = shiftTimeText !== "OFF" ? "block" : "none";

                $("#shiftModal").modal("show");
            }
        });
    });
}

// Attach validation listener only if inputs exist
const shiftStartInput = document.getElementById("shiftStartTime");
const shiftEndInput = document.getElementById("shiftEndTime");

if (shiftStartInput) shiftStartInput.addEventListener("input", validateShiftTime);
if (shiftEndInput) shiftEndInput.addEventListener("input", validateShiftTime);

function validateShiftTime() {
    const startTime = shiftStartInput?.value;
    const endTime = shiftEndInput?.value;

    const isValidTime = startTime < endTime;

    const addBtn = document.getElementById("addShiftButton");
    const editBtn = document.getElementById("editShiftButton");

    if (addBtn) addBtn.disabled = !isValidTime;
    if (editBtn) editBtn.disabled = !isValidTime;
}

// Add/Edit Shift
const addShiftButton = document.getElementById("addShiftButton");
const editShiftButton = document.getElementById("editShiftButton");

if (addShiftButton) addShiftButton.addEventListener("click", saveShift);
if (editShiftButton) editShiftButton.addEventListener("click", saveShift);

function saveShift() {
    const userId = document.getElementById("modalUserId")?.value;
    const shiftDate = document.getElementById("modalShiftDate")?.value;
    const startTime = document.getElementById("shiftStartTime")?.value;
    const endTime = document.getElementById("shiftEndTime")?.value;

    if (!userId || !shiftDate) return;

    if (!editedShifts[userId]) editedShifts[userId] = {};
    editedShifts[userId][shiftDate] = { startTime, endTime };

    updateTableCell(userId, shiftDate, startTime, endTime);
    $("#shiftModal").modal("hide");
}

// Delete Shift
const deleteBtn = document.getElementById("deleteShiftButton");
if (deleteBtn) {
    deleteBtn.addEventListener("click", function () {
        const userId = document.getElementById("modalUserId")?.value;
        const shiftDate = document.getElementById("modalShiftDate")?.value;

        if (!userId || !shiftDate) return;

        if (!editedShifts[userId]) {
            editedShifts[userId] = {};
        }

        editedShifts[userId][shiftDate] = { startTime: null, endTime: null };

        updateTableCell(userId, shiftDate, null, null);
        $("#shiftModal").modal("hide");
    });
}

function updateTableCell(userId, shiftDate, startTime, endTime) {
    let cell = document.querySelector(`[data-user-id="${userId}"] [data-date="${shiftDate}"]`);
    if (!cell) return;

    if (!startTime || !endTime) {
        cell.innerHTML = `<span class="text-muted">OFF</span>`;
        cell.style.backgroundColor = "";
    } else {
        let formattedShift = `${startTime} - ${endTime}`;
        cell.innerHTML = `<span class="shift-time">${formattedShift}</span>`;
        cell.style.backgroundColor = "#d4edda";
    }
}

// Highlight weekly rota table cells
function highlightTableCellsforWeeklyRota() {
    const table = document.querySelector("#weeklyRotaTable");
    if (!table) return;

    table.querySelectorAll("tbody tr").forEach(row => {
        row.querySelectorAll("td").forEach((cell, index) => {
            if (index > 2) {
                if (!cell.textContent.trim().includes("OFF")) {
                    cell.style.backgroundColor = "lightgreen";
                }
                if (cell.textContent.trim().includes("HOLIDAY")) {
                    cell.style.backgroundColor = "lightyellow";
                }
            }
            if (index === 10) {
                cell.style.backgroundColor = "#807C96";
            }
        });
    });
}

// Highlight pending shifts table cells
function highlightTableCellsforPendingShifts() {
    const table = document.querySelector("#pendingShiftsTable");
    if (!table) return;

    table.querySelectorAll("tbody tr").forEach(row => {
        row.querySelectorAll("td").forEach((cell, index) => {
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
    const table = document.querySelector("#weeklyRotaTable");
    if (!table) return;

    table.querySelectorAll("tbody tr").forEach(row => {
        let totalMinutes = 0;

        row.querySelectorAll(".shift-cell").forEach(cell => {
            let startTime = cell.getAttribute("data-start-time");
            let endTime = cell.getAttribute("data-end-time");

            if (startTime && endTime) {
                let start = parseTime(startTime);
                let end = parseTime(endTime);

                if (start !== null && end !== null) {
                    let shiftDuration = end - start;
                    if (shiftDuration < 0) shiftDuration += 24 * 60;
                    totalMinutes += shiftDuration;
                }
            }
        });

        let totalHours = (totalMinutes / 60).toFixed(2);
        let totalCell = row.querySelector(".total-hours");
        if (totalCell) totalCell.textContent = totalHours;
    });
}

function parseTime(timeStr) {
    if (!timeStr || timeStr.trim() === "") return null;
    let parts = timeStr.split(":");
    if (parts.length !== 2) return null;
    let hours = parseInt(parts[0], 10);
    let minutes = parseInt(parts[1], 10);
    return hours * 60 + minutes;
}
