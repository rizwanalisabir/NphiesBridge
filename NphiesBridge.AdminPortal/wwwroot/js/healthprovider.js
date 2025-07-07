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
    submitBtn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin mr-2"></i>Saving...');

    $.post(url, data)
        .done(function (response) {
            // Check if it's a JSON success response
            if (typeof response === 'object' && response.success) {
                $('#mainModal').modal('hide');
                location.reload();
                return;
            }

            // If it's HTML (validation errors), update the modal content
            if (typeof response === 'string') {
                console.log('Response type:', typeof response);
                console.log('Response content:', response);
                $('#mainModalBody').html('');
                $('#mainModalBody').html(response);
            }
        })
        .fail(function (xhr) {
            // Handle HTTP errors
            if (xhr.responseText) {
                $('#mainModalBody').html(xhr.responseText);
            } else {
                $('#mainModalBody').html('<div class="alert alert-danger">An error occurred. Please try again.</div>');
            }
        })
        .always(function () {
            // Reset button state
            submitBtn.prop('disabled', false).html(originalBtnText);
        });
});