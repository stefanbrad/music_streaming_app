// Offstream Core Client-Side Logic

window.AudioPlayer = {
    queue: [],
    currentIndex: -1,
    audio: null,
    
    init() {
        if (this.initialized) return;
        this.initialized = true;
        this.audio = document.getElementById('globalPlayer');
        if (!this.audio) return;
        
        // Bind audio events
        this.audio.addEventListener('timeupdate', () => this.onTimeUpdate());
        this.audio.addEventListener('durationchange', () => this.onDurationChange());
        this.audio.addEventListener('loadedmetadata', () => this.onDurationChange());
        this.audio.addEventListener('ended', () => this.onEnded());
        this.audio.addEventListener('play', () => this.onPlayStateChange(true));
        this.audio.addEventListener('pause', () => this.onPlayStateChange(false));
        
        // Bind UI controls
        const btnPlay = document.getElementById('playerBtnPlay');
        const progressBar = document.getElementById('playerProgressBar');
        const volumeSlider = document.getElementById('playerVolumeSlider');
        const btnMute = document.getElementById('playerBtnMute');
        const btnNext = document.getElementById('playerBtnNext');
        const btnPrev = document.getElementById('playerBtnPrev');
        
        if (btnPlay) btnPlay.addEventListener('click', () => this.togglePlay());
        
        if (progressBar) {
            progressBar.addEventListener('input', (e) => {
                if (this.audio.duration) {
                    const seekTime = (e.target.value / 100) * this.audio.duration;
                    this.audio.currentTime = seekTime;
                }
            });
        }
        
        if (volumeSlider) {
            volumeSlider.addEventListener('input', (e) => {
                this.audio.volume = e.target.value / 100;
                this.audio.muted = false;
                this.updateVolumeUI();
            });
        }
        
        if (btnMute) {
            btnMute.addEventListener('click', () => this.toggleMute());
        }
        
        if (btnNext) {
            btnNext.addEventListener('click', () => this.playNext());
        }
        
        if (btnPrev) {
            btnPrev.addEventListener('click', () => this.playPrev());
        }
        
        // Initialize volume
        if (volumeSlider) {
            this.audio.volume = volumeSlider.value / 100;
            this.updateVolumeUI();
        }
    },
    
    setQueue(tracks) {
        this.queue = tracks;
        this.currentIndex = -1;
    },
    
    play(trackId) {
        const index = this.queue.findIndex(t => t.id === trackId);
        if (index === -1) return;
        
        this.currentIndex = index;
        const track = this.queue[this.currentIndex];
        
        this.audio.src = track.url;
        this.audio.play();
        
        this.updateSongInfoUI(track);
        this.highlightPlayingTrack(trackId);
    },
    
    togglePlay() {
        if (!this.audio.src) return;
        if (this.audio.paused) {
            this.audio.play();
        } else {
            this.audio.pause();
        }
    },
    
    playNext() {
        if (this.queue.length === 0) return;
        let nextIndex = this.currentIndex + 1;
        if (nextIndex >= this.queue.length) {
            nextIndex = 0; // loop back to first track
        }
        this.play(this.queue[nextIndex].id);
    },
    
    playPrev() {
        if (this.queue.length === 0) return;
        let prevIndex = this.currentIndex - 1;
        if (prevIndex < 0) {
            prevIndex = this.queue.length - 1; // loop back to last track
        }
        this.play(this.queue[prevIndex].id);
    },
    
    toggleMute() {
        this.audio.muted = !this.audio.muted;
        this.updateVolumeUI();
    },
    
    updateVolumeUI() {
        const slider = document.getElementById('playerVolumeSlider');
        const icon = document.getElementById('muteIcon');
        if (slider) {
            slider.value = this.audio.muted ? 0 : this.audio.volume * 100;
        }
        if (icon) {
            if (this.audio.muted || this.audio.volume === 0) {
                icon.className = 'bi bi-volume-mute-fill text-danger';
            } else if (this.audio.volume < 0.5) {
                icon.className = 'bi bi-volume-down-fill text-white';
            } else {
                icon.className = 'bi bi-volume-up-fill text-white';
            }
        }
    },
    
    updateSongInfoUI(track) {
        const titleEl = document.getElementById('playerTrackTitle');
        const artistEl = document.getElementById('playerTrackArtist');
        const artEl = document.getElementById('playerArt');
        if (titleEl) titleEl.innerText = track.title;
        if (artistEl) artistEl.innerText = track.artist;
        if (artEl) {
            artEl.innerHTML = '<i class="bi bi-disc-fill text-white fs-5 music-disc-spin"></i>';
        }
    },
    
    highlightPlayingTrack(trackId) {
        // Remove active highlights
        document.querySelectorAll('.song-item').forEach(el => {
            el.classList.remove('active-playing');
            const pBtn = el.querySelector('.play-state-indicator');
            if (pBtn) pBtn.innerHTML = '<i class="bi bi-play-fill"></i>';
        });
        
        // Add highlight to currently playing track
        const playingItem = document.querySelector(`.song-item[data-song-id="${trackId}"]`);
        if (playingItem) {
            playingItem.classList.add('active-playing');
            const pBtn = playingItem.querySelector('.play-state-indicator');
            if (pBtn) pBtn.innerHTML = '<i class="bi bi-pause-fill text-white"></i>';
        }
    },
    
    onTimeUpdate() {
        const progress = document.getElementById('playerProgressBar');
        const currentText = document.getElementById('playerTimeCurrent');
        if (this.audio.duration) {
            const percent = (this.audio.currentTime / this.audio.duration) * 100;
            if (progress) progress.value = percent;
            if (currentText) currentText.innerText = this.formatTime(this.audio.currentTime);
        }
    },
    
    onDurationChange() {
        const totalText = document.getElementById('playerTimeTotal');
        if (totalText && this.audio.duration) {
            totalText.innerText = this.formatTime(this.audio.duration);
        }
    },
    
    onPlayStateChange(isPlaying) {
        const playIcon = document.getElementById('playIcon');
        const artEl = document.getElementById('playerArt');
        const discIcon = artEl ? artEl.querySelector('.bi-disc-fill') : null;
        
        if (playIcon) {
            playIcon.className = isPlaying ? 'bi bi-pause-fill' : 'bi bi-play-fill';
        }
        
        if (discIcon) {
            if (isPlaying) {
                discIcon.classList.add('music-disc-spin');
            } else {
                discIcon.classList.remove('music-disc-spin');
            }
        }
        
        // Sync item button icons on the list pages too
        if (this.currentIndex !== -1 && this.queue[this.currentIndex]) {
            const trackId = this.queue[this.currentIndex].id;
            const playingItem = document.querySelector(`.song-item[data-song-id="${trackId}"]`);
            if (playingItem) {
                const pBtn = playingItem.querySelector('.play-state-indicator');
                if (pBtn) {
                    pBtn.innerHTML = isPlaying ? '<i class="bi bi-pause-fill text-white"></i>' : '<i class="bi bi-play-fill text-white"></i>';
                }
            }
        }
    },
    
    onEnded() {
        this.playNext();
    },
    
    formatTime(seconds) {
        if (isNaN(seconds)) return "0:00";
        const m = Math.floor(seconds / 60);
        const s = Math.floor(seconds % 60).toString().padStart(2, '0');
        return `${m}:${s}`;
    }
};

// Rebind dynamic events (like search, dropzone, and custom events)
function bindDynamicEvents() {
    // Client-side search and dynamic filters
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        // Remove existing listener to avoid duplicates
        const newSearchInput = searchInput.cloneNode(true);
        searchInput.parentNode.replaceChild(newSearchInput, searchInput);
        
        newSearchInput.addEventListener('input', (e) => {
            const query = e.target.value.toLowerCase().trim();
            document.querySelectorAll('.song-item').forEach(item => {
                const title = item.querySelector('.song-title')?.innerText.toLowerCase() || '';
                const artist = item.querySelector('.song-artist')?.innerText.toLowerCase() || '';
                if (title.includes(query) || artist.includes(query)) {
                    item.style.setProperty('display', 'flex', 'important');
                } else {
                    item.style.setProperty('display', 'none', 'important');
                }
            });
            
            // For playlists on the index page
            document.querySelectorAll('.playlist-card-col').forEach(col => {
                const name = col.querySelector('.playlist-name-text')?.innerText.toLowerCase() || '';
                if (name.includes(query)) {
                    col.style.setProperty('display', 'block', 'important');
                } else {
                    col.style.setProperty('display', 'none', 'important');
                }
            });
        });
    }
}

// Update active states on navigation links
function updateNavbarActiveState(urlStr) {
    const url = new URL(urlStr, window.location.origin);
    const path = url.pathname.toLowerCase();
    
    document.querySelectorAll('.navbar-nav .nav-link').forEach(link => {
        const linkUrl = new URL(link.href, window.location.origin);
        const linkPath = linkUrl.pathname.toLowerCase();
        
        let isActive = false;
        if (linkPath === '/' && path === '/') {
            isActive = true;
        } else if (linkPath !== '/') {
            if (linkPath === '/library' && (path === '/library' || path === '/library/index')) {
                isActive = true;
            } else if (linkPath === '/library/upload' && path === '/library/upload') {
                isActive = true;
            } else if (linkPath.startsWith('/playlist') && path.startsWith('/playlist')) {
                isActive = true;
            }
        }
        
        if (isActive) {
            link.classList.add('active');
        } else {
            link.classList.remove('active');
        }
    });
}

// SPA Navigation function
async function navigateToPage(url, push = true) {
    try {
        document.body.classList.add('page-loading');
        
        const response = await fetch(url);
        if (!response.ok) {
            window.location.href = url;
            return;
        }
        
        const html = await response.text();
        
        // Modal and backdrop cleanup
        const modalBackdrops = document.querySelectorAll('.modal-backdrop');
        modalBackdrops.forEach(backdrop => backdrop.remove());
        document.body.classList.remove('modal-open');
        document.body.style.removeProperty('overflow');
        document.body.style.removeProperty('padding-right');
        
        // Parse the retrieved HTML
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');
        
        // Update title
        document.title = doc.title;
        
        // Update main content
        const newMain = doc.querySelector('main');
        const currentMain = document.querySelector('main');
        if (newMain && currentMain) {
            currentMain.innerHTML = newMain.innerHTML;
            
            // Execute page-specific inline scripts from the parsed HTML document
            executePageScripts(doc);
        }
        
        // Update navbar active state
        updateNavbarActiveState(url);
        
        // Rebind events
        bindDynamicEvents();
        
        // Update history state
        if (push) {
            history.pushState(null, '', url);
        }
        
        // Highlight active track
        if (window.AudioPlayer && window.AudioPlayer.currentIndex !== -1) {
            const track = window.AudioPlayer.queue[window.AudioPlayer.currentIndex];
            if (track) {
                window.AudioPlayer.highlightPlayingTrack(track.id);
            }
        }
    } catch (err) {
        console.error('AJAX navigation failed:', err);
        window.location.href = url;
    } finally {
        document.body.classList.remove('page-loading');
    }
}

// Helper to execute all page-specific scripts from the parsed HTML document
function executePageScripts(doc) {
    // Find all script tags in the entire parsed document
    doc.querySelectorAll('script').forEach(oldScript => {
        const src = oldScript.getAttribute('src');
        // Skip layout script files that are already running globally
        if (src && (src.includes('bootstrap') || src.includes('site.js'))) {
            return;
        }
        
        const newScript = document.createElement('script');
        // Copy all attributes
        Array.from(oldScript.attributes).forEach(attr => newScript.setAttribute(attr.name, attr.value));
        // Copy script content
        newScript.appendChild(document.createTextNode(oldScript.innerHTML));
        // Append to current document body to trigger execution, then clean up
        document.body.appendChild(newScript);
        newScript.parentNode.removeChild(newScript);
    });
}

// Intercept clicks on links
document.addEventListener('click', (e) => {
    const link = e.target.closest('a');
    if (!link) return;
    
    const href = link.getAttribute('href');
    if (!href || href.startsWith('#') || href.startsWith('javascript:') || link.getAttribute('target') === '_blank') return;
    
    // Check origin
    const url = new URL(link.href, window.location.origin);
    if (url.origin !== window.location.origin) return;
    
    // Skip external/account actions that require page reload
    if (href.includes('/Account/Logout') || href.includes('/Account/Login') || href.includes('/Account/Register')) {
        return;
    }
    
    e.preventDefault();
    navigateToPage(link.href);
});

// Intercept form submissions
document.addEventListener('submit', async (e) => {
    const form = e.target;
    if (form.getAttribute('method')?.toLowerCase() !== 'post') return;
    
    const action = form.getAttribute('action') || '';
    if (action.includes('/Account/Logout') || action.includes('/Account/Login') || action.includes('/Account/Register')) {
        return;
    }
    
    e.preventDefault();
    
    try {
        document.body.classList.add('page-loading');
        
        const formData = new FormData(form);
        const submitUrl = form.action || window.location.href;
        
        const response = await fetch(submitUrl, {
            method: 'POST',
            body: formData
        });
        
        if (!response.ok) {
            console.error('Form submission failed');
            return;
        }
        
        // Handle redirect
        const finalUrl = response.url;
        const html = await response.text();
        
        // Parse the retrieved HTML
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');
        
        // Modal cleanup
        const modalBackdrops = document.querySelectorAll('.modal-backdrop');
        modalBackdrops.forEach(backdrop => backdrop.remove());
        document.body.classList.remove('modal-open');
        document.body.style.removeProperty('overflow');
        document.body.style.removeProperty('padding-right');
        
        document.title = doc.title;
        
        const newMain = doc.querySelector('main');
        const currentMain = document.querySelector('main');
        if (newMain && currentMain) {
            currentMain.innerHTML = newMain.innerHTML;
            
            // Execute scripts
            executePageScripts(doc);
        }
        
        updateNavbarActiveState(finalUrl);
        bindDynamicEvents();
        
        history.pushState(null, '', finalUrl);
        
        if (window.AudioPlayer && window.AudioPlayer.currentIndex !== -1) {
            const track = window.AudioPlayer.queue[window.AudioPlayer.currentIndex];
            if (track) {
                window.AudioPlayer.highlightPlayingTrack(track.id);
            }
        }
    } catch (err) {
        console.error('Form submit error:', err);
    } finally {
        document.body.classList.remove('page-loading');
    }
});

// Popstate event
window.addEventListener('popstate', () => {
    navigateToPage(window.location.href, false);
});

// DOMContentLoaded global initialization
document.addEventListener('DOMContentLoaded', () => {
    window.AudioPlayer.init();
    updateNavbarActiveState(window.location.href);
    bindDynamicEvents();
});
