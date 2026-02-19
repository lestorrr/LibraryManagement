// API Configuration
const API_BASE_URL = window.location.origin + '/api';

function getAuthHeaders() {
    const token = localStorage.getItem('lm_token');
    const headers = { 'Content-Type': 'application/json' };
    if (token) headers['Authorization'] = `Bearer ${token}`;
    return headers;
}

// API Functions
async function fetchAllBooks() {
    try {
        const response = await fetch(`${API_BASE_URL}/books`, {
            method: 'GET',
            headers: { 'Content-Type': 'application/json' }
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error('Error fetching books:', error);
        showAlert('Failed to load books', 'error');
        return [];
    }
}

async function fetchBookById(id) {
    try {
        const response = await fetch(`${API_BASE_URL}/books/${id}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error('Error fetching book:', error);
        showAlert('Failed to load book details', 'error');
        return null;
    }
}

async function createBook(bookData) {
    try {
        const response = await fetch(`${API_BASE_URL}/books`, {
            method: 'POST',
            headers: getAuthHeaders(),
            body: JSON.stringify(bookData)
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error('Error creating book:', error);
        showAlert(`Failed to create book: ${error.message}`, 'error');
        return null;
    }
}

async function apiSearchBooks(searchTerm) {
    try {
        const response = await fetch(`${API_BASE_URL}/books/search?term=${encodeURIComponent(searchTerm)}`, {
            method: 'GET',
            headers: { 'Content-Type': 'application/json' }
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error('Error searching books:', error);
        showAlert('Failed to search books', 'error');
        return [];
    }
}

async function deleteBook(id) {
    try {
        const response = await fetch(`${API_BASE_URL}/books/${id}`, {
            method: 'DELETE',
            headers: getAuthHeaders()
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        return true;
    } catch (error) {
        console.error('Error deleting book:', error);
        showAlert('Failed to delete book', 'error');
        return false;
    }
}

async function loginUser(identifier, password) {
    try {
        const response = await fetch(`${API_BASE_URL}/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ usernameOrEmail: identifier, password })
        });

        if (!response.ok) {
            const err = await response.json().catch(() => ({}));
            throw new Error(err.message || `HTTP error! status: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error('Login error:', error);
        throw error;
    }
}

async function registerUser(username, email, password, confirmPassword, firstName = '', lastName = '') {
    try {
        const response = await fetch(`${API_BASE_URL}/auth/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, email, password, confirmPassword, firstName, lastName })
        });

        if (!response.ok) {
            const err = await response.json().catch(() => ({}));
            throw new Error(err.message || `HTTP error! status: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error('Register error:', error);
        throw error;
    }
}

async function fetchMyBooks() {
    try {
        const response = await fetch(`${API_BASE_URL}/books/mine`, {
            method: 'GET',
            headers: getAuthHeaders()
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error('Error fetching my books:', error);
        showAlert('Failed to load your books', 'error');
        return [];
    }
}
