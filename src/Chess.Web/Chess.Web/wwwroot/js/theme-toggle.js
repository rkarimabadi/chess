// Theme toggle with localStorage persistence and system preference detection
window.themeToggle = {
    getStoredTheme: function () {
        return localStorage.getItem('theme');
    },
    getSystemTheme: function () {
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    },
    getEffectiveTheme: function () {
        var stored = this.getStoredTheme();
        if (stored) return stored;
        return this.getSystemTheme();
    },
    applyTheme: function (theme) {
        document.documentElement.setAttribute('data-theme', theme);
    },
    init: function () {
        this.applyTheme(this.getEffectiveTheme());
    },
    toggle: function () {
        var current = document.documentElement.getAttribute('data-theme') || 'light';
        var next = current === 'dark' ? 'light' : 'dark';
        localStorage.setItem('theme', next);
        this.applyTheme(next);
        return next;
    },
    setTheme: function (theme) {
        localStorage.setItem('theme', theme);
        this.applyTheme(theme);
    }
};
