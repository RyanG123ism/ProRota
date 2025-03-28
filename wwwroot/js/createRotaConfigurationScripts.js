function calculateTotals() {
    const days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

    days.forEach(day => {
        let total = 0;

        // Select all input fields for the given day
        const inputs = document.querySelectorAll(`.covers-input[data-day='${day}']`);

        if (inputs.length > 0) {
            //Loop through each input and add its value to total
            inputs.forEach(input => {
                total += parseInt(input.value) || 0; // Convert input value to number, default to 0 if empty
            });

            // Update the total row for this day
            document.getElementById(`total-${day}`).textContent = `${day} Covers: ${total}`;
        }
    });
}


function calculateQuaterlyTimes() {
    console.log("Running script to calculate times")
    const days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

    days.forEach(day => {
        const inputs = document.querySelectorAll(`.times-input[data-day='${day}']`);

        if (inputs.length === 0) return; // Skip if no inputs for this day

        // Get open and close times from first element
        const openTimeStr = inputs[0].dataset.openTime;
        const closeTimeStr = inputs[0].dataset.closeTime;

        if (!openTimeStr || !closeTimeStr || openTimeStr === "null" || closeTimeStr === "null") {
            console.warn(`Skipping ${day} due to missing times`);
            return; // Skip if times are null or missing
        }

        // Convert string times to Date objects
        const openTime = new Date(`1970-01-01T${openTimeStr}`);
        const closeTime = new Date(`1970-01-01T${closeTimeStr}`);

        if (isNaN(openTime.getTime()) || isNaN(closeTime.getTime())) {
            console.error(`Invalid time for ${day}: Open (${openTimeStr}), Close (${closeTimeStr})`);
            return; // Skip invalid times
        }

        // Calculate total minutes between open and close time
        const totalMinutes = (closeTime - openTime) / (1000 * 60);

        if (totalMinutes <= 0) {
            console.warn(`Skipping ${day} due to invalid time range`);
            return; // Skip invalid time range
        }

        // Divide by 4 to get each quarter time period
        const quarterMinutes = totalMinutes / 4;

        // Loop through inputs and set the time ranges
        let currentTime = new Date(openTime);
        inputs.forEach((input, index) => {
            let nextTime = new Date(currentTime.getTime() + quarterMinutes * 60 * 1000);
            input.textContent = formatTime(currentTime) + " - " + formatTime(nextTime);
            currentTime = nextTime; // Move to next quarter
        });
    });
}

//helper function to format time as HH:mm (12:00, 14:30, etc.)
function formatTime(date) {
    return date.toTimeString().substring(0, 5);
}

function validateForm() {
    const inputs = document.querySelectorAll('.covers-input');
    const generateButton = document.getElementById('generateRotaButton');
    const calendar = document.getElementById("weekEndingDate");

    function validateInputs() {
        const dateValue = calendar.value;
        let allFilled = true;

        inputs.forEach(input => {
            if (input.value === "" || input.value === null) {
                allFilled = false;
            }
        });

        if (!dateValue) {
            allFilled = false;
        }

        generateButton.disabled = !allFilled;
    }

    //Hook input & date change events
    inputs.forEach(input => {
        input.addEventListener('input', validateInputs);
    });

    calendar.addEventListener('change', validateInputs);

    // Initial validation on page load
    validateInputs();
}


function validateWeekEndingDate() {
    console.log("Running validateWeekEndingDate()...");//debugging

    //getting the value form the calender
    const calender = document.getElementById("weekEndingDate");
    const dateValue = calender.value;

    if (dateValue) {

        //get the next sunday / or todays date if sunday is today
        const today = new Date(Date.now());
        const daysToSunday = (7 - today.getDay()) % 7;
        const sunday = addDays(today, daysToSunday);

        console.log("days to sunday: ", daysToSunday)//debugging

        //get the day of week from the selected date value
        const selectedDate = new Date(dateValue);
        const dayofWeek = selectedDate.getDay();

        //if the date selected is not a sunday
        if (dayofWeek !== 0) {
            alert("Week-ending date must be a sunday")
            calender.value = "";
        }

        if (selectedDate <= sunday) {
            alert(`Cannot create a rota ${dateValue} as the week has already begun. You must create a rota manually`)
            calender.value = "";
        }
    }

    //adds days on to an existing date
    function addDays(date, days) {
        var result = new Date(date);
        result.setDate(result.getDate() + days);
        console.log("addDays() returns: ", result);
        return result;
    }
}
