(() => {
      const panels = Array.from(document.querySelectorAll('[data-table-panel]'));
      const navLinks = Array.from(document.querySelectorAll('.report-nav a'));

      function parseComparable(value, type) {
        const raw = String(value ?? '').trim();
        if (type !== 'number') {
          return raw.toLowerCase();
        }

        if (!raw) {
          return null;
        }

        const parsed = Number(raw);
        return Number.isNaN(parsed) ? null : parsed;
      }

      function compareValues(left, right, type, direction) {
        const leftValue = parseComparable(left, type);
        const rightValue = parseComparable(right, type);

        if (leftValue === null && rightValue === null) {
          return 0;
        }

        if (leftValue === null) {
          return 1;
        }

        if (rightValue === null) {
          return -1;
        }

        if (type === 'number') {
          return direction === 'asc' ? leftValue - rightValue : rightValue - leftValue;
        }

        return direction === 'asc'
          ? leftValue.localeCompare(rightValue)
          : rightValue.localeCompare(leftValue);
      }

      for (const panel of panels) {
        const table = panel.querySelector('table');
        const tbody = table.querySelector('tbody');
        const rows = Array.from(tbody.querySelectorAll('tr')).filter(row => !row.classList.contains('empty') && !row.classList.contains('detail-row'));
        const search = panel.querySelector('[data-table-search]');
        const reset = panel.querySelector('[data-table-reset]');
        const sortButtons = Array.from(panel.querySelectorAll('[data-sort-column]'));
        const filters = Array.from(panel.querySelectorAll('.filter-input'));
        const multiSelects = Array.from(panel.querySelectorAll('[data-multi-select]'));
        const sortState = {
          column: table.dataset.defaultSortColumn === '' ? null : Number(table.dataset.defaultSortColumn),
          direction: table.dataset.defaultSortDirection || 'asc'
        };

        function applyFilters() {
          const globalTerm = (search?.value || '').trim().toLowerCase();

          for (const row of rows) {
            const cells = Array.from(row.children);
            const globalMatch = !globalTerm || cells.some(cell => (cell.dataset.filter || cell.textContent || '').toLowerCase().includes(globalTerm));
            const inputsMatch = filters.every(filter => {
              const term = filter.value.trim().toLowerCase();
              if (!term) {
                return true;
              }

              const cell = cells[Number(filter.dataset.filterColumn)];
              const value = (cell?.dataset.filter || cell?.textContent || '').trim().toLowerCase();
              if (filter.dataset.filterOperator === 'min') {
                return Boolean(value) && value >= term;
              }

              if (filter.dataset.filterOperator === 'max') {
                return Boolean(value) && value <= term;
              }

              return value.includes(term);
            });
            const multiSelectsMatch = multiSelects.every(multiSelect => {
              const selected = Array.from(multiSelect.querySelectorAll('input:checked'))
                .map(input => input.value.toLowerCase());
              if (selected.length === 0) {
                return true;
              }

              const cell = cells[Number(multiSelect.dataset.filterColumn)];
              const value = (cell?.dataset.filter || cell?.textContent || '').trim().toLowerCase();
              return selected.includes(value);
            });

            row.hidden = !(globalMatch && inputsMatch && multiSelectsMatch);
            const detail = row.nextElementSibling;
            if (detail?.classList.contains('detail-row') && row.hidden) {
              detail.hidden = true;
              row.classList.remove('is-open');
              const toggle = row.querySelector('.row-toggle');
              if (toggle) {
                toggle.textContent = '+';
              }
            }
          }
        }

        function applySort() {
          if (sortState.column === null) {
            return;
          }

          const button = sortButtons.find(item => Number(item.dataset.sortColumn) === sortState.column);
          const type = button?.dataset.sortType || 'text';
          const sortedRows = [...rows].sort((leftRow, rightRow) => {
            const leftCell = leftRow.children[sortState.column];
            const rightCell = rightRow.children[sortState.column];
            return compareValues(leftCell?.dataset.sort, rightCell?.dataset.sort, type, sortState.direction);
          });

          for (const row of sortedRows) {
            const detail = row.nextElementSibling;
            tbody.appendChild(row);
            if (detail?.classList.contains('detail-row')) {
              tbody.appendChild(detail);
            }
          }
        }

        function updateSortIndicators() {
          for (const button of sortButtons) {
            const indicator = button.querySelector('.sort-indicator');
            const column = Number(button.dataset.sortColumn);
            indicator.textContent = sortState.column === column ? (sortState.direction === 'asc' ? '^' : 'v') : '';
          }
        }

        search?.addEventListener('input', applyFilters);

        for (const multiSelect of multiSelects) {
          const column = Number(multiSelect.dataset.filterColumn);
          const toggle = multiSelect.querySelector('[data-multi-select-toggle]');
          const label = multiSelect.querySelector('[data-multi-select-label]');
          const menu = multiSelect.querySelector('[data-multi-select-menu]');
          const placeholder = label.textContent;
          const values = [...new Set(rows
            .map(row => (row.children[column]?.dataset.filter || row.children[column]?.textContent || '').trim())
            .filter(Boolean))]
            .sort((left, right) => left.localeCompare(right));

          for (const value of values) {
            const option = document.createElement('label');
            option.className = 'multi-select-option';
            const checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.value = value;
            const optionText = document.createElement('span');
            optionText.textContent = value;
            option.append(checkbox, optionText);
            menu.appendChild(option);
            checkbox.addEventListener('change', () => {
              const selected = Array.from(menu.querySelectorAll('input:checked'));
              label.textContent = selected.length === 0
                ? placeholder
                : selected.length === 1
                  ? selected[0].value
                  : `${selected.length} selected`;
              multiSelect.classList.toggle('has-selection', selected.length > 0);
              applyFilters();
            });
          }

          toggle.addEventListener('click', () => {
            const shouldOpen = menu.hidden;
            for (const other of multiSelects) {
              const otherMenu = other.querySelector('[data-multi-select-menu]');
              const otherToggle = other.querySelector('[data-multi-select-toggle]');
              otherMenu.hidden = true;
              otherToggle.setAttribute('aria-expanded', 'false');
              other.classList.remove('is-open');
            }

            menu.hidden = !shouldOpen;
            toggle.setAttribute('aria-expanded', String(shouldOpen));
            multiSelect.classList.toggle('is-open', shouldOpen);
          });
        }

        reset?.addEventListener('click', () => {
          if (search) {
            search.value = '';
          }

          for (const filter of filters) {
            filter.value = '';
          }

          for (const multiSelect of multiSelects) {
            for (const checkbox of multiSelect.querySelectorAll('input:checked')) {
              checkbox.checked = false;
            }
            multiSelect.querySelector('[data-multi-select-label]').textContent =
              multiSelect.dataset.multiSelectPlaceholder;
            multiSelect.classList.remove('has-selection');
          }

          applyFilters();
        });

        filters.forEach(filter => filter.addEventListener('input', applyFilters));

        for (const button of sortButtons) {
          button.addEventListener('click', () => {
            const column = Number(button.dataset.sortColumn);
            sortState.direction = sortState.column === column && sortState.direction === 'asc' ? 'desc' : 'asc';
            sortState.column = column;
            applySort();
            updateSortIndicators();
            applyFilters();
          });
        }

        for (const row of rows) {
          if (!row.hasAttribute('data-toggle-detail')) {
            continue;
          }

          row.addEventListener('click', event => {
            if (event.target.closest('a')) {
              return;
            }

            const detail = row.nextElementSibling;
            if (!detail?.classList.contains('detail-row')) {
              return;
            }

            const isOpening = detail.hidden;
            detail.hidden = !isOpening;
            row.classList.toggle('is-open', isOpening);
            const toggle = row.querySelector('.row-toggle');
            if (toggle) {
              toggle.textContent = isOpening ? '-' : '+';
            }
          });
        }

        applySort();
        updateSortIndicators();
        applyFilters();
      }

      document.addEventListener('click', event => {
        for (const multiSelect of document.querySelectorAll('[data-multi-select]')) {
          if (multiSelect.contains(event.target)) {
            continue;
          }

          multiSelect.querySelector('[data-multi-select-menu]').hidden = true;
          multiSelect.querySelector('[data-multi-select-toggle]').setAttribute('aria-expanded', 'false');
          multiSelect.classList.remove('is-open');
        }
      });

      document.addEventListener('keydown', event => {
        if (event.key !== 'Escape') {
          return;
        }

        for (const multiSelect of document.querySelectorAll('[data-multi-select]')) {
          multiSelect.querySelector('[data-multi-select-menu]').hidden = true;
          multiSelect.querySelector('[data-multi-select-toggle]').setAttribute('aria-expanded', 'false');
          multiSelect.classList.remove('is-open');
        }
      });

      if (navLinks.length > 0) {
        const linkById = new Map(navLinks.map(link => [decodeURIComponent(link.hash.slice(1)), link]));
        const observer = new IntersectionObserver(entries => {
          const visible = entries
            .filter(entry => entry.isIntersecting)
            .sort((left, right) => left.boundingClientRect.top - right.boundingClientRect.top)[0];

          if (!visible) {
            return;
          }

          for (const link of navLinks) {
            link.classList.toggle('is-active', link === linkById.get(visible.target.id));
          }
        }, { rootMargin: '-18% 0px -72% 0px', threshold: 0.01 });

        for (const section of document.querySelectorAll('.table-section[id]')) {
          observer.observe(section);
        }
      }
    })();
