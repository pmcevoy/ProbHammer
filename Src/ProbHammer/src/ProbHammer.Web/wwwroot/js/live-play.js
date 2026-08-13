document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.weapon-name-toggle').forEach(button => {
        button.addEventListener('click', () => {
            const weaponId = button.dataset.weaponId;
            const expanded = button.getAttribute('aria-expanded') === 'true';

            document.querySelectorAll(`tr.weapon-contribution-row[data-weapon-id="${weaponId}"]`)
                .forEach(row => { row.hidden = expanded; });

            button.setAttribute('aria-expanded', String(!expanded));
        });
    });
});
