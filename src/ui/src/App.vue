<script setup lang="ts">
import { computed, ref } from 'vue';

interface Solution {
  issue?: string;
  suggestedFix?: string;
  confidence?: number;
}

interface AnalysisResponse {
  rephrasedComplaint?: string;
  solutions?: Solution[];
  note?: string;
}

const complaint = ref('');
const isLoading = ref(false);
const errorMessage = ref<string | null>(null);
const result = ref<AnalysisResponse | null>(null);

const apiBaseUrl = (import.meta.env.VITE_API_URL ?? 'https://api-service-advisor-gub3c7cxbeeqfggz.southafricanorth-01.azurewebsites.net').trim();
const hasSolutions = computed(() => Array.isArray(result.value?.solutions) && result.value!.solutions!.length > 0);

async function analyzeComplaint() {
  const trimmedComplaint = complaint.value.trim();

  if (!trimmedComplaint) {
    errorMessage.value = 'Please enter a complaint.';
    result.value = null;
    return;
  }

  isLoading.value = true;
  errorMessage.value = null;
  result.value = null;

  try {
    const response = await fetch(`${apiBaseUrl}/api/advisor`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ complaint: trimmedComplaint })
    });

    const payload = await response.json().catch(() => null);

    if (!response.ok) {
      throw new Error((payload as { error?: string } | null)?.error || response.statusText || 'Request failed');
    }

    result.value = payload as AnalysisResponse;
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : 'Unable to reach the API.';
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <main class="app-shell">
    <section class="card">
      <h1>Service Advisor Assistant</h1>
      <p class="subtitle">Describe the customer concern and receive a structured analysis.</p>

      <form @submit.prevent="analyzeComplaint">
        <label for="complaint">Customer complaint</label>
        <textarea
          id="complaint"
          v-model="complaint"
          rows="6"
          placeholder="e.g. The car makes a grinding noise when braking"
        />

        <button type="submit" :disabled="isLoading">
          {{ isLoading ? 'Analyzing…' : 'Analyze' }}
        </button>
      </form>

      <div v-if="errorMessage" class="message error">{{ errorMessage }}</div>

      <div v-else-if="result" class="result-panel">
        <h2>Rephrased complaint</h2>
        <p>{{ result.rephrasedComplaint || 'No rephrased complaint returned.' }}</p>

        <h2>Suggested solutions</h2>
        <ol v-if="hasSolutions">
          <li v-for="(solution, index) in result.solutions" :key="`${solution.issue}-${index}`">
            <strong>Issue:</strong> {{ solution.issue || 'N/A' }}<br />
            <strong>Fix:</strong> {{ solution.suggestedFix || 'N/A' }}
            <div v-if="solution.confidence !== undefined" class="confidence">
              Confidence: {{ solution.confidence.toFixed(2) }}
            </div>
          </li>
        </ol>
        <p v-else>No solutions were returned.</p>

        <p v-if="result.note" class="note"><strong>Note:</strong> {{ result.note }}</p>
      </div>
    </section>
  </main>
</template>

<style scoped>
:global(body) {
  margin: 0;
  font-family: Arial, sans-serif;
  background: #f4f7fb;
  color: #102542;
}

.app-shell {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 2rem;
}

.card {
  width: min(760px, 100%);
  background: #fff;
  border-radius: 16px;
  box-shadow: 0 16px 40px rgba(16, 37, 66, 0.12);
  padding: 2rem;
}

.subtitle {
  color: #51627a;
  margin-bottom: 1.5rem;
}

label {
  display: block;
  font-weight: 600;
  margin-bottom: 0.5rem;
}

textarea {
  width: 100%;
  box-sizing: border-box;
  padding: 0.8rem;
  border: 1px solid #d6deeb;
  border-radius: 10px;
  resize: vertical;
  min-height: 120px;
}

button {
  margin-top: 1rem;
  padding: 0.75rem 1.25rem;
  border: 0;
  border-radius: 999px;
  background: #2563eb;
  color: #fff;
  font-weight: 700;
  cursor: pointer;
}

button:disabled {
  cursor: wait;
  opacity: 0.7;
}

.message {
  margin-top: 1rem;
  padding: 0.85rem 1rem;
  border-radius: 10px;
}

.error {
  background: #fee2e2;
  color: #991b1b;
}

.result-panel {
  margin-top: 1.5rem;
  padding-top: 1rem;
  border-top: 1px solid #e5ebf3;
}

.confidence {
  color: #4b5563;
  margin-top: 0.25rem;
}

.note {
  margin-top: 1rem;
}
</style>
