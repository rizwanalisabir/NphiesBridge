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

    $.post(url, data, function () {
        $('#mainModal').modal('hide');
        location.reload(); // Later: replace only table section via AJAX
    }).fail(function (xhr) {
        $('#mainModalBody').html(xhr.responseText);
    });
});
