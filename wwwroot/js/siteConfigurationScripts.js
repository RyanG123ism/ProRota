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

//function validateSiteConfigurationForm() {
//    const errorMessage = document.getElementById("errorMessage");
//    const submitButton = document.getElementById("submitButton");

//    const minManagement = parseInt(document.getElementById("minManagement").value, 10);
//    const maxManagement = parseInt(document.getElementById("maxManagement").value, 10);

//    if (minManagement > maxManagement) {
//        submitButton.disabled = true
//        errorMessage.textContent = "Minimum management cannot exceed maximum management.";
//        errorMessage.style.display = "block";
//        return;
//    }
//    else {
//        submitButton.disabled = false;
//        errorMessage.textContent = "";
//        errorMessage.style.display = "none";
//        return;
//    }
//}








