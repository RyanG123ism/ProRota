function popUpMessageError(message) {
    showModal("Error", message, "bg-danger text-white");
}

function popUpMessageSuccess(message) {
    showModal("Success", message, "bg-success text-white");
}

function popUpMessageAlert(message) {
    showModal("Alert", message, "bg-warning text-dark");
}

function showModal(title, message, headerClass) {
    let modal = document.getElementById("globalModal");

    if (!modal) {
        const modalHtml = `
        <div class="modal fade" id="globalModal" tabindex="-1" aria-labelledby="globalModalLabel" aria-hidden="true">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header ${headerClass}">
                        <h5 class="modal-title" id="globalModalLabel">${title}</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <p id="globalModalMessage"></p>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                    </div>
                </div>
            </div>
        </div>`;

        document.body.insertAdjacentHTML('beforeend', modalHtml);
        modal = document.getElementById("globalModal");
    }

    document.getElementById("globalModalMessage").textContent = message;

    const bootstrapModal = new bootstrap.Modal(modal);
    bootstrapModal.show();
}

