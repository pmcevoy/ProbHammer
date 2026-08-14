const CASUALTY_STORAGE_KEY = 'probhammer.livePlay.casualties';

// Per-unit selection (deselected select-keys) state, keyed by data-unit-index and kept OUTSIDE
// initUnitSelection's own closure so it survives a casualty-triggered swapUnitBlock - a casualty
// adjustment on one entry (e.g. Crusade Ancient) must not reset an unrelated entry's (e.g. Sword
// Brother, Initiate, Neophyte) filter in the same unit block. Select-keys themselves
// (ComponentName/StatlineName/LoadoutIndex) never change from a casualty adjustment, only
// RemainingCount does, so a persisted Set always still matches the swapped-in markup correctly.
// A real page reload/navigation still starts every unit fully selected (this Map is reinitialized
// empty on each page load), matching the existing "selection state does not persist across a page
// reload" requirement - only in-session swaps of the same load now preserve it.
const deselectedByUnit = new Map();

document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.unit-block').forEach(initUnitBlock);
    void syncCasualties();
});

// One consolidated per-unit-block init, covering everything that attaches listeners to a
// unit-block's own DOM subtree - re-run on a swapped-in unit block after a casualty adjustment
// (see swapUnitBlock) so neither piece is left with dead listeners. Combines what used to be two
// independent DOMContentLoaded passes (a page-wide .weapon-name-toggle query, and a per-unit-block
// initUnitSelection call) plus this change's own casualty controls.
function initUnitBlock(unitEl) {
    initWeaponProvenanceToggles(unitEl);
    initCasualtyControls(unitEl);
    initUnitSelection(unitEl);
}

// Provenance breakdown expand/collapse (add-live-play-weapon-provenance). Scoped to unitEl (was a
// page-wide query) so re-running this after a casualty swap only rewires the one affected unit.
function initWeaponProvenanceToggles(unitEl) {
    unitEl.querySelectorAll('.weapon-name-toggle').forEach(button => {
        button.addEventListener('click', () => {
            const weaponId = button.dataset.weaponId;
            const expanded = button.getAttribute('aria-expanded') === 'true';

            unitEl.querySelectorAll(`tr.weapon-contribution-row[data-weapon-id="${weaponId}"]`)
                .forEach(row => { row.hidden = expanded; });

            button.setAttribute('aria-expanded', String(!expanded));
        });
    });
}

// Casualty marking controls (casualty-tracking). Each button carries its own coordinate
// (data-casualty-key) and current remaining/initial counts as data-* attributes - stopPropagation
// first, since these buttons sit inside their row's own selection-toggle click target.
function initCasualtyControls(unitEl) {
    unitEl.querySelectorAll('.casualty-btn').forEach(button => {
        button.addEventListener('click', event => {
            event.stopPropagation();
            if (button.disabled) return;

            const key = button.dataset.casualtyKey;
            const remaining = Number(button.dataset.remaining);
            const initial = Number(button.dataset.initial);
            const isIncrease = button.classList.contains('casualty-inc');
            const newValue = isIncrease ? Math.min(initial, remaining + 1) : Math.max(0, remaining - 1);

            const stored = getStoredCasualties();
            stored[key] = newValue;
            setStoredCasualties(stored);

            void syncCasualties();
        });
    });
}

function getStoredCasualties() {
    try {
        return JSON.parse(localStorage.getItem(CASUALTY_STORAGE_KEY) || '{}');
    } catch {
        return {};
    }
}

function setStoredCasualties(map) {
    localStorage.setItem(CASUALTY_STORAGE_KEY, JSON.stringify(map));
}

// Reverses LivePlayModel.CasualtyKey's "{unitIndex}::{componentName}::{statlineName}[::{loadoutIndex}]"
// format. Returns null for a malformed key rather than throwing - defensive against a stale/
// hand-edited localStorage payload.
function parseCasualtyKey(key) {
    const parts = key.split('::');
    if (parts.length !== 3 && parts.length !== 4) return null;

    const unitIndex = Number(parts[0]);
    if (Number.isNaN(unitIndex)) return null;

    const loadoutIndex = parts.length === 4 ? Number(parts[3]) : -1;
    return {
        unitIndex,
        componentName: parts[1],
        statlineName: parts[2],
        loadoutIndex: Number.isNaN(loadoutIndex) ? -1 : loadoutIndex
    };
}

// Posts the *entire* current localStorage map on every call, never just the newest change - the
// server is stateless and always rebuilds from a pristine roster, so a request carrying only one
// adjustment would discard every earlier casualty from the same session (see casualty-tracking's
// design.md - "every request carries the full current map"). A no-op when the map is empty, so a
// browser with no recorded adjustments never issues a request at all.
async function syncCasualties() {
    const stored = getStoredCasualties();
    const adjustments = Object.entries(stored)
        .map(([key, remainingCount]) => {
            const coordinate = parseCasualtyKey(key);
            return coordinate ? { coordinate, remainingCount } : null;
        })
        .filter(entry => entry !== null);

    if (adjustments.length === 0) return;

    let response;
    try {
        response = await fetch('/api/live-play/casualties', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(adjustments)
        });
    } catch {
        return; // offline/network failure - leave the DOM as-is, localStorage still holds the state
    }
    if (!response.ok) return;

    const fragments = await response.json();
    Object.entries(fragments).forEach(([unitIndex, html]) => swapUnitBlock(unitIndex, html));
}

// Replaces one unit-block's markup with server-rendered HTML and re-initializes only that node -
// every other unit-block's live listeners/selection state are untouched, and this unit's own
// selection state (deselectedByUnit) survives the swap too - see initUnitSelection. Carries forward
// which <details> sections (Statline/Ranged/Melee - each independently collapsible, closed by
// default in fresh server-rendered markup) were open on the old node, so tapping a casualty control
// doesn't visually snap an expanded section shut - caught via hands-on browser testing, not from
// the spec discussion; a full markup swap has no other way to know a section was manually opened.
function swapUnitBlock(unitIndex, html) {
    const oldEl = document.querySelector(`.unit-block[data-unit-index="${unitIndex}"]`);
    if (!oldEl) return;

    const openSections = new Set(
        [...oldEl.querySelectorAll('details.lp-section[open]')].map(details => details.dataset.section)
    );

    const template = document.createElement('template');
    template.innerHTML = html.trim();
    const newEl = template.content.firstElementChild;
    if (!newEl) return;

    newEl.querySelectorAll('details.lp-section').forEach(details => {
        if (openSections.has(details.dataset.section)) details.open = true;
    });

    oldEl.replaceWith(newEl);
    initUnitBlock(newEl);
}

// One independent selection state per unit block: a Set of currently-deselected select-keys
// (see LivePlayModel.SelectKey - "{Component}::{Statline}" for a single-loadout statline,
// "{Component}::{Statline}::{LoadoutIndex}" for a loadout under a multi-loadout statline). Empty
// Set = everything selected = today's pre-filtering default rendering, unchanged. The Set itself
// lives in deselectedByUnit (module scope), keyed by this unit's data-unit-index, so it survives
// a casualty-triggered re-init of this same unit block (see swapUnitBlock) instead of resetting.
function initUnitSelection(unitEl) {
    const unitIndex = unitEl.dataset.unitIndex;
    const deselected = deselectedByUnit.get(unitIndex) ?? new Set();
    deselectedByUnit.set(unitIndex, deselected);

    const entries = [...unitEl.querySelectorAll('.statline-entry')];
    const clearBtn = unitEl.querySelector('.clear-filter-btn');

    // Groups entries by their enclosing run's data-run-index (set on col-statline and its per-row
    // ability cells in _UnitBlock.cshtml), so a run's collapse state - "every entry in this run is
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
    // decide whether every entry in a run has gone quiet). Selection state alone - a fully-dead
    // entry (see updateRunCollapse) can still be independently selected/deselected/partial, per
    // "Casualty Tracking Is Independent of Selection Filtering".
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

    // A run collapses to header-only once every entry sharing it meets either of two independent
    // conditions: it's fully deselected (this Set, ephemeral/client-side), or it's fully dead -
    // read from data-dead, rendered server-side from RemainingCount (casualty-tracking) - so a
    // dead run stays collapsed regardless of reselection, and a reload shows it already collapsed
    // without needing this function to have run first. The two are combined per-run (OR), never
    // merged into the deselected Set itself, so neither can accidentally suppress the other; a
    // spans-component ability cell collapses only once every run in its
    // [data-first-run-index, data-last-run-index] range is collapsed by that same combined rule.
    function updateRunCollapse() {
        const runDeselected = new Map();
        entriesByRun.forEach((entryEls, runIndex) => {
            runDeselected.set(runIndex, entryEls.every(isEntryFullyDeselected));
        });

        const runCollapsed = new Map();
        unitEl.querySelectorAll('.statline-cell.col-statline[data-run-index]').forEach(cell => {
            const runIndex = cell.dataset.runIndex;
            const dead = cell.dataset.dead === 'true';
            runCollapsed.set(runIndex, dead || runDeselected.get(runIndex) === true);
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
    // A fully-dead loadout/model-line never has a contribution row to begin with (excluded
    // server-side - see the Aggregate Weapon Count View requirement), so casualty state needs no
    // special-casing here at all.
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
