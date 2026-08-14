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

    document.querySelectorAll('.unit-block').forEach(initUnitSelection);
});

// One independent selection state per unit block: a Set of currently-deselected select-keys
// (see LivePlayModel.SelectKey - "{Component}::{Statline}" for a single-loadout statline,
// "{Component}::{Statline}::{LoadoutIndex}" for a loadout under a multi-loadout statline). Empty
// Set = everything selected = today's pre-filtering default rendering, unchanged.
function initUnitSelection(unitEl) {
    const deselected = new Set();
    const entries = [...unitEl.querySelectorAll('.statline-entry')];
    const clearBtn = unitEl.querySelector('.clear-filter-btn');

    // Groups entries by their enclosing run's data-run-index (set on col-statline and its per-row
    // ability cells in LivePlay.cshtml), so a run's collapse state - "every entry in this run is
    // fully deselected" - can be derived without re-deriving run membership from grid-row styling.
    const entriesByRun = new Map();
    entries.forEach(entryEl => {
        const runCell = entryEl.closest('[data-run-index]');
        if (!runCell) return;
        const runIndex = runCell.dataset.runIndex;
        if (!entriesByRun.has(runIndex)) entriesByRun.set(runIndex, []);
        entriesByRun.get(runIndex).push(entryEl);
    });

    entries.forEach(entryEl => {
        const singleToggle = entryEl.querySelector(':scope > .statline-toggle');
        const groupHeader = entryEl.querySelector(':scope > .statline-toggle-group');
        const loadouts = [...entryEl.querySelectorAll(':scope > .loadout-breakdown > .loadout-toggle')];

        if (singleToggle) {
            singleToggle.addEventListener('click', () => {
                toggleKey(singleToggle.dataset.selectKey);
                refresh();
            });
        }

        if (groupHeader) {
            // Activating the group header selects all loadouts unless every one is already
            // selected, in which case it deselects all of them - see the Statline Section
            // Rendering requirement's tri-state scenarios.
            groupHeader.addEventListener('click', () => {
                const allSelected = loadouts.every(li => !deselected.has(li.dataset.selectKey));
                loadouts.forEach(li => {
                    if (allSelected) deselected.add(li.dataset.selectKey);
                    else deselected.delete(li.dataset.selectKey);
                });
                refresh();
            });

            loadouts.forEach(li => {
                li.addEventListener('click', () => {
                    toggleKey(li.dataset.selectKey);
                    refresh();
                });
            });
        }
    });

    if (clearBtn) {
        // Lives inside the Statline <summary> now (closer to what it clears) - without
        // preventDefault, clicking it would also trigger the browser's default action for a click
        // inside <summary>: toggling the parent <details> open/closed.
        clearBtn.addEventListener('click', event => {
            event.preventDefault();
            deselected.clear();
            refresh();
        });
    }

    function toggleKey(key) {
        if (!key) return;
        if (deselected.has(key)) deselected.delete(key);
        else deselected.add(key);
    }

    function refresh() {
        updateIndicators();
        updateRunCollapse();
        recomputeWeaponSections();
        if (clearBtn) clearBtn.hidden = deselected.size === 0;
    }

    // Single definition of an entry's own selected/deselected/partial state, shared by
    // updateIndicators (which renders it) and isEntryFullyDeselected (which run-collapse uses to
    // decide whether every entry in a run has gone quiet).
    function entryState(entryEl) {
        const singleToggle = entryEl.querySelector(':scope > .statline-toggle');
        if (singleToggle) {
            return { toggle: singleToggle, loadouts: [], state: deselected.has(singleToggle.dataset.selectKey) ? 'deselected' : 'selected' };
        }

        const groupHeader = entryEl.querySelector(':scope > .statline-toggle-group');
        const loadouts = [...entryEl.querySelectorAll(':scope > .loadout-breakdown > .loadout-toggle')];
        const selectedCount = loadouts.filter(li => !deselected.has(li.dataset.selectKey)).length;
        const state = selectedCount === 0 ? 'deselected' : selectedCount === loadouts.length ? 'selected' : 'partial';
        return { toggle: groupHeader, loadouts, state };
    }

    function isEntryFullyDeselected(entryEl) {
        return entryState(entryEl).state === 'deselected';
    }

    function updateIndicators() {
        entries.forEach(entryEl => {
            const { toggle, loadouts, state } = entryState(entryEl);
            if (!toggle) return;
            loadouts.forEach(li => setState(li, deselected.has(li.dataset.selectKey) ? 'deselected' : 'selected'));
            setState(toggle, state);
        });
    }

    function setState(el, state) {
        el.classList.remove('select-selected', 'select-deselected', 'select-partial');
        el.classList.add(`select-${state}`);
    }

    // A run collapses to header-only once every entry sharing it (its col-statline cell and any
    // per-row ability cells at the same data-run-index) is fully deselected; a spans-component
    // ability cell collapses only once every run in its [data-first-run-index, data-last-run-index]
    // range is collapsed. Grid-row indices themselves are never touched - only the run-collapsed
    // class, which CSS uses to shrink cell content.
    function updateRunCollapse() {
        const runCollapsed = new Map();
        entriesByRun.forEach((entryEls, runIndex) => {
            runCollapsed.set(runIndex, entryEls.every(isEntryFullyDeselected));
        });

        unitEl.querySelectorAll('.statline-cell[data-run-index]').forEach(cell => {
            cell.classList.toggle('run-collapsed', runCollapsed.get(cell.dataset.runIndex) === true);
        });

        unitEl.querySelectorAll('.statline-cell[data-first-run-index]').forEach(cell => {
            const first = Number(cell.dataset.firstRunIndex);
            const last = Number(cell.dataset.lastRunIndex);
            let allCollapsed = true;
            for (let i = first; i <= last; i++) {
                if (runCollapsed.get(String(i)) !== true) {
                    allCollapsed = false;
                    break;
                }
            }
            cell.classList.toggle('run-collapsed', allCollapsed);
        });
    }

    function recomputeWeaponSections() {
        ['ranged', 'melee'].forEach(kind => {
            const section = unitEl.querySelector(`[data-section="${kind}"]`);
            if (!section) return;

            // :not(.weapon-contribution-row) matters here: breakdown rows carry data-weapon-id too
            // (needed to correlate them to their primary row for the expand/collapse toggle), so an
            // unqualified selector would also match them as if they were primary rows - each such
            // false match's own trailing `row.classList.toggle('selection-excluded', !anySelected)`
            // would then re-show it based on the whole weapon's selection state, undoing the correct
            // per-contribution hide that recomputeWeaponRow's group-processing step just applied.
            let sectionChanged = false;
            section.querySelectorAll('tbody > tr[data-weapon-id]:not(.weapon-contribution-row)').forEach(row => {
                if (recomputeWeaponRow(row)) sectionChanged = true;
            });

            const badge = section.querySelector(':scope > summary .filtered-badge');
            if (badge) badge.hidden = !sectionChanged;
        });
    }

    // Returns true if this weapon row's rendering (visibility or total) currently differs from its
    // unfiltered default. Groups its breakdown rows by GroupKey: a group with a merged row shows
    // that merged row only while every one of its raw siblings is selected (matching the default,
    // pre-filtering rendering exactly); once any sibling diverges, the merged row hides and only
    // the still-selected raw row(s) show instead - a deselected sibling's row simply never shows,
    // never struck through. A group with no merged alternative (a lone contribution, or contributions
    // that already disagreed on PerModelAttacks before any filtering) shows exactly its selected raw
    // rows the same way. The primary row hides entirely once no contribution anywhere is selected.
    function recomputeWeaponRow(row) {
        const weaponId = row.dataset.weaponId;
        const breakdownRows = [...unitEl.querySelectorAll(`tr.weapon-contribution-row[data-weapon-id="${weaponId}"]`)];
        if (breakdownRows.length === 0) return false;

        const groups = new Map();
        breakdownRows.forEach(r => {
            const key = r.dataset.groupKey;
            if (!groups.has(key)) groups.set(key, []);
            groups.get(key).push(r);
        });

        let sum = 0;
        let anySelected = false;
        let numericOk = true;

        groups.forEach(rowsInGroup => {
            const merged = rowsInGroup.find(r => r.dataset.merged === 'true');
            const raw = rowsInGroup.filter(r => r.dataset.merged !== 'true');
            const selectedRaw = raw.filter(r => !deselected.has(r.dataset.selectKey));
            const allSelected = raw.length > 0 && selectedRaw.length === raw.length;

            if (merged) {
                merged.classList.toggle('selection-excluded', !allSelected);
                raw.forEach(r => r.classList.toggle('selection-excluded', allSelected || !selectedRaw.includes(r)));
            } else {
                raw.forEach(r => r.classList.toggle('selection-excluded', !selectedRaw.includes(r)));
            }

            if (selectedRaw.length > 0) anySelected = true;
            selectedRaw.forEach(r => {
                const value = parseInt(r.dataset.subtotalValue, 10);
                if (Number.isNaN(value)) numericOk = false;
                else sum += value;
            });
        });

        row.classList.toggle('selection-excluded', !anySelected);

        const cell = row.querySelector('.weapon-attacks-value');
        if (cell) {
            if (row.dataset.originalTotal === undefined) row.dataset.originalTotal = cell.textContent;
            if (anySelected) cell.textContent = numericOk ? String(sum) : row.dataset.originalTotal;
        }

        return !anySelected || (numericOk && cell && String(sum) !== row.dataset.originalTotal);
    }

    refresh();
}
