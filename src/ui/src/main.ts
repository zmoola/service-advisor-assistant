// eslint-disable-next-line ts(7015)
const apiUrl = "https://localhost:61412/api/advisor"

const form = document.getElementById('complaintForm') as HTMLFormElement;
const input = document.getElementById('complaint') as HTMLTextAreaElement;
const output = document.getElementById('output') as HTMLDivElement;

form.addEventListener('submit', async (ev) => {
  ev.preventDefault();
  const complaint = input.value.trim();
  output.innerHTML = '';
  if (!complaint) {
    output.textContent = 'Please enter a complaint.';
    return;
  }

  output.textContent = 'Processing...';
  try {
    const resp = await fetch(apiUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ complaint })
    });

    if (!resp.ok) {
      const err = await resp.json();
      output.innerHTML = `<strong>Error:</strong> ${err.error || resp.statusText}`;
      return;
    }

    const data = await resp.json();
    renderResult(data);
  } catch (e) {
    output.innerHTML = '<strong>Network error</strong> - could not contact API';
    console.error(e);
  }
});

function renderResult(res: any) {
  const el = document.createElement('div');
  el.innerHTML = `
    <h3>Rephrased Complaint</h3>
    <p>${escapeHtml(res.rephrasedComplaint || '')}</p>
    <h3>Suggested Solutions</h3>
  `;

  const list = document.createElement('ol');
  if (Array.isArray(res.solutions)) {
    for (const s of res.solutions) {
      const li = document.createElement('li');
      li.innerHTML = `<strong>Issue:</strong> ${escapeHtml(s.issue || '')}<br/><strong>Fix:</strong> ${escapeHtml(s.suggestedFix || '')}`;
      if (s.confidence !== undefined) {
        li.innerHTML += `<br/><em>Confidence: ${Number(s.confidence).toFixed(2)}</em>`;
      }
      list.appendChild(li);
    }
  }
  el.appendChild(list);

  if (res.note) {
    const note = document.createElement('p');
    note.innerHTML = `<strong>Note:</strong> ${escapeHtml(res.note)}`;
    el.appendChild(note);
  }

  output.innerHTML = '';
  output.appendChild(el);
}

function escapeHtml(s: string) {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}
