// Service Code Mapping JavaScript
// ============================================

// Global Variables
let currentRow = 1;
let totalRows = 0;
let completedRows = 0;
let isProcessing = false;
let isPaused = false;
let mappingSessionId = null;
let providerServiceCodes = [];
let nphiesServiceCodes = [];
let apiUrls = {};
let existingMappings = new Map();

// ============================================
// INITIALIZATION FUNCTIONS
// ============================================

// Initialize mapping process when DOM is loaded
function initializeServiceMappingProcess() {
    console.log('Initializing Service mapping process...');

    if (window.mappingData) {
        totalRows = window.mappingData.totalRows;
        mappingSessionId = window.mappingData.sessionId;
        apiUrls = window.mappingData.apiUrls;

        console.log(`Mapping session initialized: ${mappingSessionId}`);

        // Load session data from API
        loadServiceSessionData();

        // Load NPHIES service codes first
        loadNphiesServiceCodes();

        // Check existing mappings
        checkExistingServiceMappings();

        // Show initial loading message
        updateServiceProgressText('🚀 Initializing high-performance AI matching engine (service codes)...');

        // Start the progressive mapping after data is loaded
        setTimeout(() => {
            if (!isPaused) {
                updateServiceProgressText('✅ AI engine ready! Starting progressive mapping...');
                startProgressiveServiceMapping();
            }
        }, 3000);
    } else {
        console.error('No mapping data found. Please ensure the page loaded correctly.');
        showToast('Initialization failed. Please refresh the page.', 'error');
    }
}

// Backwards compatibility alias (if you call the same name from Razor)
function initializeMappingProcess() {
    initializeServiceMappingProcess();
}

// Check existing mappings from database
async function checkExistingServiceMappings() {
    try {
        const response = await fetch(`/ServiceMapping/CheckExistingMappings?sessionId=${mappingSessionId}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        const result = await response.json();

        if (result.success && result.data) {
            result.data.forEach(mapping => {
                existingMappings.set(mapping.providerServiceCodeId, {
                    nphiesServiceCode: mapping.nphiesServiceCode,
                    confidenceScore: mapping.confidenceScore
                });
            });
            console.log(`Loaded ${existingMappings.size} existing service mappings`);
        }
    } catch (error) {
        console.error('Error checking existing service mappings:', error);
    }
}

// Load session data for service mapping
async function loadServiceSessionData() {
    try {
        updateServiceProgressText('Loading session data...');

        const response = await fetch(`${apiUrls.baseUrl}/api/servicemapping/session/${mappingSessionId}`, {
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
            providerServiceCodes = result.data.providerServiceCodes;
            totalRows = result.data.totalRows || providerServiceCodes.length;

            console.log(`Loaded session data: ${totalRows} provider service codes`);
            updateServiceProgressText('Session data loaded successfully.');

            // Render the mapping rows dynamically
            renderServiceMappingRows();

            // Update counters
            updateServiceCounters();

            // Hide loading state and show mapping rows
            document.getElementById('loadingState')?.style && (document.getElementById('loadingState').style.display = 'none');
            document.getElementById('mappingRows')?.style && (document.getElementById('mappingRows').style.display = 'block');

        } else {
            throw new Error(result.message || 'Failed to load session data');
        }

    } catch (error) {
        console.error('Failed to load session data:', error);
        showToast('Failed to load session data. Please refresh the page.', 'error');
        updateServiceProgressText('❌ Failed to load session data.');
    }
}

// Load NPHIES service codes from server
async function loadNphiesServiceCodes() {
    try {
        updateServiceProgressText('Loading NPHIES service code database...');
        console.log('Loading NPHIES service codes from API...');

        const response = await fetch(`${apiUrls.baseUrl}/api/nphiesservicecodes`, {
            method: 'GET',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        nphiesServiceCodes = await response.json();
        console.log(`Successfully loaded ${nphiesServiceCodes.length} NPHIES service codes`);

        updateServiceProgressText('NPHIES service codes loaded successfully. Ready to start mapping...');
        showToast(`Loaded ${nphiesServiceCodes.length} NPHIES service codes successfully`, 'success');

    } catch (error) {
        console.error('Failed to load NPHIES service codes:', error);
        showToast('Failed to load NPHIES service codes. Please refresh the page.', 'error');
        updateServiceProgressText('❌ Failed to load NPHIES service codes. Please refresh the page.');
    }
}

// ============================================
// RENDERING AND UI FUNCTIONS
// ============================================

// Enhanced renderMappingRows function with save buttons and status badges
function renderServiceMappingRows() {
    const mappingRowsContainer = document.getElementById('mappingRows');

    if (!mappingRowsContainer) {
        console.error('Mapping rows container not found');
        return;
    }

    let rowsHtml = '';

    providerServiceCodes.forEach((serviceCode, index) => {
        const rowNumber = index + 1;

        rowsHtml += `
            <div class="mapping-row pending" id="row${rowNumber}" data-row="${rowNumber}" data-service-id="${serviceCode.id}">
                <div class="row-header">
                    <div class="row-info">
                        <div class="row-number">${rowNumber}</div>
                        <div>
                            <div class="fw-bold text-dark">${serviceCode.providerServiceCode} - ${serviceCode.serviceName || serviceCode.providerServiceName || ''}</div>
                            <div class="text-muted small mt-1">Provider Service Code: ${serviceCode.providerServiceCode}</div>
                        </div>
                    </div>
                    <div class="row-status pending" id="status-${rowNumber}">
                        <div class="status-dot"></div>
                        <span class="status-text">Pending Analysis</span>
                    </div>
                </div>
                <div class="row-content" style="display: none;">
                    <div class="mapping-grid">
                        <!-- Provider Data Section -->
                        <div class="mapping-section user-data-section">
                            <div class="section-header">
                                <i data-lucide="user" style="width: 16px; height: 16px;"></i>
                                Your Provider Data
                            </div>
                            <div class="data-item">
                                <div class="data-label">Provider Service Code</div>
                                <div class="data-value">${serviceCode.providerServiceCode}</div>
                            </div>
                            <div class="data-item">
                                <div class="data-label">Service Name</div>
                                <div class="data-value">${serviceCode.serviceName || serviceCode.providerServiceName || ''}</div>
                            </div>
                            <div class="data-item">
                                <div class="data-label">Description</div>
                                <div class="data-value">${serviceCode.serviceDescription || serviceCode.providerServiceDescription || 'No description provided'}</div>
                            </div>
                            ${serviceCode.suggestedServiceCode ? `
                                <div class="data-item">
                                    <div class="data-label">Provided Suggested Service Code</div>
                                    <div class="data-value text-success fw-bold">${serviceCode.suggestedServiceCode}</div>
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
                                <span>Analyzing service pattern...</span>
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
                                <!-- NPHIES Service Search Component -->
                                <div class="dropdown-section">
                                    <div class="dropdown-label">
                                        <i data-lucide="database" style="width: 14px; height: 14px; margin-right: 0.5rem;"></i>
                                        Select OR Change NPHIES Service Code
                                        <span class="search-performance-badge">⚡ Fast</span>
                                    </div>
                                    <div class="custom-search-container" id="nphiesSearch${rowNumber}">
                                        <input type="text" 
                                               class="custom-search-input" 
                                               placeholder="Type 3+ chars to search service codes..."
                                               data-row="${rowNumber}"
                                               data-type="nphies"
                                               autocomplete="off">
                                        <div class="custom-search-results" style="display: none;"></div>
                                        <input type="hidden" class="selected-value" name="selectedNphiesServiceCode">
                                    </div>
                                </div>

                                <!-- Provider Search (display only for preview) -->
                                <div style="display:none" id="providerSearch${rowNumber}">
                                    <strong class="selected-value">${serviceCode.providerServiceCode}</strong> - ${serviceCode.serviceName || serviceCode.providerServiceName || ''}
                                </div>

                                <!-- Mapping Preview -->
                                <div class="mapping-preview mt-3" id="mappingPreview${rowNumber}" style="display: none;">
                                    <div class="mapping-title">
                                        <i data-lucide="arrow-right" style="width: 16px; height: 16px; margin-right: 0.5rem;"></i>
                                        Mapping Preview
                                    </div>
                                    <div class="mapping-flow">
                                        <div class="mapping-from">
                                            <div class="mapping-label">Provider Service Code</div>
                                            <div class="mapping-code">${serviceCode.providerServiceCode}</div>
                                        </div>
                                        <div class="mapping-arrow">
                                            <i data-lucide="arrow-right" style="width: 24px; height: 24px;"></i>
                                        </div>
                                        <div class="mapping-to">
                                            <div class="mapping-label">NPHIES Code</div>
                                            <div class="mapping-code" id="selectedNphiesServiceCodeDisplay${rowNumber}">-</div>
                                        </div>
                                    </div>
                                </div>

                                <!-- Action Buttons -->
                                <div class="action-buttons mt-3">
                                    <button id="approve-${rowNumber}" class="btn-action btn-approve" onclick="approveServiceMapping(${rowNumber})" disabled style="opacity: 0.6;">
                                        <i data-lucide="file-check-2" style="width: 16px; height: 16px;"></i>
                                        Approve Mapping
                                    </button>
                                    <button class="btn-action btn-edit" onclick="editServiceMapping(${rowNumber})">
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

    // Initialize custom search components
    initializeAllServiceCustomSearchComponents();

    // Reinitialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    console.log(`Rendered ${providerServiceCodes.length} service mapping rows with high-performance custom search`);
}

// ============================================
// MAIN PROCESSING FUNCTIONS
// ============================================

// Start progressive mapping process
function startProgressiveServiceMapping() {
    console.log('Starting progressive service mapping...');

    if (currentRow <= totalRows && !isProcessing && !isPaused) {
        processNextServiceRow();
    } else {
        console.log('Cannot start mapping:', {
            currentRow,
            totalRows,
            isProcessing,
            isPaused
        });
    }
}

// Backwards compatibility alias (if you call the same name from Razor)
function startProgressiveMapping() {
    startProgressiveServiceMapping();
}

// Process next row in sequence
function processNextServiceRow() {
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
    updateServiceCounters();
    updateServiceProgressText(`Processing row ${currentRow}: AI analyzing service patterns...`);

    // Get AI suggestion for current provider service code
    getServiceAiSuggestion(currentRow);
}

// Get AI suggestion from server (with UI breathing room)
async function getServiceAiSuggestion(rowNum) {
    try {
        const serviceCode = providerServiceCodes[rowNum - 1];

        if (!serviceCode) {
            throw new Error(`Service code not found for row ${rowNum}`);
        }

        console.log(`Getting AI suggestion for row ${rowNum}:`, serviceCode);

        // Update processing message with breathing room
        await updateServiceProcessingMessages(rowNum);

        const requestBody = {
            providerServiceCodeId: serviceCode.id,
            serviceName: serviceCode.serviceName || serviceCode.providerServiceName || '',
            serviceDescription: serviceCode.serviceDescription || serviceCode.providerServiceDescription || '',
            suggestedServiceCode: serviceCode.suggestedServiceCode || '',
            providerServiceCode: serviceCode.providerServiceCode,
            sessionId: mappingSessionId
        };

        console.log('AI suggestion request:', requestBody);

        // Allow UI to breathe before API call
        await new Promise(resolve => setTimeout(resolve, 50));

        const response = await fetch(`${apiUrls.baseUrl}/api/servicemapping/ai-suggestion`, {
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
            completeServiceRow(rowNum, aiResult);
            currentRow++;
            isProcessing = false;

            // Give UI time to update before next row
            setTimeout(() => {
                requestAnimationFrame(() => {
                    startProgressiveServiceMapping();
                });
            }, 100);
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
            completeServiceRow(rowNum, aiResult);
            currentRow++;
            isProcessing = false;
            setTimeout(() => requestAnimationFrame(() => startProgressiveServiceMapping()), 100);
        }, 100);
    }
}

// Smooth processing message updates
async function updateServiceProcessingMessages(rowNum) {
    const processingElement = document.getElementById(`processing${rowNum}`);
    if (processingElement) {
        const messageSpan = processingElement.querySelector('span');
        if (messageSpan) {
            const messages = [
                'Analyzing service terminology...',
                'Searching through service codes...',
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
function completeServiceRow(rowNum, aiResult) {
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
            if (aiResult && aiResult.success && aiResult.suggestedCode) {
                suggestion.innerHTML = createServiceAiSuggestionHtml(aiResult);
            } else {
                suggestion.innerHTML = createServiceNoMatchHtml(aiResult?.message);
            }
            suggestion.style.display = 'block';
        }

        // Show final mapping section
        if (finalMapping) finalMapping.style.display = 'block';

        // Initialize search inputs for this row in next frame
        requestAnimationFrame(() => {
            initializeServiceSearchForRow(rowNum, aiResult);
        });

        completedRows++;
        updateServiceCounters();

        if (completedRows === totalRows) {
            updateServiceProgressText('🎉 All mappings completed! Review and approve the AI suggestions.');
            showToast('All mappings completed successfully!', 'success');
            window.scrollTo({ top: 0, behavior: 'smooth' });
        } else {
            updateServiceProgressText(`✅ Completed ${completedRows}/${totalRows} mappings. Processing next...`);
        }
    });
}

// ============================================
// HTML GENERATION FUNCTIONS
// ============================================

// Create AI suggestion HTML
function createServiceAiSuggestionHtml(aiResult) {
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
function createServiceNoMatchHtml(message = 'No automatic match found') {
    return `
        <div class="ai-result">
            <div class="text-warning fw-bold">
                <i data-lucide="alert-triangle" style="width: 16px; height: 16px; margin-right: 0.5rem;"></i>
                No Automatic Match Found
            </div>
            <div class="text-muted small mt-1">${message || 'No match available'}</div>
            <div class="text-muted small mt-1">Please manually select the appropriate NPHIES service code</div>
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
// SEARCH INTEGRATION FUNCTIONS
// ============================================

// Initialize the individual row search inputs
function initializeServiceSearchForRow(rowNum, aiResult = null) {
    console.log(`Initializing custom search for service row ${rowNum}`);

    try {
        // Setup custom search for this row
        const nphiesInput = document.querySelector(`#nphiesSearch${rowNum} .custom-search-input`);
        const providerInput = document.querySelector(`#providerSearch${rowNum} .custom-search-input`);

        if (nphiesInput) {
            serviceSearchComponent.setupSearchInput(nphiesInput);
        }

        if (providerInput) {
            serviceSearchComponent.setupSearchInput(providerInput);

            // Pre-select current provider service code
            const currentServiceCode = providerServiceCodes[rowNum - 1];
            if (currentServiceCode) {
                const hiddenInput = document.querySelector(`#providerSearch${rowNum} .selected-value`);
                providerInput.value = `${currentServiceCode.providerServiceCode} - ${currentServiceCode.serviceName || currentServiceCode.providerServiceName || ''}`;
                providerInput.classList.add('has-selection');
                if (hiddenInput) hiddenInput.value = currentServiceCode.providerServiceCode;
            }
        }

        // Pre-select AI suggestion if available and high confidence
        if (aiResult && aiResult.success && aiResult.confidence >= 70 && aiResult.suggestedCode) {
            const nphiesHiddenInput = document.querySelector(`#nphiesSearch${rowNum} .selected-value`);

            if (nphiesInput) {
                nphiesInput.value = `${aiResult.suggestedCode.code} - ${aiResult.suggestedCode.description}`;
                nphiesInput.classList.add('has-selection');
            }
            if (nphiesHiddenInput) nphiesHiddenInput.value = aiResult.suggestedCode.code;

            console.log(`Pre-selected AI suggestion for row ${rowNum}: ${aiResult.suggestedCode.code}`);
        }

        // Update mapping preview and button state
        updateServiceMappingPreview(rowNum);
        toggleServiceApproveButton(rowNum);

        console.log(`Custom search initialized successfully for service row ${rowNum}`);

    } catch (error) {
        console.error(`Error initializing custom search for service row ${rowNum}:`, error);
    }
}

// Toggle approve button based on NPHIES selection
function toggleServiceApproveButton(rowNum) {
    const nphiesValue = document.querySelector(`#nphiesSearch${rowNum} .selected-value`)?.value;
    const approveBtn = document.querySelector(`#row${rowNum} .btn-approve`);

    if (approveBtn) {
        if (nphiesValue) {
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

// Update mapping preview
function updateServiceMappingPreview(rowNum) {
    const nphiesValue = document.querySelector(`#nphiesSearch${rowNum} .selected-value`)?.value;
    const previewDiv = document.getElementById(`mappingPreview${rowNum}`);
    const selectedNphiesDiv = document.getElementById(`selectedNphiesServiceCodeDisplay${rowNum}`);

    if (nphiesValue && previewDiv && selectedNphiesDiv) {
        selectedNphiesDiv.textContent = nphiesValue;
        previewDiv.style.display = 'block';

        // Reinitialize Lucide icons
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    } else if (previewDiv) {
        previewDiv.style.display = 'none';
    }
}

// Initialize all custom search components
function initializeAllServiceCustomSearchComponents() {
    console.log('Initializing custom search components for all service rows...');

    // Initialize the search component if not already done
    if (typeof serviceSearchComponent === 'undefined') {
        window.serviceSearchComponent = new ServiceHighPerformanceSearch();
        serviceSearchComponent.initialize();
    }

    // Initialize search inputs for all rendered rows
    serviceSearchComponent.initializeSearchInputs();
}

// High-performance Search Component for Service Mapping
class ServiceHighPerformanceSearch {
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
            this.currentRequests.get(timerKey).abort?.();
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
            // Server-side search for NPHIES service codes
            const response = await fetch(`${apiUrls.baseUrl}/api/nphiesservicecodes/search-code?q=${encodeURIComponent(query)}&limit=${this.maxResults}`, {
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
            // Client-side search for provider service codes
            return this.searchProviderServiceCodes(query);
        }
    }

    searchProviderServiceCodes(query) {
        const queryLower = query.toLowerCase();
        return providerServiceCodes
            .filter(code =>
                (code.providerServiceCode || '').toLowerCase().includes(queryLower) ||
                (code.serviceName || code.providerServiceName || '').toLowerCase().includes(queryLower) ||
                ((code.serviceDescription || code.providerServiceDescription || '')).toLowerCase().includes(queryLower)
            )
            .slice(0, this.maxResults)
            .map(code => ({
                id: code.providerServiceCode,
                text: `${code.providerServiceCode} - ${code.serviceName || code.providerServiceName || ''}`,
                code: code.providerServiceCode,
                description: code.serviceName || code.providerServiceName || ''
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
        if (input) {
            input.value = `${code} - ${description}`;
            input.classList.add('has-selection');
        }
        if (hiddenInput) hiddenInput.value = value;

        // Update preview display text
        const previewDisplay = document.getElementById(`selectedNphiesServiceCodeDisplay${rowNum}`);
        if (previewDisplay) previewDisplay.textContent = value;

        // Hide results
        if (resultsContainer) resultsContainer.style.display = 'none';

        // Update mapping preview and enable approve button
        updateServiceMappingPreview(rowNum);
        toggleServiceApproveButton(rowNum);

        console.log(`Selected ${searchType}: ${value}`);
    }

    handleSearchFocus(event, rowNum, searchType) {
        const input = event.target;
        if (input.value && !input.classList.contains('has-selection')) {
            const resultsContainer = input.parentElement.querySelector('.custom-search-results');
            if (resultsContainer && resultsContainer.children.length > 0) {
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
}

// Initialize the service search component singleton
const serviceSearchComponent = new ServiceHighPerformanceSearch();

// ============================================
// UI UPDATE FUNCTIONS
// ============================================

// Update counters and progress
function updateServiceCounters() {
    const completedEl = document.getElementById('completedCount');
    const processingEl = document.getElementById('processingCount');
    const pendingEl = document.getElementById('pendingCount');
    const totalEl = document.getElementById('totalCount');
    const progressEl = document.getElementById('overallProgress');

    if (completedEl) completedEl.textContent = completedRows;
    if (processingEl) processingEl.textContent = isProcessing ? 1 : 0;
    if (pendingEl) pendingEl.textContent = Math.max(totalRows - completedRows - (isProcessing ? 1 : 0), 0);
    if (totalEl) totalEl.textContent = totalRows;

    // Update progress bar with animation
    if (progressEl) {
        const denominator = Math.max(totalRows, 1);
        const progressPercentage = (completedRows / denominator) * 100;
        progressEl.style.width = progressPercentage + '%';
    }
}

// Update progress text
function updateServiceProgressText(text) {
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

// Backwards compatibility alias
function updateProgressText(text) {
    updateServiceProgressText(text);
}

// ============================================
// CONTROL FUNCTIONS
// ============================================

// Pause/Resume processing
function pauseServiceProcessing() {
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
        updateServiceProgressText('⏸️ Processing paused. Click Resume to continue.');
        showToast('Processing paused', 'info');
        console.log('Processing paused by user');
    } else {
        if (icon) icon.setAttribute('data-lucide', 'pause');
        pauseBtn.innerHTML = '<i data-lucide="pause" style="width: 18px; height: 18px;"></i>Pause Processing';
        updateServiceProgressText('▶️ Processing resumed...');
        showToast('Processing resumed', 'info');
        console.log('Processing resumed by user');

        // Resume processing if there are pending rows
        if (currentRow <= totalRows && !isProcessing) {
            setTimeout(() => startProgressiveServiceMapping(), 500);
        }
    }

    // Reinitialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}

// Backwards compatibility alias
function pauseProcessing() {
    pauseServiceProcessing();
}

// Edit mapping (unlock or open edit mode)
function editServiceMapping(rowNum) {
    console.log(`Opening edit mode for service row ${rowNum}`);

    const row = document.getElementById(`row${rowNum}`);

    // Check if mapping is already approved/locked
    if (row && row.dataset.approved === 'true') {
        unlockServiceMapping(rowNum);
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
        showToast(`Edit mode: Search for a different NPHIES service code for row ${rowNum}`, 'info');

        // Update button states
        toggleServiceApproveButton(rowNum);
        updateServiceMappingPreview(rowNum);
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

// Backwards compatibility alias
function editMapping(rowNum) {
    editServiceMapping(rowNum);
}

// Approve single mapping
async function approveServiceMapping(rowNum) {
    const approveBtn = document.getElementById(`approve-${rowNum}`);

    if (!approveBtn) {
        console.error('Approve button not found for row:', rowNum);
        return;
    }

    // Show loader
    showButtonLoader(approveBtn);

    try {
        // Get the selected mapping
        const selected = getFinalServiceMapping(rowNum);

        if (!selected) {
            showToast('Please select a NPHIES service code before approving', 'warning');
            hideButtonLoader(approveBtn);
            return;
        }

        const mappingRequest = {
            ProviderServiceCode: selected.providerCode,
            NphiesServiceCode: selected.nphiesCode,
            IsAiSuggested: selected.confidence === 'manual' ? false : true,
            ConfidenceScore: selected.confidence,
            MappingSessionId: mappingSessionId
        };

        const response = await fetch('/ServiceMapping/SaveMapping', {
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
            updateServiceMappingStatus(rowNum, true);
            showToast(result.message || 'Mapping saved successfully!', 'success');

            // Add to existing mappings (best-effort: find service by row index)
            const service = providerServiceCodes[rowNum - 1];
            if (service) {
                existingMappings.set(service.id, {
                    nphiesServiceCode: selected.nphiesCode,
                    confidenceScore: selected.confidence
                });
            }

            // Update counters
            updateServiceCounters();

            // Remove edit mode styles
            const row = document.getElementById(`row${rowNum}`);
            if (row) {
                row.classList.remove('edit-mode-row');
            }

            // Set badge to "Saved"
            const statusBadge = document.getElementById(`status-${rowNum}`);
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

// Backwards compatibility alias
function approveMapping(rowNum) {
    approveServiceMapping(rowNum);
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

// Get selected final mapping for a row
function getFinalServiceMapping(rowNum) {
    const providerCodeEl = document.querySelector(`#providerSearch${rowNum} .selected-value`);
    const nphiesHiddenEl = document.querySelector(`#nphiesSearch${rowNum} .selected-value`);
    const aiSuggestionCodeEl = document.querySelector(`#suggestion${rowNum} .fw-bold.text-dark`);
    const confidenceEl = document.querySelector(`#suggestion${rowNum} .confidence-badge`);

    if (!providerCodeEl || !nphiesHiddenEl) return null;

    const providerCode = providerCodeEl.textContent?.trim() || providerCodeEl.value?.trim() || '';
    const nphiesCode = nphiesHiddenEl.value?.trim() || '';

    if (!providerCode || !nphiesCode) return null;

    // If selected NPHIES equals suggested code -> use AI confidence; else manual
    let confidence = 'manual';
    let confidenceINT = 100;

    if (aiSuggestionCodeEl && nphiesCode === aiSuggestionCodeEl.textContent.trim()) {
        const match = confidenceEl?.textContent?.match(/(\d+)%/);
        confidence = match ? `${match[1]}%` : 'manual';
        confidenceINT = match ? parseInt(match[1], 10) : 100;
    }

    return {
        providerCode,
        nphiesCode,
        confidence,
        confidenceINT
    };
}

// Update mapping status UI
function updateServiceMappingStatus(rowNum, isMapped) {
    const statusRow = document.getElementById(`status-${rowNum}`);
    const approveBtn = document.getElementById(`approve-${rowNum}`);

    if (statusRow) {
        const dot = statusRow.querySelector('.status-dot');
        const text = statusRow.querySelector('.status-text');

        if (isMapped) {
            // Replace dot with check icon
            if (dot) {
                dot.outerHTML = '<i class="fas fa-check-circle text-success me-1"></i>';
            }
            // Update text
            if (text) text.textContent = 'Saved';

            // Mark row as approved
            const row = document.getElementById(`row${rowNum}`);
            if (row) row.dataset.approved = 'true';
        } else {
            // Optional fallback for unmapping
            if (!statusRow.querySelector('.status-dot')) {
                const icon = statusRow.querySelector('i.fas.fa-check-circle');
                if (icon) {
                    icon.outerHTML = '<div class="status-dot processing"></div>';
                }
            }
            if (text) text.textContent = 'Pending';
        }
    }

    if (approveBtn) {
        approveBtn.disabled = true;
        approveBtn.style.display = 'none';
    }

    updateServiceGlobalProgress();
}

// Helper function to update global progress (approved vs total)
function updateServiceGlobalProgress() {
    const approvedRows = document.querySelectorAll('.mapping-row[data-approved="true"]').length;
    const progressPercentage = Math.round((approvedRows / Math.max(totalRows, 1)) * 100);

    // Update progress text
    updateServiceProgressText(`✅ ${approvedRows}/${totalRows} mappings approved (${progressPercentage}%)`);

    // Check if all mappings are completed
    if (approvedRows === totalRows && totalRows > 0) {
        updateServiceProgressText('🎉 All mappings completed and approved! Ready for export.');
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

// Unlock/edit an approved mapping
function unlockServiceMapping(rowNum) {
    const row = document.getElementById(`row${rowNum}`);
    const nphiesInput = document.querySelector(`#nphiesSearch${rowNum} .custom-search-input`);
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

            // Reset approve button
            if (approveBtn) {
                approveBtn.disabled = false;
                approveBtn.style.display = '';
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
            updateServiceGlobalProgress();
        }
    }
}

// Approve all high-confidence mappings
async function approveAllService() {
    const highConfidenceMappings = getHighConfidenceServiceMappings();

    if (highConfidenceMappings.length === 0) {
        showToast('No high-confidence mappings found to approve', 'warning');
        return;
    }

    // Show confirmation dialog
    Swal.fire({
        title: 'Are you sure?',
        text: `Do you want to approve ${highConfidenceMappings.length} high-confidence mappings?`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonClass: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, approve!',
        cancelButtonText: 'Cancel'
    }).then(async (result) => {
        if (result.isConfirmed) {
            // Show global loader
            showGlobalLoader(`Saving ${highConfidenceMappings.length} mappings...`);

            try {
                const bulkRequest = {
                    mappings: highConfidenceMappings
                };

                const response = await fetch('/ServiceMapping/SaveBulkMappings', {
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
                        updateServiceMappingStatus(mapping.rowMapped, true);
                        existingMappings.set(mapping.providerServiceCodeId, {
                            nphiesServiceCode: mapping.nphiesServiceCode,
                            confidenceScore: mapping.confidenceScore
                        });
                    });

                    showToast(result.message || `Successfully saved ${highConfidenceMappings.length} mappings!`, 'success');
                    updateServiceCounters();
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
    });
}

// Backwards compatibility alias
function approveAll() {
    approveAllService();
}

// Get high-confidence mappings that are ready to be saved
function getHighConfidenceServiceMappings() {
    const mappings = [];
    const threshold = 80; // High confidence threshold
    let i = 1;

    providerServiceCodes.forEach(service => {
        // Skip if already mapped
        if (existingMappings.has(service.id)) {
            i++;
            return;
        }

        const selected = getFinalServiceMapping(i);

        if (selected && selected.confidenceINT >= threshold) {
            mappings.push({
                providerServiceCodeId: service.id,
                nphiesServiceCode: selected.nphiesCode,
                providerServiceCode: selected.providerCode,
                isAiSuggested: true,
                confidenceScore: selected.confidence,
                rowMapped: i,
                mappingSessionId: mappingSessionId
            });
        }
        i++;
    });

    return mappings;
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

// Export mapping results
async function exportServiceResults() {
    console.log('Starting service export process...');

    try {
        updateServiceProgressText('Preparing export...');
        showToast('Exporting service mappings...', 'info');

        const requestBody = {
            sessionId: mappingSessionId,
            includeUnapproved: true
        };

        console.log('Export request:', requestBody);

        const response = await fetch(`${apiUrls.baseUrl}/api/servicemapping/export`, {
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
        link.download = `service-mappings-${mappingSessionId}-${timestamp}.xlsx`;

        // Trigger download
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);

        showToast('Service mappings exported successfully!', 'success');
        updateServiceProgressText('Export completed successfully.');

        console.log('Export completed successfully');

    } catch (error) {
        console.error('Error exporting service mappings:', error);
        showToast('Failed to export service mappings. Please try again.', 'error');
        updateServiceProgressText('Export failed. Please try again.');
    }
}

// Backwards compatibility alias
function exportResults() {
    exportServiceResults();
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
function validateServiceMappingData() {
    if (!mappingSessionId) {
        console.error('No mapping session ID found');
        return false;
    }

    if (!providerServiceCodes || providerServiceCodes.length === 0) {
        console.error('No provider service codes found');
        return false;
    }

    if (!nphiesServiceCodes || nphiesServiceCodes.length === 0) {
        console.error('No NPHIES service codes loaded');
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
    if (e.code === 'Space' && e.target.tagName !== 'INPUT' && e.target.tagName !== 'SELECT' && e.target.tagName !== 'TEXTAREA') {
        e.preventDefault();
        pauseServiceProcessing();
    }

    // Escape to pause
    if (e.code === 'Escape') {
        if (!isPaused) {
            pauseServiceProcessing();
        }
    }
});

// Initialize everything when DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    console.log('DOM loaded - Service mapping ready for initialization');
    // This function will be called from the Razor view after window.mappingData is set
    serviceSearchComponent.initialize();
});

// ============================================
// GLOBAL EXPORTS
// ============================================

// Export functions for global access (Service Mapping names)
window.initializeServiceMappingProcess = initializeServiceMappingProcess;
window.pauseServiceProcessing = pauseServiceProcessing;
window.approveServiceMapping = approveServiceMapping;
window.editServiceMapping = editServiceMapping;
window.approveAllService = approveAllService;
window.exportServiceResults = exportServiceResults;

// Backwards-compatible exports (if your Razor or other code expects these names)
window.initializeMappingProcess = initializeServiceMappingProcess;
window.pauseProcessing = pauseServiceProcessing;
window.approveMapping = approveServiceMapping;
window.editMapping = editServiceMapping;
window.approveAll = approveAllService;
window.exportResults = exportServiceResults;

// Additional utility exports
window.showToast = showToast;
window.updateProgressText = updateServiceProgressText;

// Debug exports (remove in production)
window.serviceMappingDebug = {
    getCurrentState: () => ({
        currentRow,
        totalRows,
        completedRows,
        isProcessing,
        isPaused,
        mappingSessionId,
        providerServiceCodesCount: providerServiceCodes.length,
        nphiesServiceCodesCount: nphiesServiceCodes.length
    }),
    getProviderServiceCodes: () => providerServiceCodes,
    getNphiesServiceCodes: () => nphiesServiceCodes,
    validateData: validateServiceMappingData,
    restartMapping: () => {
        currentRow = 1;
        completedRows = 0;
        isProcessing = false;
        isPaused = false;
        updateServiceCounters();
        startProgressiveServiceMapping();
    }
};