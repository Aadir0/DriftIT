mergeInto(LibraryManager.library, {
    UnlockWebAudioContext: function () {
        try {
            if (typeof WEBAudio !== 'undefined' && WEBAudio.audioContext) {
                if (WEBAudio.audioContext.state === 'suspended') {
                    WEBAudio.audioContext.resume();
                }
            }
            
            var resumeAudio = function() {
                if (typeof WEBAudio !== 'undefined' && WEBAudio.audioContext) {
                    if (WEBAudio.audioContext.state === 'suspended') {
                        WEBAudio.audioContext.resume();
                    }
                }
                document.removeEventListener('touchstart', resumeAudio, true);
                document.removeEventListener('touchend', resumeAudio, true);
                document.removeEventListener('click', resumeAudio, true);
            };

            document.addEventListener('touchstart', resumeAudio, true);
            document.addEventListener('touchend', resumeAudio, true);
            document.addEventListener('click', resumeAudio, true);
        } catch (e) {
            console.warn('[DriftIT] WebAudio unlocker notice:', e);
        }
    }
});
