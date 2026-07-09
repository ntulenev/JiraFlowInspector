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
        const sortState = {
          column: table.dataset.defaultSortColumn === '' ? null : Number(table.dataset.defaultSortColumn),
          direction: table.dataset.defaultSortDirection || 'asc'
        };

        filters.forEach((filter, index) => {
          filter.dataset.filterColumn = String(index);
        });

        function applyFilters() {
          const globalTerm = (search?.value || '').trim().toLowerCase();
          const columnTerms = new Map(filters.map(filter => [Number(filter.dataset.filterColumn), filter.value.trim().toLowerCase()]));

          for (const row of rows) {
            const cells = Array.from(row.children);
            const globalMatch = !globalTerm || cells.some(cell => (cell.dataset.filter || cell.textContent || '').toLowerCase().includes(globalTerm));
            const columnsMatch = cells.every((cell, index) => {
              const term = columnTerms.get(index);
              return !term || (cell.dataset.filter || cell.textContent || '').toLowerCase().includes(term);
            });

            row.hidden = !(globalMatch && columnsMatch);
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
        reset?.addEventListener('click', () => {
          if (search) {
            search.value = '';
          }

          for (const filter of filters) {
            filter.value = '';
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