// Card Tag Manager JavaScript
document.addEventListener('DOMContentLoaded', function() {
    initializeSearch();
    initializeFilters();
    initializeFormValidation();
    initializePrintButtons();
    initializeQrCodes();
});

// Initialize search functionality
function initializeSearch() {
    const searchInput = document.getElementById('card-search');
    if (searchInput) {
        searchInput.addEventListener('input', function() {
            const searchTerm = this.value.toLowerCase();
            const cards = document.querySelectorAll('.card-item');
            
            cards.forEach(card => {
                const cardText = card.textContent.toLowerCase();
                if (cardText.includes(searchTerm)) {
                    card.style.display = '';
                } else {
                    card.style.display = 'none';
                }
            });
        });
    }
}

// Initialize filter buttons
function initializeFilters() {
    const allCardsBtn = document.getElementById('all-cards-btn');
    const recentlyUpdatedBtn = document.getElementById('recently-updated-btn');
    
    if (allCardsBtn && recentlyUpdatedBtn) {
        allCardsBtn.addEventListener('click', function() {
            this.classList.add('bg-gray-100');
            recentlyUpdatedBtn.classList.remove('bg-gray-100');
            
            const cards = document.querySelectorAll('.card-item');
            cards.forEach(card => {
                card.style.display = '';
            });
        });
        
        recentlyUpdatedBtn.addEventListener('click', function() {
            this.classList.add('bg-gray-100');
            allCardsBtn.classList.remove('bg-gray-100');
            
            const cards = document.querySelectorAll('.card-item');
            const today = new Date();
            const oneWeekAgo = new Date();
            oneWeekAgo.setDate(today.getDate() - 7);
            
            cards.forEach(card => {
                const updatedDate = new Date(card.getAttribute('data-updated'));
                
                if (updatedDate >= oneWeekAgo) {
                    card.style.display = '';
                } else {
                    card.style.display = 'none';
                }
            });
        });
    }
}

// Initialize form validation
function initializeFormValidation() {
    const forms = document.querySelectorAll('form');
    forms.forEach(form => {
        form.addEventListener('submit', function(e) {
            const requiredFields = form.querySelectorAll('[required]');
            let hasErrors = false;
            
            requiredFields.forEach(field => {
                if (!field.value.trim()) {
                    hasErrors = true;
                    field.classList.add('border-red-500');
                } else {
                    field.classList.remove('border-red-500');
                }
            });
            
            if (hasErrors) {
                e.preventDefault();
                const firstError = form.querySelector('.border-red-500');
                if (firstError) {
                    firstError.focus();
                }
            }
        });
        
        // Live validation
        const inputs = form.querySelectorAll('input, textarea, select');
        inputs.forEach(input => {
            input.addEventListener('blur', function() {
                if (this.hasAttribute('required') && !this.value.trim()) {
                    this.classList.add('border-red-500');
                } else {
                    this.classList.remove('border-red-500');
                }
            });
            
            input.addEventListener('input', function() {
                if (this.classList.contains('border-red-500')) {
                    if (this.value.trim()) {
                        this.classList.remove('border-red-500');
                    }
                }
            });
        });
    });
}

// Initialize print buttons
function initializePrintButtons() {
    const printButtons = document.querySelectorAll('.print-button, [onclick*="window.print()"]');
    printButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            if (this.tagName.toLowerCase() === 'a') {
                e.preventDefault();
            }
            
            window.print();
        });
    });
}

// Initialize QR code generation
function initializeQrCodes() {
    const qrCodeContainers = document.querySelectorAll('.qr-code-container');
    qrCodeContainers.forEach(container => {
        const data = container.getAttribute('data-qr');
        if (data && typeof QRCode !== 'undefined') {
            new QRCode(container, {
                text: data,
                width: 128,
                height: 128,
                colorDark: "#000000",
                colorLight: "#ffffff",
                correctLevel: QRCode.CorrectLevel.H
            });
        }
    });
}

// Copy contact information to clipboard
function copyContactInfo(contactInfo) {
    navigator.clipboard.writeText(contactInfo).then(() => {
        alert('Contact information copied to clipboard');
    }, () => {
        alert('Failed to copy contact information');
    });
}