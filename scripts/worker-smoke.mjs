const baseUrl = process.env.IDENTITY_API_URL ?? "http://127.0.0.1:5055";
const runId = `${Date.now()}-${process.pid}`;
const userId = `worker-smoke-${runId}`;
const userHeaders = {
  "Content-Type": "application/json",
  "X-Dev-User-Id": userId
};

async function request(path, options = {}) {
  const response = await fetch(`${baseUrl}${path}`, {
    ...options,
    headers: { ...userHeaders, ...(options.headers ?? {}) }
  });
  const raw = await response.text();
  let body = raw;
  try {
    body = raw ? JSON.parse(raw) : null;
  } catch {
    // Keep plain text for diagnostics.
  }
  return { response, body };
}

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

const start = await request("/api/exams/bootstrap-hsk3-subjective/attempts", { method: "POST" });
assert(start.response.status === 201, `subjective attempt start expected 201, got ${start.response.status}`);
const attemptId = start.body?.id;
assert(typeof attemptId === "string", "worker smoke attempt id is missing");

const answer = await request(`/api/exam-attempts/${attemptId}/answers`, {
  method: "POST",
  body: JSON.stringify({ questionId: "bootstrap-exam-subjective-q1", answer: "我叫小明。" })
});
assert(answer.response.status === 200, `subjective answer expected 200, got ${answer.response.status}`);

const submit = await request(`/api/exam-attempts/${attemptId}/submit`, { method: "POST" });
assert(
  submit.response.status === 200 && submit.body?.subjectiveGradingStatus === "Pending",
  "subjective attempt did not enter Pending before worker delivery"
);

let finalAttempt;
for (let attempt = 0; attempt < 30; attempt += 1) {
  const current = await request(`/api/exam-attempts/${attemptId}`);
  assert(current.response.status === 200, `worker smoke read expected 200, got ${current.response.status}`);
  finalAttempt = current.body;
  if (finalAttempt?.subjectiveGradingStatus === "Failed") break;
  await new Promise(resolve => setTimeout(resolve, 200));
}

assert(finalAttempt?.subjectiveGradingStatus === "Failed", "worker did not deliver failed provider result within 6 seconds");
assert(
  String(finalAttempt?.subjectiveFeedback ?? "").includes("provider chưa được cấu hình"),
  "worker failure feedback did not persist through signed callback"
);

console.log("Worker RabbitMQ consumer + signed failure callback smoke passed");
