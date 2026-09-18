const baseUrl = process.env.IDENTITY_API_URL ?? "http://127.0.0.1:5056";
const mode = process.env.PERSISTENCE_MODE ?? "write";
const headers = { "Content-Type": "application/json", "X-Dev-User-Id": "marten-persistence-user" };
const curriculumAdminHeaders = { ...headers, "X-Dev-Permission": "curriculum.manage" };

async function request(path, options = {}) {
  const response = await fetch(`${baseUrl}${path}`, {
    ...options,
    headers: { ...headers, ...(options.headers ?? {}) }
  });
  const text = await response.text();
  return { response, body: text ? JSON.parse(text) : null };
}

if (mode === "write") {
  const update = await request("/api/me/profile", {
    method: "PUT",
    body: JSON.stringify({
      displayName: "Marten persisted",
      avatarUrl: null,
      timezone: "Asia/Ho_Chi_Minh",
      studyPreferences: { dailyMinutes: 45, preferredStudyTime: "21:00" }
    })
  });
  if (update.response.status !== 200) {
    throw new Error(`write expected 200, got ${update.response.status}`);
  }

  const importResponse = await request("/api/admin/curriculum/import", {
    method: "POST",
    headers: curriculumAdminHeaders,
    body: JSON.stringify({
      syllabusVersionId: "marten-hsk-3.0",
      syllabusName: "HSK 3.0 Marten smoke",
      sourceType: "smoke-test",
      levels: [{ id: "marten-hsk3", levelNumber: 3, displayName: "HSK 3" }]
    })
  });
  if (importResponse.response.status !== 200) {
    throw new Error(`curriculum write expected 200, got ${importResponse.response.status}`);
  }
  const publishResponse = await request("/api/admin/curriculum/hsk-levels/marten-hsk3/publish", {
    method: "POST",
    headers: curriculumAdminHeaders
  });
  if (publishResponse.response.status !== 200) {
    throw new Error(`curriculum publish expected 200, got ${publishResponse.response.status}`);
  }
  console.log("Marten persistence write passed");
} else {
  const read = await request("/api/me");
  if (read.response.status !== 200 || read.body?.profile?.displayName !== "Marten persisted" || read.body?.profile?.studyPreferences?.dailyMinutes !== 45) {
    throw new Error(`Marten persistence read failed: ${read.response.status}`);
  }
  const curriculum = await request("/api/curriculum/hsk-levels");
  if (curriculum.response.status !== 200 || !curriculum.body?.some(level => level.id === "marten-hsk3")) {
    throw new Error(`Marten curriculum read failed: ${curriculum.response.status}`);
  }
  console.log("Marten persistence restart read passed");
}
