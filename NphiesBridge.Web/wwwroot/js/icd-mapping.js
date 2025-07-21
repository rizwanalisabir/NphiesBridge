// ICD-10 Code Mapping JavaScript
// ============================================

// Global Variables
let currentRow = 1;
let totalRows = 0;
let completedRows = 0;
let isProcessing = false;
let isPaused = false;
let mappingSessionId = null;
let hospitalCodes = [];
let nphiesCodes = [];
let apiUrls = {};

// ============================================
// INITIALIZATION FUNCTIONS
// ============================================

// Initialize mapping process when DOM is loaded
function initializeMappingProcess() {
    console.log('Initializing ICD mapping process...');

    if (window.mappingData) {
        totalRows = window.mappingData.totalRows;
        mappingSessionId = window.mappingData.sessionId;
        apiUrls = window.mappingData.apiUrls;

        console.log(`Mapping session initialized: ${mappingSessionId}`);

        // Load session data from API
        loadSessionData();

        // Load NPHIES codes first
        loadNphiesCodes();

        // Start the progressive mapping after data is loaded
        setTimeout(() => {
            if (!isPaused) {
                startProgressiveMapping();
            }
        }, 3000);
    } else {
        console.error('No mapping data found. Please ensure the page loaded correctly.');
        showToast('Initialization failed. Please refresh the page.', 'error');
    }
}
// Updated loadSessionData function with dynamic rendering
async function loadSessionData() {
    try {
        updateProgressText('Loading session data...');

        const response = await fetch(`${apiUrls.baseUrl}/api/icdmapping/session/${mappingSessionId}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();

        if (result.success && result.data) {
            hospitalCodes = result.data.hospitalCodes;
            totalRows = result.data.totalRows;

            console.log(`Loaded session data: ${totalRows} hospital codes`);
            updateProgressText('Session data loaded successfully.');

            // Render the mapping rows dynamically
            renderMappingRows();

            // Update counters
            updateCounters();

            // Hide loading state and show mapping rows
            document.getElementById('loadingState').style.display = 'none';
            document.getElementById('mappingRows').style.display = 'block';

        } else {
            throw new Error(result.message || 'Failed to load session data');
        }

    } catch (error) {
        console.error('Failed to load session data:', error);
        showToast('Failed to load session data. Please refresh the page.', 'error');
        updateProgressText('❌ Failed to load session data.');
    }
}

// New function to dynamically render mapping rows
function renderMappingRows() {
    const mappingRowsContainer = document.getElementById('mappingRows');

    if (!mappingRowsContainer) {
        console.error('Mapping rows container not found');
        return;
    }

    let rowsHtml = '';

    hospitalCodes.forEach((hospitalCode, index) => {
        const rowNumber = index + 1;

        rowsHtml += `
            <div class="mapping-row pending" id="row${rowNumber}" data-row="${rowNumber}" data-hospital-id="${hospitalCode.id}">
                <div class="row-header">
                    <div class="row-info">
                        <div class="row-number">${rowNumber}</div>
                        <div>
                            <div class="fw-bold text-dark">${hospitalCode.hospitalCode} - ${hospitalCode.diagnosisName}</div>
                            <div class="text-muted small mt-1">Hospital Code: ${hospitalCode.hospitalCode}</div>
                        </div>
                    </div>
                    <div class="row-status pending">
                        <div class="status-dot"></div>
                        <span class="status-text">Pending Analysis</span>
                    </div>
                </div>
                <div class="row-content" style="display: none;">
                    <div class="mapping-grid">
                        <!-- User Data Section -->
                        <div class="mapping-section user-data-section">
                            <div class="section-header">
                                <i data-lucide="user" style="width: 16px; height: 16px;"></i>
                                Your Hospital Data
                            </div>
                            <div class="data-item">
                                <div class="data-label">Hospital Code</div>
                                <div class="data-value">${hospitalCode.hospitalCode}</div>
                            </div>
                            <div class="data-item">
                                <div class="data-label">Diagnosis Name</div>
                                <div class="data-value">${hospitalCode.diagnosisName}</div>
                            </div>
                            <div class="data-item">
                                <div class="data-label">Description</div>
                                <div class="data-value">${hospitalCode.diagnosisDescription || 'No description provided'}</div>
                            </div>
                            ${hospitalCode.suggestedIcd10Am ? `
                                <div class="data-item">
                                    <div class="data-label">Provided ICD-10-AM</div>
                                    <div class="data-value text-success fw-bold">${hospitalCode.suggestedIcd10Am}</div>
                                </div>
                            ` : ''}
                        </div>

                        <!-- AI Suggestion Section -->
                        <div class="mapping-section ai-suggestion-section">
                            <div class="section-header">
                                <i data-lucide="brain" style="width: 16px; height: 16px;"></i>
                                AI Suggestion
                            </div>
                            <div class="processing-indicator" id="processing${rowNumber}">
                                <div class="spinner"></div>
                                <span>Analyzing diagnosis pattern...</span>
                            </div>
                            <div style="display: none;" id="suggestion${rowNumber}">
                                <!-- AI suggestion will be populated here -->
                            </div>
                        </div>

                        <!-- Final Mapping Section -->
                        <div class="mapping-section final-mapping-section">
                            <div class="section-header">
                                <i data-lucide="target" style="width: 16px; height: 16px;"></i>
                                Final Mapping
                            </div>
                            <div style="display: none;" id="finalMapping${rowNumber}">
                                <div class="dropdown-section">
                                    <div class="dropdown-label">NPHIES ICD Code</div>
                                    <select class="form-select nphies-select" id="nphiesSelect${rowNumber}" style="width: 100%;" data-hospital-id="${hospitalCode.id}">
                                        <option value="">Select NPHIES Code...</option>
                                    </select>
                                </div>
                                <div class="dropdown-section">
                                    <div class="dropdown-label">Mapped to Hospital</div>
                                    <select class="form-select hospital-select" id="hospitalSelect${rowNumber}" style="width: 100%;">
                                        <option value="${hospitalCode.hospitalCode}">${hospitalCode.hospitalCode} - ${hospitalCode.diagnosisName}</option>
                                    </select>
                                </div>
                                <div class="action-buttons">
                                    <button class="btn-action btn-approve" onclick="approveMapping(${rowNumber})">
                                        <i data-lucide="check" style="width: 16px; height: 16px;"></i>
                                        Approve
                                    </button>
                                    <button class="btn-action btn-edit" onclick="editMapping(${rowNumber})">
                                        <i data-lucide="edit" style="width: 16px; height: 16px;"></i>
                                        Edit
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    });

    // Insert the generated HTML
    mappingRowsContainer.innerHTML = rowsHtml;

    // Reinitialize Lucide icons for the new content
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    console.log(`Rendered ${hospitalCodes.length} mapping rows`);
}
// Load NPHIES codes from server
async function loadNphiesCodes() {
    try {
        updateProgressText('Loading NPHIES code database...');
        console.log('Loading NPHIES codes from API...');

        const response = await fetch(`${apiUrls.baseUrl}/api/nphiescodes`, {
            method: 'GET',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        nphiesCodes = await response.json();
        console.log(`Successfully loaded ${nphiesCodes.length} NPHIES codes`);

        updateProgressText('NPHIES codes loaded successfully. Ready to start mapping...');
        showToast(`Loaded ${nphiesCodes.length} NPHIES codes successfully`, 'success');

    } catch (error) {
        console.error('Failed to load NPHIES codes:', error);
        showToast('Failed to load NPHIES codes. Please refresh the page.', 'error');
        updateProgressText('❌ Failed to load NPHIES codes. Please refresh the page.');
    }
}

// ============================================
// MAIN PROCESSING FUNCTIONS
// ============================================

// Start progressive mapping process
function startProgressiveMapping() {
    console.log('Starting progressive mapping...');

    if (currentRow <= totalRows && !isProcessing && !isPaused) {
        processNextRow();
    } else {
        console.log('Cannot start mapping:', {
            currentRow,
            totalRows,
            isProcessing,
            isPaused
        });
    }
}

// Process next row in sequence
function processNextRow() {
    if (currentRow > totalRows || isPaused) {
        console.log('Processing stopped:', { currentRow, totalRows, isPaused });
        return;
    }

    console.log(`Processing row ${currentRow}...`);
    isProcessing = true;

    const row = document.getElementById(`row${currentRow}`);
    if (!row) {
        console.error(`Row element not found: row${currentRow}`);
        return;
    }

    const rowNumber = row.querySelector('.row-number');
    const statusDiv = row.querySelector('.row-status');
    const statusText = row.querySelector('.status-text');
    const rowContent = row.querySelector('.row-content');

    // Update to processing state
    row.classList.remove('pending');
    row.classList.add('processing');

    if (rowNumber) rowNumber.classList.add('processing');
    if (statusDiv) {
        statusDiv.classList.remove('pending');
        statusDiv.classList.add('processing');
    }
    if (statusText) statusText.textContent = 'AI Processing...';
    if (rowContent) rowContent.style.display = 'block';

    // Update counters and progress
    updateCounters();
    updateProgressText(`Processing row ${currentRow}: AI analyzing diagnosis patterns...`);

    // Get AI suggestion for current hospital code
    getAiSuggestion(currentRow);
}

// Get AI suggestion from server
async function getAiSuggestion(rowNum) {
    try {
        const hospitalCode = hospitalCodes[rowNum - 1];

        if (!hospitalCode) {
            throw new Error(`Hospital code not found for row ${rowNum}`);
        }

        console.log(`Getting AI suggestion for row ${rowNum}:`, hospitalCode);

        // Update processing message
        const processingElement = document.getElementById(`processing${rowNum}`);
        if (processingElement) {
            const messageSpan = processingElement.querySelector('span');
            if (messageSpan) {
                const messages = [
                    'Analyzing medical terminology...',
                    'Searching through 44K codes...',
                    'Applying fuzzy matching...',
                    'Calculating confidence scores...',
                    'Finalizing recommendations...'
                ];

                let messageIndex = 0;
                const messageInterval = setInterval(() => {
                    if (messageIndex < messages.length) {
                        messageSpan.textContent = messages[messageIndex];
                        messageIndex++;
                    } else {
                        clearInterval(messageInterval);
                    }
                }, 800);
            }
        }

        const requestBody = {
            hospitalCodeId: hospitalCode.id,
            diagnosisName: hospitalCode.diagnosisName,
            diagnosisDescription: hospitalCode.diagnosisDescription || '',
            suggestedIcd10Am: hospitalCode.suggestedIcd10Am || '',
            hospitalCode: hospitalCode.hospitalCode,
            sessionId: mappingSessionId
        };

        console.log('AI suggestion request:', requestBody);

        const response = await fetch(`${apiUrls.baseUrl}/api/icdmapping/ai-suggestion`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify(requestBody)
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        console.log(`AI suggestion result for row ${rowNum}:`, result);

        // Extract the actual AI response from the API response wrapper
        const aiResult = result.success ? result.data : result;

        // Simulate realistic processing time (1-3 seconds additional)
        const processingTime = 1000 + Math.random() * 2000;

        setTimeout(() => {
            if (!isPaused) {
                completeRow(rowNum, aiResult);
                currentRow++;
                isProcessing = false;

                // Continue to next row after a brief pause
                setTimeout(() => {
                    startProgressiveMapping();
                }, 800);
            }
        }, processingTime);

    } catch (error) {
        console.error(`Error getting AI suggestion for row ${rowNum}:`, error);

        // Show error state but continue processing
        const aiResult = {
            success: false,
            suggestedCode: null,
            confidence: 0,
            message: error.message || 'AI analysis failed',
            error: true
        };

        setTimeout(() => {
            completeRow(rowNum, aiResult);
            currentRow++;
            isProcessing = false;
            setTimeout(() => startProgressiveMapping(), 800);
        }, 1000);
    }
}
// Complete processing for a row
function completeRow(rowNum, aiResult) {
    console.log(`Completing row ${rowNum}:`, aiResult);

    const row = document.getElementById(`row${rowNum}`);
    if (!row) {
        console.error(`Row element not found: row${rowNum}`);
        return;
    }

    const rowNumber = row.querySelector('.row-number');
    const statusDiv = row.querySelector('.row-status');
    const statusText = row.querySelector('.status-text');
    const processing = document.getElementById(`processing${rowNum}`);
    const suggestion = document.getElementById(`suggestion${rowNum}`);
    const finalMapping = document.getElementById(`finalMapping${rowNum}`);

    // Update to completed state
    row.classList.remove('processing');
    row.classList.add('completed');

    if (rowNumber) {
        rowNumber.classList.remove('processing');
        rowNumber.classList.add('completed');
    }

    if (statusDiv) {
        statusDiv.classList.remove('processing');
        statusDiv.classList.add('completed');
    }

    if (statusText) {
        statusText.textContent = aiResult.success ? 'AI Match Found' : 'Manual Review Required';
    }

    // Hide processing indicator
    if (processing) processing.style.display = 'none';

    // Show AI suggestion
    if (suggestion) {
        if (aiResult.success && aiResult.suggestedCode) {
            suggestion.innerHTML = createAiSuggestionHtml(aiResult);
        } else {
            suggestion.innerHTML = createNoMatchHtml(aiResult.message);
        }
        suggestion.style.display = 'block';
    }

    // Show final mapping section
    if (finalMapping) finalMapping.style.display = 'block';

    // Initialize Select2 for this row
    initializeSelect2ForRow(rowNum, aiResult);

    completedRows++;
    updateCounters();

    if (completedRows === totalRows) {
        updateProgressText('🎉 All mappings completed! Review and approve the AI suggestions.');
        showToast('All mappings completed successfully!', 'success');

        // Auto-scroll to top for review
        window.scrollTo({ top: 0, behavior: 'smooth' });
    } else {
        updateProgressText(`✅ Completed ${completedRows}/${totalRows} mappings. Processing next...`);
    }
}

// ============================================
// HTML GENERATION FUNCTIONS
// ============================================

// Create AI suggestion HTML
function createAiSuggestionHtml(aiResult) {
    const confidenceClass = getConfidenceClass(aiResult.confidence);
    const confidenceText = getConfidenceText(aiResult.confidence);

    return `
        <div class="ai-result">
            <div class="fw-bold text-dark">${aiResult.suggestedCode.code}</div>
            <div class="text-muted small mt-1">${aiResult.suggestedCode.description}</div>
            <div class="mt-2">
                <span class="confidence-badge ${confidenceClass}">${aiResult.confidence}% ${confidenceText}</span>
            </div>
            ${aiResult.matchType ? `<div class="text-muted small mt-1">Match Type: ${aiResult.matchType}</div>` : ''}
        </div>
    `;
}

// Create no match HTML
function createNoMatchHtml(message = 'No automatic match found') {
    return `
        <div class="ai-result">
            <div class="text-warning fw-bold">
                <i data-lucide="alert-triangle" style="width: 16px; height: 16px; margin-right: 0.5rem;"></i>
                No Automatic Match Found
            </div>
            <div class="text-muted small mt-1">${message}</div>
            <div class="text-muted small mt-1">Please manually select the appropriate NPHIES code</div>
            <div class="mt-2">
                <span class="confidence-badge confidence-low">Manual Selection Required</span>
            </div>
        </div>
    `;
}

// Get confidence class based on percentage
function getConfidenceClass(confidence) {
    if (confidence >= 80) return 'confidence-high';
    if (confidence >= 60) return 'confidence-medium';
    return 'confidence-low';
}

// Get confidence text based on percentage
function getConfidenceText(confidence) {
    if (confidence >= 80) return 'High Match';
    if (confidence >= 60) return 'Medium Match';
    return 'Low Match';
}

// ============================================
// SELECT2 INTEGRATION FUNCTIONS
// ============================================

// Initialize Select2 dropdowns for a specific row
function initializeSelect2ForRow(rowNum, aiResult = null) {
    console.log(`Initializing Select2 for row ${rowNum}`);

    try {
        // Initialize NPHIES codes dropdown
        const nphiesSelectId = `#nphiesSelect${rowNum}`;

        // Destroy existing Select2 if it exists
        if ($(nphiesSelectId).hasClass('select2-hidden-accessible')) {
            $(nphiesSelectId).select2('destroy');
        }

        $(nphiesSelectId).select2({
            data: nphiesCodes,
            placeholder: 'Search NPHIES codes...',
            allowClear: true,
            width: '100%',
            templateResult: formatNphiesCode,
            templateSelection: formatNphiesSelection,
            escapeMarkup: function (markup) { return markup; },
            matcher: customMatcher
        });

        // Pre-select AI suggestion if available and high confidence
        if (aiResult && aiResult.success && aiResult.confidence >= 70 && aiResult.suggestedCode) {
            const suggestedCodeId = aiResult.suggestedCode.code;
            console.log(`Pre-selecting AI suggestion: ${suggestedCodeId}`);
            $(nphiesSelectId).val(suggestedCodeId).trigger('change');
        }

        // Initialize hospital codes dropdown (read-only)
        const hospitalSelectId = `#hospitalSelect${rowNum}`;

        if ($(hospitalSelectId).hasClass('select2-hidden-accessible')) {
            $(hospitalSelectId).select2('destroy');
        }

        $(hospitalSelectId).select2({
            placeholder: 'Hospital code mapping',
            allowClear: false,
            width: '100%'
        });

        // Add change event listener for NPHIES select
        $(nphiesSelectId).on('change', function () {
            const selectedValue = $(this).val();
            const approveBtn = document.querySelector(`#row${rowNum} .btn-approve`);

            if (selectedValue && approveBtn) {
                // Enable approve button when a selection is made
                approveBtn.disabled = false;
                approveBtn.style.opacity = '1';

                console.log(`NPHIES code selected for row ${rowNum}: ${selectedValue}`);
            }
        });

        // Reinitialize Lucide icons
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }

    } catch (error) {
        console.error(`Error initializing Select2 for row ${rowNum}:`, error);
        showToast(`Failed to initialize dropdowns for row ${rowNum}`, 'error');
    }
}

// Custom matcher for Select2 search
function customMatcher(params, data) {
    // If there are no search terms, return all data
    if ($.trim(params.term) === '') {
        return data;
    }

    // Skip if there is no 'text' property
    if (typeof data.text === 'undefined') {
        return null;
    }

    // Search in both ID and text
    const searchTerm = params.term.toLowerCase();
    const dataText = data.text.toLowerCase();
    const dataId = (data.id || '').toLowerCase();

    if (dataText.indexOf(searchTerm) > -1 || dataId.indexOf(searchTerm) > -1) {
        return data;
    }

    // Return null if the term should not be displayed
    return null;
}

// Format NPHIES code for display in dropdown
function formatNphiesCode(code) {
    if (!code.id) return code.text;

    const codeId = code.id;
    const description = code.text.replace(codeId + ' - ', '') || code.text;

    return $(`
        <div style="padding: 6px 0; border-bottom: 1px solid #f0f0f0;">
            <div style="font-weight: 600; color: #333; font-size: 0.9rem;">${codeId}</div>
            <div style="font-size: 0.8rem; color: #666; margin-top: 2px; line-height: 1.3;">${description}</div>
        </div>
    `);
}

// Format NPHIES selection for display
function formatNphiesSelection(code) {
    if (!code.id) return code.text;
    return `${code.id} - ${code.text.replace(code.id + ' - ', '')}`;
}

// ============================================
// UI UPDATE FUNCTIONS
// ============================================

// Update counters and progress
function updateCounters() {
    const completedEl = document.getElementById('completedCount');
    const processingEl = document.getElementById('processingCount');
    const pendingEl = document.getElementById('pendingCount');
    const totalEl = document.getElementById('totalCount');
    const progressEl = document.getElementById('overallProgress');

    if (completedEl) completedEl.textContent = completedRows;
    if (processingEl) processingEl.textContent = isProcessing ? 1 : 0;
    if (pendingEl) pendingEl.textContent = totalRows - completedRows - (isProcessing ? 1 : 0);
    if (totalEl) totalEl.textContent = totalRows;

    // Update progress bar with animation
    if (progressEl) {
        const progressPercentage = (completedRows / totalRows) * 100;
        progressEl.style.width = progressPercentage + '%';
    }
}

// Update progress text
function updateProgressText(text) {
    const progressTextEl = document.getElementById('progressText');
    if (progressTextEl) {
        progressTextEl.textContent = text;
    }
    console.log('Progress:', text);
}

// ============================================
// CONTROL FUNCTIONS
// ============================================

// Pause/Resume processing
function pauseProcessing() {
    isPaused = !isPaused;
    const pauseBtn = document.querySelector('.btn-pause');

    if (!pauseBtn) {
        console.error('Pause button not found');
        return;
    }

    const icon = pauseBtn.querySelector('i');

    if (isPaused) {
        if (icon) icon.setAttribute('data-lucide', 'play');
        pauseBtn.innerHTML = '<i data-lucide="play" style="width: 18px; height: 18px;"></i>Resume Processing';
        updateProgressText('⏸️ Processing paused. Click Resume to continue.');
        showToast('Processing paused', 'info');
        console.log('Processing paused by user');
    } else {
        if (icon) icon.setAttribute('data-lucide', 'pause');
        pauseBtn.innerHTML = '<i data-lucide="pause" style="width: 18px; height: 18px;"></i>Pause Processing';
        updateProgressText('▶️ Processing resumed...');
        showToast('Processing resumed', 'info');
        console.log('Processing resumed by user');

        // Resume processing if there are pending rows
        if (currentRow <= totalRows && !isProcessing) {
            setTimeout(() => startProgressiveMapping(), 500);
        }
    }

    // Reinitialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}

// Approve mapping for a specific row
async function approveMapping(rowNum) {
    console.log(`Attempting to approve mapping for row ${rowNum}`);

    const nphiesCode = $(`#nphiesSelect${rowNum}`).val();
    const hospitalCodeId = hospitalCodes[rowNum - 1]?.id;

    if (!nphiesCode) {
        showToast('Please select a NPHIES code before approving', 'warning');
        return;
    }

    if (!hospitalCodeId) {
        showToast('Hospital code not found', 'error');
        return;
    }

    const approveBtn = document.querySelector(`#row${rowNum} .btn-approve`);
    if (!approveBtn) {
        console.error(`Approve button not found for row ${rowNum}`);
        return;
    }

    const originalContent = approveBtn.innerHTML;

    // Show loading state
    approveBtn.classList.add('loading');
    approveBtn.disabled = true;

    try {
        const requestBody = {
            hospitalCodeId: hospitalCodeId,
            nphiesCode: nphiesCode,
            sessionId: mappingSessionId,
            isApproved: true,
            rowNumber: rowNum
        };

        console.log('Saving mapping:', requestBody);

        const response = await fetch(`${apiUrls.baseUrl}/api/icdmapping/save-mapping`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify(requestBody)
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        console.log('Save mapping result:', result);

        if (result.success && result.data?.success) {
            // Visual feedback for successful approval
            approveBtn.classList.remove('loading');
            approveBtn.style.background = 'linear-gradient(135deg, #28a745, #20c997)';
            approveBtn.innerHTML = '<i data-lucide="check-circle" style="width: 16px; height: 16px;"></i>Approved';

            // Add approved styling to row
            const row = document.getElementById(`row${rowNum}`);
            if (row) {
                row.style.background = 'rgba(40, 167, 69, 0.08)';
                row.style.borderColor = '#28a745';
                row.dataset.approved = 'true';
            }

            // Reinitialize Lucide icons
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }

            showToast(`Row ${rowNum} mapping approved successfully!`, 'success');
        } else {
            throw new Error(result.message || 'Failed to save mapping');
        }

    } catch (error) {
        console.error('Error approving mapping:', error);

        // Reset button state
        approveBtn.classList.remove('loading');
        approveBtn.disabled = false;
        approveBtn.innerHTML = originalContent;

        showToast('Failed to approve mapping. Please try again.', 'error');

        // Reinitialize Lucide icons
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    }
}

// Edit mapping for a specific row
function editMapping(rowNum) {
    console.log(`Editing mapping for row ${rowNum}`);

    const nphiesSelect = document.getElementById(`nphiesSelect${rowNum}`);
    if (nphiesSelect) {
        nphiesSelect.focus();
        $(`#nphiesSelect${rowNum}`).select2('open');
        showToast(`Edit mode: Select different NPHIES code for row ${rowNum}`, 'info');
    } else {
        console.error(`NPHIES select not found for row ${rowNum}`);
    }
}

// Approve all high-confidence mappings
async function approveAll() {
    console.log('Starting bulk approval process...');

    let approvedCount = 0;
    let skippedCount = 0;

    // Show progress
    updateProgressText('Bulk approving mappings...');

    for (let i = 1; i <= completedRows; i++) {
        const approveBtn = document.querySelector(`#row${i} .btn-approve`);
        const nphiesCode = $(`#nphiesSelect${i}`).val();
        const row = document.getElementById(`row${i}`);

        // Only approve if button is not disabled, a code is selected, and not already approved
        if (approveBtn && !approveBtn.disabled && nphiesCode && !row?.dataset.approved) {
            try {
                await approveMapping(i);
                approvedCount++;
                console.log(`Approved row ${i}`);

                // Add small delay between approvals to prevent server overload
                await new Promise(resolve => setTimeout(resolve, 300));
            } catch (error) {
                console.error(`Failed to approve row ${i}:`, error);
                skippedCount++;
            }
        } else {
            skippedCount++;
            console.log(`Skipped row ${i} - no selection or already approved`);
        }
    }

    const message = skippedCount > 0
        ? `${approvedCount} mappings approved, ${skippedCount} skipped`
        : `All ${approvedCount} mappings approved successfully!`;

    showToast(message, 'success');
    updateProgressText('Bulk approval completed.');

    console.log(`Bulk approval completed: ${approvedCount} approved, ${skippedCount} skipped`);
}

// Export mapping results
async function exportResults() {
    console.log('Starting export process...');

    try {
        updateProgressText('Preparing export...');
        showToast('Exporting mappings...', 'info');

        const requestBody = {
            sessionId: mappingSessionId,
            includeUnapproved: true
        };

        console.log('Export request:', requestBody);

        const response = await fetch(`${apiUrls.baseUrl}/api/icdmapping/export`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify(requestBody)
        });

        if (!response.ok) {
            // Try to get error message from response
            const errorText = await response.text();
            let errorMessage = 'Export failed';

            try {
                const errorJson = JSON.parse(errorText);
                errorMessage = errorJson.message || errorMessage;
            } catch {
                errorMessage = `HTTP error: ${response.status}`;
            }

            throw new Error(errorMessage);
        }

        // Check if response is JSON (error) or binary (file)
        const contentType = response.headers.get('content-type');

        if (contentType && contentType.includes('application/json')) {
            // Error response
            const errorResult = await response.json();
            throw new Error(errorResult.message || 'Export failed');
        }

        // Handle file download
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;

        // Generate filename with timestamp
        const timestamp = new Date().toISOString().split('T')[0];
        link.download = `icd-mappings-${mappingSessionId}-${timestamp}.xlsx`;

        // Trigger download
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);

        showToast('Mappings exported successfully!', 'success');
        updateProgressText('Export completed successfully.');

        console.log('Export completed successfully');

    } catch (error) {
        console.error('Error exporting mappings:', error);
        showToast('Failed to export mappings. Please try again.', 'error');
        updateProgressText('Export failed. Please try again.');
    }
}
// ============================================
// UTILITY FUNCTIONS
// ============================================

// Show toast notification
function showToast(message, type = 'info') {
    console.log(`Toast [${type}]: ${message}`);

    // Remove existing toast
    const existingToast = document.querySelector('.toast-notification');
    if (existingToast) {
        existingToast.remove();
    }

    // Create toast element
    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${type}`;
    toast.style.animation = 'slideDown 0.3s ease';
    toast.textContent = message;

    document.body.appendChild(toast);

    // Auto remove after 4 seconds
    setTimeout(() => {
        if (toast.parentElement) {
            toast.style.animation = 'slideUp 0.3s ease forwards';
            setTimeout(() => {
                if (toast.parentElement) {
                    toast.remove();
                }
            }, 300);
        }
    }, 4000);
}

// Utility function to debounce API calls
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Validate mapping data
function validateMappingData() {
    if (!mappingSessionId) {
        console.error('No mapping session ID found');
        return false;
    }

    if (!hospitalCodes || hospitalCodes.length === 0) {
        console.error('No hospital codes found');
        return false;
    }

    if (!nphiesCodes || nphiesCodes.length === 0) {
        console.error('No NPHIES codes loaded');
        return false;
    }

    return true;
}

// ============================================
// EVENT HANDLERS
// ============================================

// Handle page visibility changes (pause when tab is hidden)
document.addEventListener('visibilitychange', function () {
    if (document.hidden && isProcessing) {
        console.log('Page hidden during processing - continuing in background');
    } else if (!document.hidden && isPaused) {
        console.log('Page visible again - processing still paused');
    }
});

// Handle window beforeunload to warn about unsaved changes
window.addEventListener('beforeunload', function (e) {
    if (isProcessing || (completedRows > 0 && completedRows < totalRows)) {
        const message = 'You have unsaved mapping progress. Are you sure you want to leave?';
        e.returnValue = message;
        return message;
    }
});

// Handle keyboard shortcuts
document.addEventListener('keydown', function (e) {
    // Spacebar to pause/resume
    if (e.code === 'Space' && e.target.tagName !== 'INPUT' && e.target.tagName !== 'SELECT') {
        e.preventDefault();
        pauseProcessing();
    }

    // Escape to pause
    if (e.code === 'Escape') {
        if (!isPaused) {
            pauseProcessing();
        }
    }
});

// Initialize everything when DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    console.log('DOM loaded - ICD mapping ready for initialization');
    // This function will be called from the Razor view
    // after window.mappingData is set
});

// ============================================
// GLOBAL EXPORTS
// ============================================

// Export functions for global access
window.initializeMappingProcess = initializeMappingProcess;
window.pauseProcessing = pauseProcessing;
window.approveMapping = approveMapping;
window.editMapping = editMapping;
window.approveAll = approveAll;
window.exportResults = exportResults;

// Additional utility exports
window.showToast = showToast;
window.updateProgressText = updateProgressText;

// Debug exports (remove in production)
window.mappingDebug = {
    getCurrentState: () => ({
        currentRow,
        totalRows,
        completedRows,
        isProcessing,
        isPaused,
        mappingSessionId,
        hospitalCodesCount: hospitalCodes.length,
        nphiesCodesCount: nphiesCodes.length
    }),
    getHospitalCodes: () => hospitalCodes,
    getNphiesCodes: () => nphiesCodes,
    validateData: validateMappingData,
    restartMapping: () => {
        currentRow = 1;
        completedRows = 0;
        isProcessing = false;
        isPaused = false;
        updateCounters();
        startProgressiveMapping();
    }
};