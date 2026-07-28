function showToast(message, variant = 'danger') {
    const toast = document.getElementById('toast');
    toast.className = `toast align-items-center text-bg-${variant} border-0`;
    document.getElementById('toastBody').textContent = message;
    bootstrap.Toast.getOrCreateInstance(toast).show();
}

document.body.addEventListener('htmx:responseError', () => showToast('Something went wrong.'));

document.body.addEventListener('htmx:afterSwap', function (evt) {
    const select = evt.target.querySelector ? evt.target.querySelector('#HuntLocation') : null;
    if (!select || select.tomselect) return;

    new TomSelect(select, {
        valueField: 'value',
        labelField: 'text',
        searchField: 'text',
        create: false,
        maxItems: 1,
        load: function (query, callback) {
            if (!query.length) return callback();
            fetch(`/Home/LocationSuggestions?query=${encodeURIComponent(query)}`)
                .then(r => r.json())
                .then(data => callback(data))
                .catch(() => callback());
        }
    });
});

document.body.addEventListener('htmx:beforeRequest', function (evt) {
    if (evt.target.id !== 'createHuntForm') return;
    const select = document.getElementById('HuntLocation');
    if (!select || !select.value) {
        evt.preventDefault();
        showToast('Please select a location from the suggestions list.', 'warning');
    }
});