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

    $.ajax({
        url: url,
        type: 'POST',
        data: data,
        success: function (response) {
            console.log('Response received:', response);

            // Check if it's a JSON success response
            if (response && typeof response === 'object' && response.success) {
                $('#mainModal').modal('hide');
                location.reload();
                return;
            }

            // If it's HTML content (validation errors), update modal
            $('#mainModalBody').html(response);
        },
        error: function (xhr) {
            console.log('Error response:', xhr.responseText);
            $('#mainModalBody').html(xhr.responseText || '<div class="alert alert-danger">An error occurred. Please try again.</div>');
        },
        complete: function () {
            // Reset button state
            submitBtn.prop('disabled', false).html(originalBtnText);
        }
    });
});