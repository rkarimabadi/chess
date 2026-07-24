window.audio = {
    _muted: false,

    playSound: function (url) {
        if (this._muted) return;
        try {
            var audio = new Audio(url);
            audio.play().catch(function () { });
        } catch (e) { }
    },

    stopAllSounds: function () {
        // No-op for now; future: track and stop active Audio elements
    },

    setMuted: function (muted) {
        this._muted = muted;
    }
};
