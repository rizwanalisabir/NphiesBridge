(function () {
    const page = document.getElementById('svc-mapping');
    const setup = document.getElementById('svc-setup');

    function notify(msg, type) {
        // Simple Bootstrap alert fallback; replace with your toast if available
        const containerId = page ? 'svc-alerts' : 'svc-upload-msg';
        let container = document.getElementById(containerId);
        if (!container) {
            container = document.createElement('div');
            container.id = containerId;
            (page || document.body).prepend(container);
        }
        container.innerHTML = '<div class="alert alert-' + (type || 'info') + ' mt-2">' + msg + '</div>';
    }

    // Setup page: show spinner on upload
    const uploadBtn = document.getElementById('svc-upload-btn');
    const uploadForm = document.getElementById('svc-upload-form');
    if (uploadForm && uploadBtn) {
        uploadForm.addEventListener('submit', function () {
            uploadBtn.disabled = true;
            uploadBtn.querySelector('.btn-text').classList.add('d-none');
            uploadBtn.querySelector('.spinner-border').classList.remove('d-none');
        });
    }

    if (!page) return;

    // Client-side filter + search (works on the loaded 100 rows)
    const tbl = document.getElementById('svc-table');
    const search = document.getElementById('svc-search');
    const onlyUnmapped = document.getElementById('svc-filter-unmapped');

    function applyFilters() {
        const q = (search?.value || '').toLowerCase();
        const unmappedOnly = !!onlyUnmapped?.checked;

        const rows = tbl.querySelectorAll('tbody tr[data-row]');
        let index = 0;
        rows.forEach(r => {
            const mapped = r.getAttribute('data-mapped') === '1';
            const name = (r.getAttribute('data-name') || '').toLowerCase();
            const rel = (r.getAttribute('data-itemrelation') || '').toLowerCase();
            const nphies = (r.getAttribute('data-nphies') || '').toLowerCase();
            const suggested = (r.getAttribute('data-suggested') || '').toLowerCase();

            let match = true;
            if (unmappedOnly && mapped) match = false;
            if (q) match = name.includes(q) || rel.includes(q) || nphies.includes(q) || suggested.includes(q);

            r.style.display = match ? '' : 'none';
            if (match) {
                index++;
                r.querySelector('td:first-child').textContent = index.toString();
            }
        });
    }

    search?.addEventListener('input', applyFilters);
    onlyUnmapped?.addEventListener('change', applyFilters);
    applyFilters();

    // Inline save via AJAX
    tbl.addEventListener('submit', async function (e) {
        const form = e.target.closest('form.svc-save-form');
        if (!form) return;
        e.preventDefault();

        const btn = form.querySelector('button[type="submit"]');
        const txt = btn.querySelector('.btn-text');
        const spin = btn.querySelector('.spinner-border');
        btn.disabled = true; txt.classList.add('d-none'); spin.classList.remove('d-none');

        try {
            const formData = new FormData(form);
            const res = await fetch(form.action, {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: formData
            });
            const data = await res.json();
            if (data.success) {
                notify('Saved mapping.', 'success');
                const tr = form.closest('tr[data-row]');
                if (tr) {
                    tr.setAttribute('data-mapped', '1');
                    const nphiesInput = form.querySelector('input[name="NphiesServiceCode"]');
                    const nphiesCell = tr.children[4];
                    if (nphiesInput && nphiesCell) {
                        nphiesCell.innerHTML = '<span class="badge bg-info">' + nphiesInput.value + '</span>';
                    }
                    applyFilters();
                }
            } else {
                notify(data.message || 'Failed to save mapping.', 'danger');
            }
        } catch (err) {
            notify('Error saving mapping.', 'danger');
        } finally {
            btn.disabled = false; txt.classList.remove('d-none'); spin.classList.add('d-none');
        }
    });

    // Optional: switch Generate/Approve to AJAX by uncommenting below and pointing to Ajax actions
    // const genForm = document.getElementById('svc-generate-form');
    // const apvForm = document.getElementById('svc-approve-form');
    // async function postAjax(form, url) {
    //     const fd = new FormData(form);
    //     const res = await fetch(url, { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest' }, body: fd });
    //     return res.json();
    // }
    // genForm?.addEventListener('submit', async function (e) {
    //     e.preventDefault();
    //     const data = await postAjax(genForm, genForm.action.replace('GenerateSuggestions', 'GenerateSuggestionsAjax'));
    //     notify(data.message || 'Done.', data.success ? 'success' : 'danger');
    //     if (data.success) location.reload();
    // });
    // apvForm?.addEventListener('submit', async function (e) {
    //     e.preventDefault();
    //     const data = await postAjax(apvForm, apvForm.action.replace('ApproveAllHigh', 'ApproveAllHighAjax'));
    //     notify(data.message || 'Done.', data.success ? 'success' : 'danger');
    //     if (data.success) location.reload();
    // });
})();