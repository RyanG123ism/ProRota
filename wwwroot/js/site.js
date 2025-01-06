
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
function initialisePendingShifts(shiftsJson) {

    console.log("Initializing Pending Shifts...");
    const shifts = shiftsJson;

    let currentWeekStart; // Tracks the current week's Monday
    const oneWeekMilliseconds = 7 * 24 * 60 * 60 * 1000; // Number of milliseconds in a week

    // Function to initialize the current week based on the earliest shift
    function initializeWeek() {
        const sortedShifts = shifts.map(shift => ({
            ...shift,
            StartDate: shift.StartDateTime ? new Date(shift.StartDateTime).toISOString().split('T')[0] : null
        })).filter(shift => shift.StartDate !== null)
            .sort((a, b) => new Date(a.StartDateTime) - new Date(b.StartDateTime));

        const firstShiftDate = sortedShifts.length > 0 ? new Date(sortedShifts[0].StartDateTime) : new Date();
        currentWeekStart = new Date(firstShiftDate);
        currentWeekStart.setDate(currentWeekStart.getDate() - currentWeekStart.getDay() + 1); // Adjust to Monday
    }

    // Function to populate the table for the current week
    function populateTable() {
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
            const td = document.createElement("td");
            if (shift) {
                td.textContent =
                    new Date(shift.StartDateTime).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }) +
                    " - " +
                    new Date(shift.EndDateTime).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
            } else {
                td.textContent = "OFF";
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

    // Initialize the table
    initializeWeek();
    populateTable();

    // Attach the navigateWeek function to buttons
    document.getElementById("prev-week-btn").addEventListener("click", () => navigateWeek(-1));
    document.getElementById("current-week-btn").addEventListener("click", () => initializeWeek() && populateTable());
    document.getElementById("next-week-btn").addEventListener("click", () => navigateWeek(1));
}

// Export to global scope (or use export if using ES modules)
/*window.initialisePendingShifts = initialisePendingShifts;*/




