import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';

const base = process.env.IDENTITY_API_URL ?? 'http://127.0.0.1:5055';
const id = `completion-${randomUUID()}`;
async function call(path, method = 'GET', body, admin = false, status = 200) {
  const response = await fetch(`${base}/api${path}`, {
    method,
    headers: { 'Content-Type': 'application/json', 'X-Dev-User-Id': id,
      ...(admin ? { 'X-Dev-Permission': 'curriculum.manage' } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  assert.equal(response.status, status, `${method} ${path}`);
  return response.json();
}

await call('/learning/beginner/stages/tones/complete', 'POST', undefined, false, 409);
assert.deepEqual(await call('/progress/history'), []);
await call('/learning/beginner/start', 'POST');
await call('/learning/beginner/stages/tones/complete', 'POST', undefined, false, 409);
const first = await call('/learning/beginner/stages/pinyin/complete', 'POST');
const retry = await call('/learning/beginner/stages/pinyin/complete', 'POST');
assert.deepEqual(retry, first);
assert.equal((await call('/progress/history')).length, 1);

await call('/admin/curriculum/import', 'POST', {
  syllabusVersionId: id, syllabusName: 'Completion regression', sourceType: 'test',
  levels: [{ id, levelNumber: 1, displayName: 'HSK 1', topics: [{ id: `${id}-topic`, name: 'Test',
    units: [{ id: `${id}-unit`, name: 'Test', lessons: [{ id: `${id}-lesson`, name: 'Test' }] }] }] }],
}, true);
await call(`/admin/curriculum/hsk-levels/${id}/publish`, 'POST', undefined, true);
await call(`/admin/curriculum/hsk-levels/${id}/lessons/${id}-lesson/publish`, 'POST', undefined, true);
await call(`/learning/lessons/${id}-lesson/complete`, 'POST', undefined, false, 409);
assert.equal((await call('/progress/history')).length, 1);
await call(`/learning/lessons/${id}-lesson/start`, 'POST');
assert.equal((await call(`/learning/hsk/${id}`)).isLevelCompleted, false);
assert.equal((await call('/learning/home')).continueTarget, `${id}-lesson`);
await call('/learning/beginner/start', 'POST');
const beginnerHome = await call('/learning/home');
assert.equal(beginnerHome.continueTarget, 'beginner/tones');
assert.equal(beginnerHome.state.currentLessonId, `${id}-lesson`);
await call(`/learning/hsk/${id}/select`, 'POST');
assert.equal((await call('/learning/home')).continueTarget, `${id}-lesson`);
const lesson = await call(`/learning/lessons/${id}-lesson/complete`, 'POST');
assert.deepEqual(await call(`/learning/lessons/${id}-lesson/complete`, 'POST'), lesson);
assert.equal((await call('/progress/history')).length, 2);
const completedPath = await call(`/learning/hsk/${id}`);
assert.deepEqual(completedPath.completedUnitIds, [`${id}-unit`]);
assert.equal(completedPath.isLevelCompleted, true);
console.log('Learning completion guards and retry regression passed');
