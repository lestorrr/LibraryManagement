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
        if (res && res.success) {
            showAlert('Signed in successfully', 'success');
            updateAuthUI(res.user);
            await loadAndDisplayBooks();
            // if on standalone login page, redirect to home
            if (window.location.pathname.endsWith('login.html')) {
                window.location.href = 'index.html';
            } else {
                closeLoginModal();
            }
        } else {
            throw new Error(res.message || 'Invalid login response');
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
    fetch(`${API_BASE_URL}/auth/logout`, {
        method: 'POST',
        credentials: 'include'
    }).then(() => {
        sessionStorage.removeItem('currentUser');
        updateAuthUI();
        showAlert('Signed out', 'success');
        window.location.href = 'index.html';
    });
}

function updateAuthUI(user = null) {
    if (!user) {
        user = JSON.parse(sessionStorage.getItem('currentUser') || 'null');
    } else {
        sessionStorage.setItem('currentUser', JSON.stringify(user));
    }
    
    const signedIn = !!user;
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
        const username = user.username || 'User';
        userDisplay.textContent = `Signed in as ${username}`;
        userDisplay.classList.remove('hidden');
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
    const user = JSON.parse(sessionStorage.getItem('currentUser') || 'null');
    if (!user) {
        openLoginModal();
        return;
    }
    toggleAddBookForm();
}

// initialize auth UI
document.addEventListener('DOMContentLoaded', () => {
    updateAuthUI();
});