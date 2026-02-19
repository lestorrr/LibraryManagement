// Auth UI + flow
function openLoginModal() {
    document.getElementById('loginModal').classList.remove('hidden');
}
function closeLoginModal() {
    document.getElementById('loginModal').classList.add('hidden');
}
function openRegisterModal() {
    document.getElementById('registerModal').classList.remove('hidden');
}
function closeRegisterModal() {
    document.getElementById('registerModal').classList.add('hidden');
}

async function login(event) {
    event.preventDefault();
    const identifier = document.getElementById('loginIdentifier').value.trim();
    const password = document.getElementById('loginPassword').value;

    try {
        const res = await loginUser(identifier, password);
        if (res && res.token) {
            localStorage.setItem('lm_token', res.token);
            closeLoginModal();
            showAlert('Signed in successfully', 'success');
            updateAuthUI();
            await loadAndDisplayBooks();
        } else {
            throw new Error('Invalid login response');
        }
    } catch (err) {
        showAlert(err.message || 'Login failed', 'error');
    }
}

async function register(event) {
    event.preventDefault();
    const username = document.getElementById('regUsername').value.trim();
    const email = document.getElementById('regEmail').value.trim();
    const password = document.getElementById('regPassword').value;
    const confirm = document.getElementById('regConfirm').value;

    if (password !== confirm) {
        showAlert('Passwords do not match', 'error');
        return;
    }

    try {
        const res = await registerUser(username, email, password, confirm);
        if (res && res.success !== false) {
            showAlert('Account created — please sign in', 'success');
            closeRegisterModal();
        } else {
            showAlert(res.message || 'Registration failed', 'error');
        }
    } catch (err) {
        showAlert(err.message || 'Registration failed', 'error');
    }
}

function signOut() {
    localStorage.removeItem('lm_token');
    updateAuthUI();
    showAlert('Signed out', 'success');
}

function updateAuthUI() {
    const token = localStorage.getItem('lm_token');
    const signedIn = !!token;
    document.getElementById('btnSignIn').classList.toggle('hidden', signedIn);
    document.getElementById('btnSignUp').classList.toggle('hidden', signedIn);
    document.getElementById('btnSignOut').classList.toggle('hidden', !signedIn);
    document.getElementById('addBookButton').disabled = !signedIn;
    const myBtn = document.getElementById('myBooksBtn');
    if (myBtn) myBtn.classList.toggle('hidden', !signedIn);
}

// Called when user clicks "Add New Book" button
function onAddBookClick() {
    const token = localStorage.getItem('lm_token');
    if (!token) {
        openLoginModal();
        return;
    }
    toggleAddBookForm();
}

// initialize auth UI
document.addEventListener('DOMContentLoaded', () => {
    updateAuthUI();
});