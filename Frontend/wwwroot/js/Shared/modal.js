function openModal(id) {
    const stage = document.getElementById(id);
    if (!stage) return;
    stage.classList.add('is-open');
    document.body.classList.add('modal-open');
}

function closeModal(id) {
    const stage = document.getElementById(id);
    if (!stage) return;
    stage.classList.remove('is-open');
    if (!document.querySelector('.modal-stage.is-open')) {
        document.body.classList.remove('modal-open');
    }
}

// Global escape key handler
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        const openModalStage = document.querySelector('.modal-stage.is-open');
        if (openModalStage) {
            closeModal(openModalStage.id);
        }
    }
});

// Global backdrop click handler
document.addEventListener('click', (e) => {
    if (e.target.classList.contains('modal-stage')) {
        closeModal(e.target.id);
    }
    
    // Check if clicked element or its parent has data-close attribute
    const closeBtn = e.target.closest('[data-close]');
    if (closeBtn) {
        const modalId = closeBtn.getAttribute('data-close');
        if (modalId) {
            closeModal(modalId);
        }
    }
});
