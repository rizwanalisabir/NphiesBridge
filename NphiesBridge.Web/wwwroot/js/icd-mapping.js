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
let existingMappings = new Map();

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

        // Check existing mappings
        checkExistingMappings();

        // Show initial loading message for 44K codes
        updateProgressText('🚀 Initializing high-performance AI matching engine (44K+ codes)...');

        // Start the progressive mapping after data is loaded
        setTimeout(() => {
            if (!isPaused) {
                updateProgressText('✅ AI engine ready! Starting progressive mapping...');
                startProgressiveMapping();
            }
        }, 3000);
    } else {
        console.error('No mapping data found. Please ensure the page loaded correctly.');
        showToast('Initialization failed. Please refresh the page.', 'error');
    }
}

// Check existing mappings from database
async function checkExistingMappings() {
    try {
        const response = await fetch(`/IcdMapping/CheckExistingMappings?sessionId=${mappingSessionId}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        const result = await response.json();

        if (result.success && result.data) {
            result.data.forEach(mapping => {
                existingMappings.set(mapping.hospitalCodeId, {
                    nphiesIcdCode: mapping.nphiesIcdCode,
                    confidenceScore: mapping.confidenceScore
                });
            });
            console.log(`Loaded ${existingMappings.size} existing mappings`);
        }
    } catch (error) {
        console.error('Error checking existing mappings:', error);
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

// Enhanced renderMappingRows function with save buttons and status badges
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
                    <div class="row-status pending" id="status-${rowNumber}">
                        <div class="status-dot"></div>
                        <span class="status-text">Pending Analysis</span>
                    </div>
                </div>
                <div class="row-content" style="display: none;">
                    <div class="mapping-grid">
                        <!-- User Data Section (40% width) -->
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

                        <!-- AI Suggestion Section (40% width) -->
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

                        <!-- Final Mapping Section (20% width) -->
                        <div class="mapping-section final-mapping-section">
                            <div class="section-header">
                                <i data-lucide="target" style="width: 16px; height: 16px;"></i>
                                Final Mapping
                            </div>
                            <div style="display: none;" id="finalMapping${rowNumber}">
                                <!-- NPHIES Search Component ONLY -->
                                <div class="dropdown-section">
                                    <div class="dropdown-label">
                                        <i data-lucide="database" style="width: 14px; height: 14px; margin-right: 0.5rem;"></i>
                                        Select OR Change NPHIES ICD Code
                                        <span class="search-performance-badge">⚡ Fast</span>
                                    </div>
                                    <div class="custom-search-container" id="nphiesSearch${rowNumber}">
                                        <input type="text" 
                                               class="custom-search-input" 
                                               placeholder="Type 3+ chars to search 44K codes..."
                                               data-row="${rowNumber}"
                                               data-type="nphies"
                                               autocomplete="off">
                                        <div class="custom-search-results" style="display: none;"></div>
                                        <input type="hidden" class="selected-value" name="selectedNphiesCode">
                                    </div>
                                </div>

                                    <div style="display:none" id="hospitalSearch${rowNumber}">
                                        <strong class="selected-value">${hospitalCode.hospitalCode}</strong> - ${hospitalCode.diagnosisName}
                                    </div>

                                <!-- Enhanced Mapping Preview -->
                                <div class="mapping-preview mt-3" id="mappingPreview${rowNumber}" style="display: none;">
                                    <div class="mapping-title">
                                        <i data-lucide="arrow-right" style="width: 16px; height: 16px; margin-right: 0.5rem;"></i>
                                        Mapping Preview
                                    </div>
                                    <div class="mapping-flow">
                                        <div class="mapping-from">
                                            <div class="mapping-label">Hospital Code</div>
                                            <div class="mapping-code">${hospitalCode.hospitalCode}</div>
                                        </div>
                                        <div class="mapping-arrow">
                                            <i data-lucide="arrow-right" style="width: 24px; height: 24px;"></i>
                                        </div>
                                        <div class="mapping-to">
                                            <div class="mapping-label">NPHIES Code</div>
                                            <div class="mapping-code" id="selectedNphiesCodeDisplay${rowNumber}">-</div>
                                        </div>
                                    </div>
                                </div>

                                <!-- Action Buttons -->
                                <div class="action-buttons mt-3">
                                    <button id="approve-${rowNumber}" class="btn-action btn-approve" onclick="approveMapping(${rowNumber})" disabled style="opacity: 0.6;">
                                        <i data-lucide="file-check-2" style="width: 16px; height: 16px;"></i>
                                        Approve Mapping
                                    </button>
                                    <button class="btn-action btn-edit" onclick="editMapping(${rowNumber})">
                                        <i data-lucide="replace" style="width: 16px; height: 16px;"></i>
                                        Change Mapping
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

    // Initialize custom search components instead of Select2
    initializeAllCustomSearchComponents();

    // Reinitialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    console.log(`Rendered ${hospitalCodes.length} mapping rows with high-performance custom search`);
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
// Updated function with UI breathing room
async function getAiSuggestion(rowNum) {
    try {
        const hospitalCode = hospitalCodes[rowNum - 1];

        if (!hospitalCode) {
            throw new Error(`Hospital code not found for row ${rowNum}`);
        }

        console.log(`Getting AI suggestion for row ${rowNum}:`, hospitalCode);

        // Update processing message with breathing room
        await updateProcessingMessages(rowNum);

        const requestBody = {
            hospitalCodeId: hospitalCode.id,
            diagnosisName: hospitalCode.diagnosisName,
            diagnosisDescription: hospitalCode.diagnosisDescription || '',
            suggestedIcd10Am: hospitalCode.suggestedIcd10Am || '',
            hospitalCode: hospitalCode.hospitalCode,
            sessionId: mappingSessionId
        };

        console.log('AI suggestion request:', requestBody);

        // Allow UI to breathe before API call
        await new Promise(resolve => setTimeout(resolve, 50));

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

        const aiResult = result.success ? result.data : result;

        // Allow UI to update before completing row
        await new Promise(resolve => requestAnimationFrame(resolve));

        if (!isPaused) {
            completeRow(rowNum, aiResult);
            currentRow++;
            isProcessing = false;

            // Give UI time to update before next row
            setTimeout(() => {
                requestAnimationFrame(() => {
                    startProgressiveMapping();
                });
            }, 100); // Reduced from 800ms for faster processing
        }

    } catch (error) {
        console.error(`Error getting AI suggestion for row ${rowNum}:`, error);

        const aiResult = {
            success: false,
            suggestedCode: null,
            confidence: 0,
            message: error.message || 'AI analysis failed',
            error: true
        };

        // Allow UI breathing room on error too
        await new Promise(resolve => requestAnimationFrame(resolve));

        setTimeout(() => {
            completeRow(rowNum, aiResult);
            currentRow++;
            isProcessing = false;
            setTimeout(() => requestAnimationFrame(() => startProgressiveMapping()), 100);
        }, 100);
    }
}

// New function for smooth processing message updates
async function updateProcessingMessages(rowNum) {
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

            for (let i = 0; i < messages.length; i++) {
                messageSpan.textContent = messages[i];
                // Allow UI to update between message changes
                await new Promise(resolve => setTimeout(resolve, 200));
                if (isPaused) break;
            }
        }
    }
}
// Complete processing for a row
// Update the completeRow function to be more responsive
function completeRow(rowNum, aiResult) {
    console.log(`Completing row ${rowNum}:`, aiResult);

    // Use requestAnimationFrame for smooth DOM updates
    requestAnimationFrame(() => {
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

        // Initialize Select2 for this row in next frame
        requestAnimationFrame(() => {
            initializeSelect2ForRow(rowNum, aiResult);
        });

        completedRows++;
        updateCounters();

        if (completedRows === totalRows) {
            updateProgressText('🎉 All mappings completed! Review and approve the AI suggestions.');
            showToast('All mappings completed successfully!', 'success');
            window.scrollTo({ top: 0, behavior: 'smooth' });
        } else {
            updateProgressText(`✅ Completed ${completedRows}/${totalRows} mappings. Processing next...`);
        }
    });
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

// Update the individual row initialization
function initializeSelect2ForRow(rowNum, aiResult = null) {
    console.log(`Initializing custom search for row ${rowNum}`);

    try {
        // Setup custom search for this row
        const nphiesInput = document.querySelector(`#nphiesSearch${rowNum} .custom-search-input`);
        const hospitalInput = document.querySelector(`#hospitalSearch${rowNum} .custom-search-input`);

        if (nphiesInput) {
            searchComponent.setupSearchInput(nphiesInput);
        }

        if (hospitalInput) {
            searchComponent.setupSearchInput(hospitalInput);

            // Pre-select current hospital code
            const currentHospitalCode = hospitalCodes[rowNum - 1];
            if (currentHospitalCode) {
                const hiddenInput = document.querySelector(`#hospitalSearch${rowNum} .selected-value`);
                hospitalInput.value = `${currentHospitalCode.hospitalCode} - ${currentHospitalCode.diagnosisName}`;
                hospitalInput.classList.add('has-selection');
                hiddenInput.value = currentHospitalCode.hospitalCode;
            }
        }

        // Pre-select AI suggestion if available and high confidence
        if (aiResult && aiResult.success && aiResult.confidence >= 70 && aiResult.suggestedCode) {
            const nphiesHiddenInput = document.querySelector(`#nphiesSearch${rowNum} .selected-value`);

            nphiesInput.value = `${aiResult.suggestedCode.code} - ${aiResult.suggestedCode.description}`;
            nphiesInput.classList.add('has-selection');
            nphiesHiddenInput.value = aiResult.suggestedCode.code;

            console.log(`Pre-selected AI suggestion for row ${rowNum}: ${aiResult.suggestedCode.code}`);
        }

        // Update mapping preview and button state
        updateMappingPreview(rowNum);
        toggleApproveButton(rowNum);

        console.log(`Custom search initialized successfully for row ${rowNum}`);

    } catch (error) {
        console.error(`Error initializing custom search for row ${rowNum}:`, error);
    }
}
// Toggle approve button based on selections
function toggleApproveButton(rowNum) {
    const nphiesValue = document.querySelector(`#nphiesSearch${rowNum} .selected-value`)?.value;
    const approveBtn = document.querySelector(`#row${rowNum} .btn-approve`);

    if (approveBtn) {
        if (nphiesValue) { // Only check NPHIES code now
            approveBtn.disabled = false;
            approveBtn.style.opacity = '1';
            approveBtn.style.background = 'linear-gradient(135deg, #28a745, #20c997)';
        } else {
            approveBtn.disabled = true;
            approveBtn.style.opacity = '0.6';
            approveBtn.style.background = '#6c757d';
        }
    }
}

// Update these functions to work with custom search
function updateMappingPreview(rowNum) {
    const nphiesValue = document.querySelector(`#nphiesSearch${rowNum} .selected-value`)?.value;
    const previewDiv = document.getElementById(`mappingPreview${rowNum}`);
    const selectedNphiesDiv = document.getElementById(`selectedNphiesCodeDisplay${rowNum}`);

    if (nphiesValue && previewDiv && selectedNphiesDiv) {
        // Get display text for selected NPHIES code
        const nphiesText = document.querySelector(`#nphiesSearch${rowNum} .selected-value`)?.value || nphiesValue;

        selectedNphiesDiv.textContent = nphiesText;
        previewDiv.style.display = 'block';

        // Reinitialize Lucide icons
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    } else if (previewDiv) {
        previewDiv.style.display = 'none';
    }
}

// Format hospital selection for display
function formatHospitalSelection(code) {
    if (!code.id) return code.text;
    return code.text;
}

// Update the initialization function
function initializeAllCustomSearchComponents() {
    console.log('Initializing custom search components for all rows...');

    // Initialize the search component if not already done
    if (typeof searchComponent === 'undefined') {
        window.searchComponent = new HighPerformanceSearch();
        searchComponent.initialize();
    }

    // Initialize search inputs for all rendered rows
    searchComponent.initializeSearchInputs();
}
// Custom Search Component Class
class HighPerformanceSearch {
    constructor() {
        this.searchCache = new Map();
        this.debounceTimers = new Map();
        this.currentRequests = new Map();
        this.minSearchLength = 3;
        this.maxResults = 50;
        this.debounceDelay = 300;
    }

    initialize() {
        // Initialize all search inputs
        document.addEventListener('click', this.handleOutsideClick.bind(this));
        document.addEventListener('keydown', this.handleKeyNavigation.bind(this));

        // Initialize existing search inputs
        this.initializeSearchInputs();
    }

    initializeSearchInputs() {
        const searchInputs = document.querySelectorAll('.custom-search-input');
        searchInputs.forEach(input => {
            this.setupSearchInput(input);
        });
    }

    setupSearchInput(input) {
        const rowNum = input.dataset.row;
        const searchType = input.dataset.type;

        // Remove existing listeners
        input.removeEventListener('input', this.handleSearchInput);
        input.removeEventListener('focus', this.handleSearchFocus);
        input.removeEventListener('blur', this.handleSearchBlur);

        // Add event listeners
        input.addEventListener('input', (e) => this.handleSearchInput(e, rowNum, searchType));
        input.addEventListener('focus', (e) => this.handleSearchFocus(e, rowNum, searchType));
        input.addEventListener('blur', (e) => this.handleSearchBlur(e, rowNum, searchType));
    }

    handleSearchInput(event, rowNum, searchType) {
        const input = event.target;
        const query = input.value.trim();
        const containerId = `${searchType}Search${rowNum}`;
        const resultsContainer = document.querySelector(`#${containerId} .custom-search-results`);

        // Clear previous timer
        const timerKey = `${rowNum}-${searchType}`;
        if (this.debounceTimers.has(timerKey)) {
            clearTimeout(this.debounceTimers.get(timerKey));
        }

        // Cancel previous request
        if (this.currentRequests.has(timerKey)) {
            this.currentRequests.get(timerKey).abort();
        }

        if (query.length < this.minSearchLength) {
            resultsContainer.innerHTML = `
                <div class="search-min-chars">
                    Type at least ${this.minSearchLength} characters to search...
                </div>
            `;
            resultsContainer.style.display = 'block';
            return;
        }

        // Show loading
        resultsContainer.innerHTML = `
            <div class="search-loading">
                <div class="spinner-border spinner-border-sm me-2"></div>
                Searching...
            </div>
        `;
        resultsContainer.style.display = 'block';

        // Debounced search
        const timer = setTimeout(() => {
            this.performSearch(query, rowNum, searchType);
        }, this.debounceDelay);

        this.debounceTimers.set(timerKey, timer);
    }

    async performSearch(query, rowNum, searchType) {
        const timerKey = `${rowNum}-${searchType}`;
        const cacheKey = `${searchType}-${query.toLowerCase()}`;
        const containerId = `${searchType}Search${rowNum}`;
        const resultsContainer = document.querySelector(`#${containerId} .custom-search-results`);

        try {
            let results;

            // Check cache first
            if (this.searchCache.has(cacheKey)) {
                results = this.searchCache.get(cacheKey);
                console.log(`Cache hit for: ${cacheKey}`);
            } else {
                // Perform API search
                const startTime = performance.now();
                results = await this.searchApi(query, searchType);
                const endTime = performance.now();

                console.log(`Search completed in ${Math.round(endTime - startTime)}ms`);

                // Cache results
                this.searchCache.set(cacheKey, results);
            }

            // Display results
            this.displayResults(results, resultsContainer, rowNum, searchType);

        } catch (error) {
            console.error('Search error:', error);
            resultsContainer.innerHTML = `
                <div class="search-no-results">
                    Search failed. Please try again.
                </div>
            `;
        }
    }

    async searchApi(query, searchType) {
        if (searchType === 'nphies') {
            // Server-side search for NPHIES codes
            const response = await fetch(`${apiUrls.baseUrl}/api/nphiescodes/search-code?q=${encodeURIComponent(query)}&limit=${this.maxResults}`, {
                method: 'GET',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            return await response.json();
        } else {
            // Client-side search for hospital codes
            return this.searchHospitalCodes(query);
        }
    }

    searchHospitalCodes(query) {
        const queryLower = query.toLowerCase();
        return hospitalCodes
            .filter(code =>
                code.hospitalCode.toLowerCase().includes(queryLower) ||
                code.diagnosisName.toLowerCase().includes(queryLower) ||
                (code.diagnosisDescription && code.diagnosisDescription.toLowerCase().includes(queryLower))
            )
            .slice(0, this.maxResults)
            .map(code => ({
                id: code.hospitalCode,
                text: `${code.hospitalCode} - ${code.diagnosisName}`,
                code: code.hospitalCode,
                description: code.diagnosisName
            }));
    }

    displayResults(results, container, rowNum, searchType) {
        if (!results || results.length === 0) {
            container.innerHTML = `
                <div class="search-no-results">
                    No results found. Try different keywords.
                </div>
            `;
            return;
        }

        const resultHtml = results.map((result, index) => `
            <div class="search-result-item" 
                 data-value="${result.id || result.code}" 
                 data-row="${rowNum}" 
                 data-type="${searchType}"
                 data-index="${index}">
                <div class="result-code">${result.code || result.id}</div>
                <div class="result-description">${result.description || result.text}</div>
            </div>
        `).join('');

        container.innerHTML = resultHtml;

        // Add click handlers
        container.querySelectorAll('.search-result-item').forEach(item => {
            item.addEventListener('click', () => this.selectResult(item, rowNum, searchType));
        });
    }

    selectResult(item, rowNum, searchType) {
        const value = item.dataset.value;
        const code = item.querySelector('.result-code').textContent;
        const description = item.querySelector('.result-description').textContent;

        const containerId = `${searchType}Search${rowNum}`;
        const input = document.querySelector(`#${containerId} .custom-search-input`);
        const hiddenInput = document.querySelector(`#${containerId} .selected-value`);
        const resultsContainer = document.querySelector(`#${containerId} .custom-search-results`);

        // Update input values
        input.value = `${code} - ${description}`;
        input.classList.add('has-selection');
        hiddenInput.value = value;

        // Hide results
        resultsContainer.style.display = 'none';

        // Update mapping preview and enable approve button
        this.updateMappingPreview(rowNum);
        this.toggleApproveButton(rowNum);

        console.log(`Selected ${searchType}: ${value}`);
    }

    handleSearchFocus(event, rowNum, searchType) {
        const input = event.target;
        if (input.value && !input.classList.contains('has-selection')) {
            const resultsContainer = input.parentElement.querySelector('.custom-search-results');
            if (resultsContainer.children.length > 0) {
                resultsContainer.style.display = 'block';
            }
        }
    }

    handleSearchBlur(event, rowNum, searchType) {
        // Delay hiding to allow click on results
        setTimeout(() => {
            const resultsContainer = event.target.parentElement.querySelector('.custom-search-results');
            if (resultsContainer && !resultsContainer.matches(':hover')) {
                resultsContainer.style.display = 'none';
            }
        }, 200);
    }

    handleOutsideClick(event) {
        if (!event.target.closest('.custom-search-container')) {
            document.querySelectorAll('.custom-search-results').forEach(container => {
                container.style.display = 'none';
            });
        }
    }

    handleKeyNavigation(event) {
        const activeElement = document.activeElement;
        if (!activeElement.classList.contains('custom-search-input')) return;

        const resultsContainer = activeElement.parentElement.querySelector('.custom-search-results');
        if (!resultsContainer || resultsContainer.style.display === 'none') return;

        const items = resultsContainer.querySelectorAll('.search-result-item');
        const currentHighlighted = resultsContainer.querySelector('.search-result-item.highlighted');

        switch (event.key) {
            case 'ArrowDown':
                event.preventDefault();
                this.highlightNext(items, currentHighlighted);
                break;
            case 'ArrowUp':
                event.preventDefault();
                this.highlightPrevious(items, currentHighlighted);
                break;
            case 'Enter':
                event.preventDefault();
                if (currentHighlighted) {
                    currentHighlighted.click();
                }
                break;
            case 'Escape':
                resultsContainer.style.display = 'none';
                break;
        }
    }

    highlightNext(items, current) {
        if (current) current.classList.remove('highlighted');

        const currentIndex = current ? Array.from(items).indexOf(current) : -1;
        const nextIndex = currentIndex < items.length - 1 ? currentIndex + 1 : 0;

        items[nextIndex].classList.add('highlighted');
        items[nextIndex].scrollIntoView({ block: 'nearest' });
    }

    highlightPrevious(items, current) {
        if (current) current.classList.remove('highlighted');

        const currentIndex = current ? Array.from(items).indexOf(current) : items.length;
        const prevIndex = currentIndex > 0 ? currentIndex - 1 : items.length - 1;

        items[prevIndex].classList.add('highlighted');
        items[prevIndex].scrollIntoView({ block: 'nearest' });
    }

    updateMappingPreview(rowNum) {
        const nphiesValue = document.querySelector(`#nphiesSearch${rowNum} .selected-value`).value;
        const hospitalValue = document.querySelector(`#hospitalSearch${rowNum} .selected-value`).value;

        // Your existing preview update logic
        updateMappingPreview(rowNum);
    }

    toggleApproveButton(rowNum) {
        const nphiesValue = document.querySelector(`#nphiesSearch${rowNum} .selected-value`).value;
        const hospitalValue = document.querySelector(`#hospitalSearch${rowNum} .selected-value`).value;

        // Your existing button toggle logic
        toggleApproveButton(rowNum);
    }
}

// Initialize the search component
const searchComponent = new HighPerformanceSearch();

// Update your existing functions
function initializeAllSelect2Dropdowns() {
    // Remove Select2 initialization
    // Initialize custom search instead
    searchComponent.initializeSearchInputs();
}

function initializeSelect2ForRow(rowNum, aiResult = null) {
    // Setup custom search for this row
    const nphiesInput = document.querySelector(`#nphiesSearch${rowNum} .custom-search-input`);
    const hospitalInput = document.querySelector(`#hospitalSearch${rowNum} .custom-search-input`);

    if (nphiesInput) searchComponent.setupSearchInput(nphiesInput);
    if (hospitalInput) searchComponent.setupSearchInput(hospitalInput);

    // Pre-select AI suggestion if available
    if (aiResult && aiResult.success && aiResult.confidence >= 70 && aiResult.suggestedCode) {
        const input = nphiesInput;
        const hiddenInput = document.querySelector(`#nphiesSearch${rowNum} .selected-value`);

        input.value = `${aiResult.suggestedCode.code} - ${aiResult.suggestedCode.description}`;
        input.classList.add('has-selection');
        hiddenInput.value = aiResult.suggestedCode.code;

        searchComponent.updateMappingPreview(rowNum);
        searchComponent.toggleApproveButton(rowNum);
    }
}

// Initialize when DOM loads
document.addEventListener('DOMContentLoaded', function () {
    searchComponent.initialize();
});
// Format hospital code for display in dropdown
function formatHospitalCode(code) {
    if (!code.id) return code.text;

    const codeId = code.id;
    const fullText = code.text;
    const description = code.description || 'No description';

    return $(`
        <div style="padding: 8px 0; border-bottom: 1px solid #f8f9fa;">
            <div style="font-weight: 600; color: #495057; font-size: 0.9rem;">${codeId}</div>
            <div style="font-size: 0.85rem; color: #6c757d; margin-top: 2px; line-height: 1.3;">
                ${fullText.replace(codeId + ' - ', '')}
            </div>
            <div style="font-size: 0.75rem; color: #868e96; margin-top: 2px; font-style: italic;">
                ${description.substring(0, 80)}${description.length > 80 ? '...' : ''}
            </div>
        </div>
    `);
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
        // Add performance badge for fast processing
        if (text.includes('completed') || text.includes('✅')) {
            text += ' <span class="performance-badge">High Performance ⚡</span>';
            progressTextEl.innerHTML = text;
        } else {
            progressTextEl.textContent = text;
        }
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

// Approve single mapping
async function approveMapping(hospitalCodeId) {
    const approveBtn = document.getElementById(`approve-${hospitalCodeId}`);
    //const statusBadge = document.getElementById(`status-${hospitalCodeId}`);

    if (!approveBtn) {
        console.error('Approve button not found for hospital code:', hospitalCodeId);
        return;
    }

    // Show loader
    showButtonLoader(approveBtn);

    try {
        // Get the selected suggestion
        const selectedSuggestion = getFinalMapping(hospitalCodeId);

        if (!selectedSuggestion) {
            showToast('Please select a suggestion before approving', 'warning');
            hideButtonLoader(approveBtn);
            return;
        }

        const mappingRequest = {
            HospitalIcdCode: selectedSuggestion.hospitalCode,
            NphiesIcdCode: selectedSuggestion.nphiesCode,
            IsAiSuggested: selectedSuggestion.confidence == 'manual' ? false : true,
            ConfidenceScore: selectedSuggestion.confidence
        };

        const response = await fetch('/IcdMapping/SaveMapping', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify(mappingRequest)
        });

        const result = await response.json();

        if (result.success) {
            // Update UI to show saved status
            updateMappingStatus(hospitalCodeId, true);
            showToast(result.message || 'Mapping saved successfully!', 'success');

            // Add to existing mappings
            existingMappings.set(hospitalCodeId, {
                nphiesIcdCode: selectedSuggestion.code,
                confidenceScore: selectedSuggestion.confidence
            });

            // Update counters
            updateCounters();

            // After mapping saved successfully:
            updateMappingStatus(hospitalCodeId, true);

            // Remove edit mode border and set original solid border
            const row = document.getElementById(`row${hospitalCodeId}`);
            if (row) {
                row.classList.remove('edit-mode-row');
                // Optionally, restore the original border class if you use one (e.g.):
                // row.classList.add('original-row-border');
            }

            // Set badge to "Saved" with original styling
            const statusBadge = document.getElementById(`status-${hospitalCodeId}`);
            if (statusBadge) {
                statusBadge.innerHTML = `<div class="status-dot"></div>
                        <span class="status-text">Mapping Saved</span>`;
                statusBadge.classList.remove('edit-mode-badge');
                statusBadge.classList.add('saved-badge');
            }
        } else {
            showToast(result.message || 'Failed to save mapping', 'error');
        }
    } catch (error) {
        console.error('Error saving mapping:', error);
        showToast('Network error occurred while saving mapping', 'error');
    } finally {
        hideButtonLoader(approveBtn);
    }
}

// Show button loader
function showButtonLoader(button) {
    const btnText = button.querySelector('.btn-text');
    const btnLoader = button.querySelector('.btn-loader');

    if (btnText) btnText.style.display = 'none';
    if (btnLoader) btnLoader.style.display = 'inline-flex';

    button.disabled = true;
}

// Hide button loader
function hideButtonLoader(button) {
    const btnText = button.querySelector('.btn-text');
    const btnLoader = button.querySelector('.btn-loader');

    if (btnText) btnText.style.display = 'inline-flex';
    if (btnLoader) btnLoader.style.display = 'none';

    button.disabled = false;
}

// Get selected suggestion for a hospital code
function getFinalMapping(hospitalCodeId) {
    const hospitalCodeEl = document.querySelector(`#hospitalSearch${hospitalCodeId} .selected-value`);
    const nphiesCodeEl = document.querySelector(`#selectedNphiesCodeDisplay${hospitalCodeId}`);
    const aiSuggestionCodeEl = document.querySelector(`#suggestion${hospitalCodeId} .fw-bold.text-dark`);
    const confidenceEl = document.querySelector(`#suggestion${hospitalCodeId} .confidence-badge`);

    if (!hospitalCodeEl || !nphiesCodeEl || !aiSuggestionCodeEl) return null;

    const hospitalCode = hospitalCodeEl.textContent.trim();
    const nphiesCode = nphiesCodeEl.textContent.trim();
    const suggestedCode = aiSuggestionCodeEl.textContent.trim();

    let confidence = "manual";
    if (nphiesCode === suggestedCode && confidenceEl) {
        const match = confidenceEl.textContent.match(/(\d+)%/);
        confidence = match ? `${match[1]}%` : "manual";
    }

    return {
        hospitalCode,
        nphiesCode,
        confidence
    };
}

// Helper function to update global progress
function updateGlobalProgress() {
    const approvedRows = document.querySelectorAll('.mapping-row[data-approved="true"]').length;
    const progressPercentage = Math.round((approvedRows / totalRows) * 100);

    // Update progress text
    updateProgressText(`✅ ${approvedRows}/${totalRows} mappings approved (${progressPercentage}%)`);

    // Check if all mappings are completed
    if (approvedRows === totalRows) {
        updateProgressText('🎉 All mappings completed and approved! Ready for export.');
        showToast('All mappings have been approved! You can now export the results.', 'success');

        // Enable export button or show completion message
        const exportBtn = document.querySelector('.btn-export');
        if (exportBtn) {
            exportBtn.style.background = 'linear-gradient(135deg, #28a745, #20c997)';
            exportBtn.style.boxShadow = '0 4px 15px rgba(40, 167, 69, 0.3)';
        }

        // Auto-scroll to top for better UX
        setTimeout(() => {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }, 1000);
    }
}

// Helper function to unlock/edit an approved mapping
function unlockMapping(rowNum) {
    const row = document.getElementById(`row${rowNum}`);
    const nphiesInput = document.querySelector(`#nphiesSearch${rowNum} .custom-search-input`);
    const hospitalInput = document.querySelector(`#hospitalSearch${rowNum} .custom-search-input`);
    const approveBtn = document.querySelector(`#row${rowNum} .btn-approve`);
    const editBtn = document.querySelector(`#row${rowNum} .btn-edit`);

    if (row && row.dataset.approved === 'true') {
        // Confirm unlock
        if (confirm(`Are you sure you want to unlock and edit the mapping for row ${rowNum}?`)) {
            // Reset row state
            row.style.background = '';
            row.style.borderColor = '';
            row.style.borderWidth = '';
            row.dataset.approved = 'false';

            // Re-enable inputs
            if (nphiesInput) {
                nphiesInput.disabled = false;
                nphiesInput.style.background = '';
                nphiesInput.style.cursor = '';
            }

            if (hospitalInput) {
                hospitalInput.disabled = false;
                hospitalInput.style.background = '';
                hospitalInput.style.cursor = '';
            }

            // Reset approve button
            if (approveBtn) {
                approveBtn.disabled = false;
                approveBtn.style.background = '';
                approveBtn.style.boxShadow = '';
                approveBtn.innerHTML = `
                    <i data-lucide="check" style="width: 16px; height: 16px;"></i>
                    Approve
                `;
            }

            // Reset edit button
            if (editBtn) {
                editBtn.disabled = false;
                editBtn.style.background = '';
                editBtn.style.cursor = '';
                editBtn.innerHTML = `
                    <i data-lucide="edit" style="width: 16px; height: 16px;"></i>
                    Edit
                `;
            }

            // Reinitialize Lucide icons
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }

            showToast(`Row ${rowNum} unlocked for editing`, 'info');
            updateGlobalProgress();
        }
    }
}

function editMapping(rowNum) {
    console.log(`Opening edit mode for row ${rowNum}`);

    const row = document.getElementById(`row${rowNum}`);

    // Check if mapping is already approved/locked
    if (row && row.dataset.approved === 'true') {
        unlockMapping(rowNum);
        return;
    }

    // Focus on NPHIES search input for editing
    const nphiesInput = document.querySelector(`#nphiesSearch${rowNum} .custom-search-input`);

    if (nphiesInput) {
        // Clear current selection to allow new search
        nphiesInput.value = '';
        nphiesInput.classList.remove('has-selection');
        const hiddenInput = document.querySelector(`#nphiesSearch${rowNum} .selected-value`);
        if (hiddenInput) hiddenInput.value = '';

        // Focus and open search
        nphiesInput.focus();

        // Show a helpful tooltip
        showToast(`Edit mode: Search for a different NPHIES code for row ${rowNum}`, 'info');

        // Update button states
        toggleApproveButton(rowNum);
        updateMappingPreview(rowNum);
    } else {
        console.error(`NPHIES search input not found for row ${rowNum}`);
    }

    // 1. Show and enable Approve button
    const approveBtn = document.getElementById(`approve-${rowNum}`);
    if (approveBtn) {
        approveBtn.style.display = '';
        approveBtn.disabled = false;
    }

    // 2. Change status badge to "Edit Mode" with blue dot
    const statusBadge = document.getElementById(`status-${rowNum}`);
    if (statusBadge) {
        statusBadge.innerHTML = `<div class="status-dot edit-mode-dot"></div>
                        <span class="status-text">Edit Mode</span>`;
        statusBadge.classList.add('edit-mode-badge');
        statusBadge.classList.remove('bg-success', 'bg-secondary'); // Remove old color classes if any
    }

    // 3. Add dashed light blue border
    if (row) {
        row.classList.add('edit-mode-row');
    }
}

// Approve all high-confidence mappings
async function approveAll() {
    const highConfidenceMappings = getHighConfidenceMappings();

    if (highConfidenceMappings.length === 0) {
        showToast('No high-confidence mappings found to approve', 'warning');
        return;
    }

    // Show confirmation dialog
    if (!confirm(`Are you sure you want to approve ${highConfidenceMappings.length} high-confidence mappings?`)) {
        return;
    }

    // Show global loader
    showGlobalLoader(`Saving ${highConfidenceMappings.length} mappings...`);

    try {
        const bulkRequest = {
            mappings: highConfidenceMappings
        };

        const response = await fetch('/IcdMapping/SaveBulkMappings', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify(bulkRequest)
        });

        const result = await response.json();

        if (result.success) {
            // Update UI for all saved mappings
            highConfidenceMappings.forEach(mapping => {
                updateMappingStatus(mapping.hospitalCodeId, true);
                existingMappings.set(mapping.hospitalCodeId, {
                    nphiesIcdCode: mapping.nphiesIcdCode,
                    confidenceScore: mapping.confidenceScore
                });
            });

            showToast(result.message || `Successfully saved ${highConfidenceMappings.length} mappings!`, 'success');
            updateCounters();
        } else {
            showToast(result.message || 'Failed to save bulk mappings', 'error');
        }
    } catch (error) {
        console.error('Error saving bulk mappings:', error);
        showToast('Network error occurred while saving bulk mappings', 'error');
    } finally {
        hideGlobalLoader();
    }
}
// Hide global loader
function hideGlobalLoader() {
    const loader = document.getElementById('globalLoader');
    if (loader) {
        loader.style.display = 'none';
    }
}

// Show global loader
function showGlobalLoader(message) {
    const loader = document.getElementById('globalLoader') || createGlobalLoader();
    const loaderText = loader.querySelector('.loader-text');

    if (loaderText) {
        loaderText.textContent = message;
    }

    loader.style.display = 'flex';
}

// Create global loader element
function createGlobalLoader() {
    const loader = document.createElement('div');
    loader.id = 'globalLoader';
    loader.className = 'global-loader';
    loader.innerHTML = `
        <div class="loader-content">
            <div class="spinner-border text-primary" role="status"></div>
            <div class="loader-text mt-3">Processing...</div>
        </div>
    `;

    document.body.appendChild(loader);
    return loader;
}

// Update mapping status UI
function updateMappingStatus(hospitalCodeId, isMapped) {
    const statusRow = document.getElementById(`status-${hospitalCodeId}`);
    const approveBtn = document.getElementById(`approve-${hospitalCodeId}`);

    if (statusRow) {
        const dot = statusRow.querySelector('.status-dot');
        const text = statusRow.querySelector('.status-text');

        if (isMapped) {
            // Replace dot with check icon
            if (dot) {
                dot.outerHTML = '<i class="fas fa-check-circle text-success me-1"></i>';
            }
            // Update text
            text.textContent = 'Saved';
        } else {
            // Optional fallback for unmapping
            if (!statusRow.querySelector('.status-dot')) {
                const icon = statusRow.querySelector('i.fas.fa-check-circle');
                if (icon) {
                    icon.outerHTML = '<div class="status-dot processing"></div>';
                }
            }
            text.textContent = 'Pending';
        }
    }

    if (approveBtn) {
        approveBtn.disabled = true;
        approveBtn.style.display = 'none';
    }
}




// Get high-confidence mappings that are ready to be saved
function getHighConfidenceMappings() {
    const mappings = [];
    const threshold = 80; // High confidence threshold

    hospitalCodes.forEach(hospitalCode => {
        // Skip if already mapped
        if (existingMappings.has(hospitalCode.id)) {
            return;
        }

        const selectedSuggestion = getSelectedSuggestion(hospitalCode.id);

        if (selectedSuggestion && selectedSuggestion.confidence >= threshold) {
            mappings.push({
                hospitalCodeId: hospitalCode.id,
                nphiesIcdCode: selectedSuggestion.code,
                isAiSuggested: true,
                confidenceScore: selectedSuggestion.confidence
            });
        }
    });

    return mappings;
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