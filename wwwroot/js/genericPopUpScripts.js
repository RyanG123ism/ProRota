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
                        <h5 class="modal-title" id="errorModalLabel">Attention</h5>
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