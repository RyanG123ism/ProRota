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