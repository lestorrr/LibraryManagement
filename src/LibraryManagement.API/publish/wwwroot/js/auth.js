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
            showAlert('Signed in successfully', 'success');
            updateAuthUI();
            await loadAndDisplayBooks();
            // if on standalone login page, redirect to home
            if (window.location.pathname.endsWith('login.html')) {
                window.location.href = 'index.html';
            } else {
                closeLoginModal();
            }
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
    const firstName = (document.getElementById('regFirst') && document.getElementById('regFirst').value.trim()) || '';
    const lastName = (document.getElementById('regLast') && document.getElementById('regLast').value.trim()) || '';

    if (password !== confirm) {
        showAlert('Passwords do not match', 'error');
        return;
    }

    try {
        const res = await registerUser(username, email, password, confirm, firstName, lastName);
        if (res && res.success !== false) {
            showAlert('Account created — please sign in', 'success');
            // if on standalone register page, navigate to login
            if (window.location.pathname.endsWith('register.html')) {
                window.location.href = 'login.html';
            } else {
                closeRegisterModal();
            }
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
    const btnSignIn = document.getElementById('btnSignIn');
    const btnSignUp = document.getElementById('btnSignUp');
    const btnSignOut = document.getElementById('btnSignOut');
    const addBookButton = document.getElementById('addBookButton');
    const myBtn = document.getElementById('myBooksBtn');
    const userDisplay = document.getElementById('userDisplay');

    if (btnSignIn) btnSignIn.classList.toggle('hidden', signedIn);
    if (btnSignUp) btnSignUp.classList.toggle('hidden', signedIn);
    if (btnSignOut) btnSignOut.classList.toggle('hidden', !signedIn);
    if (addBookButton) addBookButton.disabled = !signedIn;
    if (myBtn) myBtn.classList.toggle('hidden', !signedIn);

    if (signedIn && userDisplay) {
        try {
            const payload = parseJwt(token);
            const username = payload && (payload.username || payload.sub || payload.unique_name) ? (payload.username || payload.sub || payload.unique_name) : null;
            userDisplay.textContent = username ? `Signed in as ${username}` : 'Signed in';
            userDisplay.classList.remove('hidden');
        } catch (e) {
            userDisplay.textContent = 'Signed in';
            userDisplay.classList.remove('hidden');
        }
    } else if (userDisplay) {
        userDisplay.textContent = '';
        userDisplay.classList.add('hidden');
    }
}

function parseJwt(token) {
    if (!token) return null;
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    try {
        const payload = parts[1];
        const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
        return JSON.parse(decodeURIComponent(escape(json)));
    } catch (e) {
        return null;
    }
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