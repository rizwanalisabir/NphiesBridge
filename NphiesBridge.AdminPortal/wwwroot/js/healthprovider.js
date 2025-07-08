// wwwroot/js/healthprovider.js
function openModal(title, url) {
    $('#mainModalTitle').text(title);
    $('#mainModalBody').html('<div class="text-center p-4">Loading...</div>');
    $('#mainModal').modal('show');
    $.get(url, function (data) {
        $('#mainModalBody').html(data);
    });
}
$(document).on("submit", ".ajax-form", function (e) {
    e.preventDefault();
    var form = $(this);
    var url = form.attr("action");
    var data = form.serialize();

    // Show loading state
    var submitBtn = form.find('button[type="submit"]');
    var originalBtnText = submitBtn.html();
    submitBtn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin mr-2"></i>Processing...');

    $.ajax({
        url: url,
        type: 'POST',
        data: data,
        headers: {
            'X-Requested-With': 'XMLHttpRequest'  // ← This ensures AJAX detection
        },
        success: function (response) {
            console.log('Response type:', typeof response);
            console.log('Response content:', response);

            // Check if it's a JSON success response
            if (typeof response === 'object' && response.success) {
                // Show success toast
                showSuccess(response.message || 'Operation completed successfully!');

                // Close modal and refresh
                $('#mainModal').modal('hide');
                setTimeout(function () {
                    location.reload();
                }, 1000);
                return;
            }

            // If it's HTML (validation errors), update the modal content
            if (typeof response === 'string') {
                $('#mainModalBody').html(response);
            }
        },
        error: function (xhr) {
            console.log('Error response:', xhr.responseText);

            var errorMessage = 'An error occurred. Please try again.';
            try {
                var errorData = JSON.parse(xhr.responseText);
                if (errorData && errorData.message) {
                    errorMessage = errorData.message;
                }
            } catch (e) {
                // Use default error message
            }

            showError(errorMessage);
            $('#mainModal').modal('hide');
        },
        complete: function () {
            // Reset button state
            submitBtn.prop('disabled', false).html(originalBtnText);
        }
    });
});