(function () {
    const root = document.getElementById('svc-mapping-root');
    if (!root) return;

    const sessionId = root.getAttribute('data-session');
    const highThreshold = parseInt(root.getAttribute('data-high') || '90', 10);
    const loading = document.getElementById('svc-loading');
    const rowsHost = document.getElementById('svc-rows');
    const progText = document.getElementById('svc-progress-text');
    const statMapped = document.getElementById('svc-stat-mapped');
    const statUnmapped = document.getElementById('svc-stat-unmapped');
    const statTotal = document.getElementById('svc-stat-total');
    const statCompletion = document.getElementById('svc-stat-completion');
    const progFill = document.getElementById('svc-overall-progress');
    const btnStart = document.getElementById('svc-start');
    const btnCancel = document.getElementById('svc-cancel');
    const btnApproveHigh = document.getElementById('svc-approve-high');

    const afToken = document.querySelector('#svc-af-form input[name="__RequestVerificationToken"]')?.value || '';
    let cancelRequested = false;

    // Utilities
    function toast(msg, type) {
        const div = document.createElement('div');
        div.className = `alert alert-${type || 'info'}`;
        div.style.position = 'fixed';
        div.style.right = '16px';
        div.style.bottom = '16px';
        div.style.zIndex = '1060';
        div.textContent = msg;
        document.body.appendChild(div);
        setTimeout(() => div.remove(), 2500);
    }
    function escapeHtml(s) { return (s ?? '').toString().replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c])); }
    function escapeAttr(s) { return (s ?? '').toString().replace(/"/g, '&quot;'); }
    function debounce(fn, ms) { let t; return (...a) => { clearTimeout(t); t = setTimeout(() => fn(...a), ms); }; }

    async function postJson(url, body) {
        const res = await fetch(url, {
            method: 'POST',
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'Content-Type': 'application/json',
                'RequestVerificationToken': afToken
            },
            body: JSON.stringify(body || {})
        });
        return res.json();
    }
    async function getJson(url) {
        const res = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        return res.json();
    }

    function setStats(stats) {
        if (!stats) return;
        const mapped = stats.mappedCodes ?? 0;
        const total = stats.totalCodes ?? 0;
        const unmapped = stats.unmappedCodes ?? (total - mapped);
        const pct = Math.round(stats.completionPercentage ?? (total ? (mapped / total) * 100 : 0));
        statMapped.textContent = mapped;
        statUnmapped.textContent = unmapped;
        statTotal.textContent = total;
        statCompletion.textContent = pct + '%';
        progFill.style.width = pct + '%';
        progText.textContent = (stats.status || 'Processing') + ' • updated ' + (stats.lastUpdated ? new Date(stats.lastUpdated).toLocaleString() : 'now');
    }

    function rowCard(r) {
        const isMapped = !!r.isMapped || (!!r.nphiesCode && r.nphiesCode.length > 0);
        const conf = r.confidenceScore || 0;
        const confClass = conf >= highThreshold ? 'badge bg-success' : conf >= 70 ? 'badge bg-warning text-dark' : 'badge bg-secondary';
        const suggest = r.suggestedNphiesCode || '';
        const current = r.nphiesCode || '';

        return `
        <div class="mapping-row ${isMapped ? 'mapped' : ''}" data-id="${r.id}">
            <div class="row-left">
                <div class="row-title">${escapeHtml(r.name || '(no name)')}</div>
                <div class="row-meta">
                    <span class="pill"><i class="zmdi zmdi-link"></i> ${escapeHtml(r.itemRelation || '-')}</span>
                    ${r.itemId ? `<span class="pill"><i class="zmdi zmdi-hashtag"></i> ${escapeHtml(r.itemId)}</span>` : ''}
                </div>
            </div>
            <div class="row-middle">
                <div class="row-section-title">Current NPHIES</div>
                ${current ? `<div class="code-chip code-current">${escapeHtml(current)}</div>` : `<div class="text-muted">—</div>`}
            </div>
            <div class="row-right">
                <div class="row-section-title d-flex align-items-center gap-2">
                    Suggested
                    ${suggest ? `<span class="${confClass}"><span class="svc-conf">${conf}</span>%</span>` : '<span class="badge bg-secondary">—</span>'}
                    <span class="svc-state text-muted small"></span>
                </div>
                <div class="svc-suggest">
                    ${suggest ? `
                        <div class="code-chip code-suggest">${escapeHtml(suggest)}</div>
                        ${r.matchReason ? `<div class="small text-muted mt-1">${escapeHtml(r.matchReason)}</div>` : ''}
                    ` : `<div class="text-muted">No suggestion yet</div>`}
                </div>

                <div class="row-actions">
                    <form class="save-form">
                        <input type="hidden" name="ProviderServiceItemId" value="${r.id}">
                        <input type="hidden" name="IsAiSuggested" value="${!!suggest}">
                        <input type="hidden" name="ConfidenceScore" value="${conf || ''}">
                        <div class="mb-2 d-flex gap-2">
                            <input type="text" class="form-control form-control-sm code-input"
                                   name="NphiesServiceCode"
                                   placeholder="Enter NPHIES service code"
                                   value="${escapeAttr(current || suggest || '')}">
                            <button type="submit" class="btn btn-primary btn-sm">
                                <span class="btn-text">Save</span>
                                <span class="spinner-border spinner-border-sm d-none" role="status" aria-hidden="true"></span>
                            </button>
                        </div>

                        <div class="manual-search">
                            <div class="input-group input-group-sm">
                                <span class="input-group-text">Search</span>
                                <input type="text" class="form-control search-nphies" placeholder="Type code or description...">
                            </div>
                            <div class="search-results list-group list-group-flush" style="display:none; max-height: 220px; overflow:auto;"></div>
                        </div>
                    </form>
                </div>
            </div>
        </div>`;
    }

    function renderRows(items) {
        rowsHost.innerHTML = (items || []).map(rowCard).join('') || `<div class="text-center text-muted py-4">No items found.</div>`;
        attachRowHandlers();
    }

    function attachRowHandlers() {
        // Save
        rowsHost.querySelectorAll('form.save-form').forEach(form => {
            form.addEventListener('submit', async (e) => {
                e.preventDefault();
                const btn = form.querySelector('button[type="submit"]');
                const txt = btn.querySelector('.btn-text');
                const spin = btn.querySelector('.spinner-border');
                btn.disabled = true; txt.classList.add('d-none'); spin.classList.remove('d-none');

                const payload = Object.fromEntries(new FormData(form).entries());
                if (!payload.NphiesServiceCode || !payload.NphiesServiceCode.toString().trim()) {
                    toast('Please enter a NPHIES service code.', 'warning');
                    btn.disabled = false; txt.classList.remove('d-none'); spin.classList.add('d-none');
                    return;
                }
                try {
                    const res = await postJson('/ServiceCodesMapping/SaveRowMappingAjax', payload);
                    if (res?.success) {
                        toast('Saved mapping.', 'success');
                        await refresh();
                    } else {
                        toast(res?.message || 'Failed to save.', 'danger');
                    }
                } catch {
                    toast('Error saving.', 'danger');
                } finally {
                    btn.disabled = false; txt.classList.remove('d-none'); spin.classList.add('d-none');
                }
            });

            // Manual search (typeahead)
            const searchInput = form.querySelector('.search-nphies');
            const resultsBox = form.querySelector('.search-results');
            const codeInput = form.querySelector('.code-input');

            const closeResults = () => { resultsBox.style.display = 'none'; resultsBox.innerHTML = ''; };

            const doSearch = debounce(async (q) => {
                q = (q || '').trim();
                if (q.length < 2) { closeResults(); return; }
                try {
                    const res = await fetch(`/ServiceCodesMapping/SearchNphiesCodesAjax?q=${encodeURIComponent(q)}&limit=10`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                    const json = await res.json();
                    const list = json?.data || [];
                    if (!list.length) {
                        resultsBox.innerHTML = `<div class="list-group-item small text-muted">No matches</div>`;
                        resultsBox.style.display = '';
                        return;
                    }

                    resultsBox.innerHTML = list.map(x => `
                        <button type="button" class="list-group-item list-group-item-action d-flex align-items-center gap-2"
                                data-code="${escapeAttr(x.nphiesServiceCode)}"
                                data-desc="${escapeAttr(x.nphiesServiceDescription || '')}">
                            <span class="badge bg-dark">${escapeHtml(x.nphiesServiceCode)}</span>
                            <span class="text-truncate">${escapeHtml(x.nphiesServiceDescription || '')}</span>
                        </button>
                    `).join('');
                    resultsBox.style.display = '';

                    resultsBox.querySelectorAll('button.list-group-item').forEach(btn => {
                        btn.addEventListener('click', () => {
                            const code = btn.getAttribute('data-code') || '';
                            codeInput.value = code;
                            closeResults();
                            codeInput.focus();
                        });
                    });
                } catch {
                    closeResults();
                }
            }, 250);

            searchInput?.addEventListener('input', e => doSearch(e.target.value));
            document.addEventListener('click', (e) => {
                if (!resultsBox.contains(e.target) && e.target !== searchInput) closeResults();
            });
        });
    }

    async function fetchPage() {
        const data = await getJson(`/ServiceCodesMapping/GetSessionPageAjax?sessionId=${encodeURIComponent(sessionId)}&page=1&pageSize=100`);
        return data?.data;
    }
    async function fetchStats() {
        const data = await getJson(`/ServiceCodesMapping/GetStatisticsAjax?sessionId=${encodeURIComponent(sessionId)}`);
        return data?.data;
    }
    async function refresh() {
        const [page, stats] = await Promise.all([fetchPage(), fetchStats()]);
        setStats(stats);
        renderRows(page?.providerItems || []);
        loading.style.display = 'none';
        rowsHost.style.display = '';
    }

    function markProcessing(rowEl, on) {
        const state = rowEl.querySelector('.svc-state');
        if (!state) return;
        state.innerHTML = on ? `<span class="spinner-border spinner-border-sm text-primary"></span> Processing...` : '';
        rowEl.classList.toggle('processing', !!on);
    }
    function updateRowFromDto(rowEl, dto) {
        const suggestHost = rowEl.querySelector('.svc-suggest');
        const confSpan = rowEl.querySelector('.svc-conf');
        if (suggestHost) {
            if (dto.suggestedNphiesCode) {
                suggestHost.innerHTML = `
                    <div class="code-chip code-suggest">${escapeHtml(dto.suggestedNphiesCode)}</div>
                    ${dto.matchReason ? `<div class="small text-muted mt-1">${escapeHtml(dto.matchReason)}</div>` : ''}
                `;
            } else {
                suggestHost.innerHTML = `<div class="text-muted">No suggestion yet</div>`;
            }
        }
        if (confSpan) confSpan.textContent = (dto.confidenceScore || 0);
        const input = rowEl.querySelector('input[name="NphiesServiceCode"]');
        if (input && !input.value) input.value = dto.suggestedNphiesCode || '';
        if (dto.nphiesCode) rowEl.classList.add('mapped');
    }

    async function processQueue(items, concurrency = 3) {
        cancelRequested = false;
        btnCancel.disabled = false;
        btnStart.disabled = true;

        const candidates = items.filter(r => !r.isMapped && !r.nphiesCode);
        const total = candidates.length;
        let done = 0;
        const inFlight = new Set();

        function updateProgressText() {
            progText.textContent = `AI matching ${done}/${total} • running ${inFlight.size}...`;
        }

        const queue = [...candidates];
        async function worker() {
            while (queue.length && !cancelRequested) {
                const r = queue.shift();
                const rowEl = rowsHost.querySelector(`.mapping-row[data-id="${r.id}"]`);
                if (!rowEl) continue;

                inFlight.add(r.id);
                markProcessing(rowEl, true);
                updateProgressText();

                try {
                    const res = await postJson('/ServiceCodesMapping/GenerateSuggestionForItemAjax', { providerServiceItemId: r.id });
                    if (res?.success && res.data) {
                        updateRowFromDto(rowEl, res.data);
                    } else {
                        const state = rowEl.querySelector('.svc-state');
                        if (state) state.innerHTML = `<span class="text-warning">No suggestion</span>`;
                    }
                } catch {
                    const state = rowEl.querySelector('.svc-state');
                    if (state) state.innerHTML = `<span class="text-danger">Error</span>`;
                } finally {
                    markProcessing(rowEl, false);
                    inFlight.delete(r.id);
                    done++;
                    updateProgressText();
                }
            }
        }

        const workers = Array.from({ length: Math.min(concurrency, total || 1) }, () => worker());
        await Promise.all(workers);

        btnCancel.disabled = true;
        btnStart.disabled = false;

        if (cancelRequested) {
            toast('AI matching cancelled.', 'warning');
        } else {
            toast('AI suggestions completed.', 'success');
        }
        await refresh();
    }

    // Buttons
    btnStart?.addEventListener('click', async () => {
        loading.style.display = '';
        rowsHost.style.display = 'none';
        const page = await fetchPage();
        renderRows(page?.providerItems || []);
        loading.style.display = 'none';
        rowsHost.style.display = '';
        if (!page?.providerItems?.length) return;
        await processQueue(page.providerItems, 3);
    });

    btnCancel?.addEventListener('click', () => {
        cancelRequested = true;
        btnCancel.disabled = true;
    });

    btnApproveHigh?.addEventListener('click', async () => {
        loading.style.display = '';
        rowsHost.style.display = 'none';
        const res = await postJson('/ServiceCodesMapping/ApproveAllHighAjax', { sessionId, max: 100 });
        await refresh();
        toast(res?.message || 'Approved.', res?.success ? 'success' : 'warning');
    });

    // Auto boot like ICD page
    (async function init() {
        try {
            await refresh();
            const page = await fetchPage();
            if (page?.providerItems?.length) {
                await processQueue(page.providerItems, 3);
            }
        } catch {
            loading.style.display = 'none';
            rowsHost.style.display = '';
            rowsHost.innerHTML = `<div class="alert alert-danger">Failed to load mapping session.</div>`;
        }
    })();
})();