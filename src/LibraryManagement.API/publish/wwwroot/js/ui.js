// Global Variables
let allBooks = [];
let filteredBooks = [];

// Initialize App
document.addEventListener('DOMContentLoaded', async () => {
    await loadAndDisplayBooks();
});

// Load and Display All Books
async function loadAndDisplayBooks() {
    const container = document.getElementById('booksContainer');
    container.innerHTML = '<div class="loading">Loading books...</div>';
    
    allBooks = await fetchAllBooks();
    filteredBooks = [...allBooks];
    
    displayBooks(filteredBooks);
    updateStats();
}

// Display Books in Grid
function displayBooks(books) {
    const container = document.getElementById('booksContainer');
    
    if (!books || books.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <p>📚 No books found. Start by adding your first book!</p>
                <button class="add-btn" onclick="toggleAddBookForm()">+ Add New Book</button>
            </div>
        `;
        return;
    }
    
    container.innerHTML = books.map(book => createBookCard(book)).join('');
}

// Create Book Card HTML
function createBookCard(book) {
    const status = book.status === 0 ? 'available' : 'issued';
    const statusText = status === 'available' ? 'Available' : 'Issued';
    
    return `
        <div class="book-card" onclick="viewBookDetails('${book.id}')">
            <div class="book-card-header">
                <div class="book-title">${escapeHtml(book.title)}</div>
                <div class="book-author">by ${escapeHtml(book.author)}</div>
            </div>
            <div class="book-card-body">
                <div class="book-meta">
                    ${book.isbn ? `
                        <div class="meta-item">
                            <span class="meta-label">ISBN</span>
                            <span class="meta-value">${escapeHtml(book.isbn)}</span>
                        </div>
                    ` : ''}
                    ${book.category ? `
                        <div class="meta-item">
                            <span class="meta-label">Category</span>
                            <span class="meta-value">${escapeHtml(book.category)}</span>
                        </div>
                    ` : ''}
                    ${book.publishedYear ? `
                        <div class="meta-item">
                            <span class="meta-label">Published</span>
                            <span class="meta-value">${book.publishedYear}</span>
                        </div>
                    ` : ''}
                    <div class="meta-item">
                        <span class="meta-label">Quantity</span>
                        <span class="meta-value">${book.quantity}</span>
                    </div>
                    <div class="meta-item">
                        <span class="meta-label">Status</span>
                        <span class="status-badge status-${status}">${statusText}</span>
                    </div>
                </div>
            </div>
            <div class="book-card-footer">
                <button class="btn-view" onclick="previewBook('${book.id}'); event.stopPropagation();">Preview</button>
                <button class="btn-delete" onclick="downloadBook('${book.id}'); event.stopPropagation();">Download</button>
            </div>
        </div>
    `;
}

// View Book Details Modal (now acts as preview)
async function viewBookDetails(bookId) {
    // delegate to preview behaviour
    await previewBook(bookId);
}

// previewBook implementation reused from dev version
async function previewBook(id) {
    try {
        const fileResp = await fetch(`/api/books/${id}/preview`, { credentials: 'include' });
        if (!fileResp.ok) {
            showToast('Could not load book file', 'error');
            return;
        }
        const blob = await fileResp.blob();
        const url = URL.createObjectURL(blob);
        // show in some modal or new window; for simplicity open new tab
        window.open(url, '_blank');
    } catch (err) {
        console.error('Error previewing book:', err);
        showToast('Error loading preview', 'error');
    }
}

// direct download helper
async function downloadBook(id) {
    try {
        const resp = await fetch(`/api/books/${id}/download`, { credentials: 'include' });
        if (!resp.ok) {
            showToast('Could not download file', 'error');
            return;
        }
        const blob = await resp.blob();
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        const cd = resp.headers.get('content-disposition');
        if (cd) {
            const match = cd.match(/filename="?([^";]+)"?/);
            if (match) a.download = match[1];
        }
        document.body.appendChild(a);
        a.click();
        a.remove();
        URL.revokeObjectURL(url);
    } catch (err) {
        console.error('Error downloading book:', err);
        showToast('Error downloading file', 'error');
    }
}

// Close Modal
function closeBookModal() {
    document.getElementById('bookModal').classList.add('hidden');
}

// Search Books
async function searchBooks() {
    const searchTerm = document.getElementById('searchInput').value.trim();
    
    if (!searchTerm) {
        filteredBooks = [...allBooks];
        displayBooks(filteredBooks);
        return;
    }
    
    const results = await apiSearchBooks(searchTerm);
    filteredBooks = results || [];
    displayBooks(filteredBooks);
}

// Reset Search
function resetSearch() {
    document.getElementById('searchInput').value = '';
    filteredBooks = [...allBooks];
    displayBooks(filteredBooks);
}

// Load current user's books
async function loadMyBooks() {
    const token = localStorage.getItem('lm_token');
    if (!token) {
        openLoginModal();
        return;
    }

    const books = await fetchMyBooks();
    allBooks = books;
    filteredBooks = [...allBooks];
    displayBooks(filteredBooks);
    updateStats();
}

// Toggle Add Book Form
function toggleAddBookForm() {
    document.getElementById('addBookFormContainer').classList.toggle('hidden');
    if (!document.getElementById('addBookFormContainer').classList.contains('hidden')) {
        document.getElementById('addBookForm').reset();
    }
}

// Add Book
async function addBook(event) {
    event.preventDefault();
    
    const bookData = {
        title: document.getElementById('bookTitle').value,
        author: document.getElementById('bookAuthor').value,
        isbn: document.getElementById('bookISBN').value || null,
        category: document.getElementById('bookCategory').value || null,
        publishedYear: parseInt(document.getElementById('bookPublishedYear').value) || null,
        quantity: parseInt(document.getElementById('bookQuantity').value)
    };
    
    const result = await createBook(bookData);
    if (result) {
        showAlert('Book added successfully!', 'success');
        toggleAddBookForm();
        await loadAndDisplayBooks();
    }
}

// Confirm Delete
function confirmDelete(bookId) {
    if (confirm('Are you sure you want to delete this book?')) {
        deleteBookRecord(bookId);
    }
}

// Delete Book
async function deleteBookRecord(bookId) {
    const success = await deleteBook(bookId);
    if (success) {
        showAlert('Book deleted successfully!', 'success');
        await loadAndDisplayBooks();
    }
}

// Update Statistics
function updateStats() {
    const totalBooks = allBooks.length;
    const availableBooks = allBooks.filter(book => book.status === 0).length;
    
    document.getElementById('totalBooks').textContent = totalBooks;
    document.getElementById('availableBooks').textContent = availableBooks;
}

// Show Alert
function showAlert(message, type = 'error') {
    const alert = document.createElement('div');
    alert.className = `alert alert-${type}`;
    alert.textContent = message;
    
    const container = document.querySelector('.main-content');
    if (container.firstChild) {
        container.insertBefore(alert, container.firstChild);
    } else {
        container.appendChild(alert);
    }
    
    setTimeout(() => {
        alert.remove();
    }, 5000);
}

// Escape HTML
function escapeHtml(text) {
    if (!text) return '';
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, m => map[m]);
}

// Allow Enter key to search
document.addEventListener('DOMContentLoaded', () => {
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                searchBooks();
            }
        });
    }
});
